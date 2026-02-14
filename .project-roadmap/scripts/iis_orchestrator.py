#!/usr/bin/env python3
"""
IIS Orchestrator — Automated Issue Resolution & Warning Cleanup
Uses Claude Code CLI (-p headless) as sub-agent for AI triage and code fixes.

Usage:
    python iis_orchestrator.py                  # run all (issues + warnings)
    python iis_orchestrator.py issues           # only open issues
    python iis_orchestrator.py warnings         # only CS8xxx warnings
    python iis_orchestrator.py status           # show current status
    python iis_orchestrator.py --dry-run        # simulate without changes
    python iis_orchestrator.py --max-issues 5   # limit issues processed
    python iis_orchestrator.py --max-files 10   # limit files fixed
"""

import sys
if sys.platform == "win32":
    sys.stdout.reconfigure(encoding="utf-8", errors="replace")
    sys.stderr.reconfigure(encoding="utf-8", errors="replace")

import argparse
import datetime
import json
import logging
import os
import re
import shutil
import subprocess
import time
from pathlib import Path

# ── CONFIG ──────────────────────────────────────────────────────────────────
REPO_ROOT = Path(r"D:\github\mRemoteNG")
SCRIPTS_DIR = REPO_ROOT / ".project-roadmap" / "scripts"
ISSUES_DB_DIR = REPO_ROOT / ".project-roadmap" / "issues-db" / "upstream"
STATUS_FILE = SCRIPTS_DIR / "orchestrator-status.json"
LOG_FILE = SCRIPTS_DIR / "orchestrator.log"

BUILD_CMD = [
    "powershell.exe", "-NoProfile", "-ExecutionPolicy", "Bypass",
    "-File", str(REPO_ROOT / "build.ps1"),
    "-Rebuild",
]
TEST_FILTER = (
    "FullyQualifiedName!~UI"
    "&FullyQualifiedName!~CueBanner"
    "&FullyQualifiedName!~Tree.ConnectionTreeTests"
    "&FullyQualifiedName!~PasswordForm"
    "&FullyQualifiedName!~XmlConnectionsLoaderTests.ThrowsWhen"
    "&FullyQualifiedName!~ConnectionInitiatorSshTunnelTests"
)
TEST_DLL = str(
    REPO_ROOT / "mRemoteNGTests" / "bin" / "x64" / "Release" / "mRemoteNGTests.dll"
)
TEST_CMD = [
    "dotnet", "test", TEST_DLL,
    "--verbosity", "normal",
    "--filter", TEST_FILTER,
    "--", "NUnit.DefaultTimeout=15000",
]

UPSTREAM_REPO = "mRemoteNG/mRemoteNG"
FORK_REPO = "robertpopa22/mRemoteNG"
BETA_TAG = "v1.81.0-beta.1"
BETA_URL = f"https://github.com/{FORK_REPO}/releases/tag/{BETA_TAG}"

# Warning codes to fix, in priority order
WARNING_CODES = [
    "CS8618", "CS8602", "CS8600", "CS8604",
    "CS8603", "CS8625", "CS8601", "CS8605",
]

BUILD_TIMEOUT = 300   # 5 min
TEST_TIMEOUT = 300    # 5 min
CLAUDE_TIMEOUT = 600  # 10 min per task

# Environment for Claude sub-process: strip nesting guard so claude -p works
CLAUDE_ENV = {k: v for k, v in os.environ.items()
              if k not in ("CLAUDECODE", "CLAUDE_CODE_ENTRYPOINT")}


# ── LOGGING ─────────────────────────────────────────────────────────────────
def setup_logging():
    fmt = "%(asctime)s [%(levelname)s] %(message)s"
    logging.basicConfig(
        level=logging.INFO,
        format=fmt,
        handlers=[
            logging.FileHandler(LOG_FILE, encoding="utf-8"),
            logging.StreamHandler(sys.stdout),
        ],
    )
    return logging.getLogger("iis-orchestrator")


log = setup_logging()


# ── STATUS TRACKER ──────────────────────────────────────────────────────────
class Status:
    """Persistent status file — readable by any tool/agent at any time."""

    def __init__(self):
        self.data = {
            "started_at": _now_iso(),
            "running": True,
            "current_phase": None,
            "current_task": None,
            "issues": {
                "total_synced": 0,
                "triaged": 0,
                "to_implement": 0,
                "implemented": 0,
                "failed": 0,
                "skipped_wontfix": 0,
                "skipped_duplicate": 0,
                "skipped_needs_info": 0,
                "commented_on_github": 0,
            },
            "warnings": {
                "total_start": 0,
                "total_now": 0,
                "fixed_this_session": 0,
                "by_type": {},
            },
            "commits": [],
            "errors": [],
            "last_updated": None,
        }

    def save(self):
        self.data["last_updated"] = _now_iso()
        content = json.dumps(self.data, indent=2, ensure_ascii=False)
        for attempt in range(3):
            try:
                STATUS_FILE.write_text(content, encoding="utf-8")
                return
            except OSError:
                time.sleep(0.5)
        # Last resort: silently skip (non-critical file)
        log.warning("    [STATUS] Could not write status file (locked)")

    def set_phase(self, phase):
        self.data["current_phase"] = phase
        self.save()

    def set_task(self, **kwargs):
        self.data["current_task"] = {**kwargs, "started_at": _now_iso()}
        self.save()

    def clear_task(self):
        self.data["current_task"] = None
        self.save()

    def add_commit(self, hash, message, tests_passed):
        self.data["commits"].append(
            {"hash": hash, "message": message, "tests_passed": tests_passed}
        )
        self.save()

    def add_error(self, task, step, error):
        self.data["errors"].append(
            {
                "time": datetime.datetime.now().strftime("%H:%M:%S"),
                "task": task,
                "step": step,
                "error": str(error)[:200],
            }
        )
        self.save()

    def finish(self):
        self.data["running"] = False
        self.data["current_phase"] = "done"
        self.clear_task()


# ── HELPERS ─────────────────────────────────────────────────────────────────
STALE_PROCESSES = ["notepad.exe", "testhost.exe", "mstsc.exe"]


def kill_stale_processes():
    """Kill processes that tests/Claude may have left open."""
    for proc in STALE_PROCESSES:
        try:
            subprocess.run(["taskkill", "//F", "//IM", proc],
                           capture_output=True, timeout=10)
        except Exception:
            pass


def _now_iso():
    return datetime.datetime.now().isoformat(timespec="seconds")


def _run(cmd, timeout=60, cwd=None, capture=True):
    """Run a command and return CompletedProcess."""
    return subprocess.run(
        cmd,
        capture_output=capture,
        text=True,
        timeout=timeout,
        cwd=cwd or str(REPO_ROOT),
        encoding="utf-8",
        errors="replace",
    )


def _extract_json(text):
    """Extract a JSON object from Claude's output (may be wrapped)."""
    # Direct parse
    try:
        return json.loads(text)
    except (json.JSONDecodeError, TypeError):
        pass

    # Claude --output-format json wraps in {"result": "..."}
    try:
        outer = json.loads(text)
        if isinstance(outer, dict) and "result" in outer:
            inner = outer["result"]
            if isinstance(inner, str):
                return _extract_json(inner)
            return inner
    except (json.JSONDecodeError, TypeError):
        pass

    # Find JSON with "decision" key
    m = re.search(r"\{[^{}]*\"decision\"[^{}]*\}", text, re.DOTALL)
    if m:
        try:
            return json.loads(m.group())
        except json.JSONDecodeError:
            pass

    # Find ```json ... ```
    m = re.search(r"```(?:json)?\s*(\{.*?\})\s*```", text, re.DOTALL)
    if m:
        try:
            return json.loads(m.group(1))
        except json.JSONDecodeError:
            pass

    return None


# ── CORE: BUILD & TEST ─────────────────────────────────────────────────────
def run_build(capture_output=False):
    """Run build.ps1.  Returns (ok: bool, output: str|None)."""
    log.info("    [BUILD] Running build.ps1 ...")
    try:
        r = _run(BUILD_CMD, timeout=BUILD_TIMEOUT)
        full = (r.stdout or "") + "\n" + (r.stderr or "")
        ok = r.returncode == 0
        if not ok:
            log.error("    [BUILD] FAILED (exit %d)", r.returncode)
        else:
            log.info("    [BUILD] OK")
        return (ok, full) if capture_output else (ok, None)
    except subprocess.TimeoutExpired:
        log.error("    [BUILD] TIMEOUT (%ds)", BUILD_TIMEOUT)
        return (False, None)
    except Exception as e:
        log.error("    [BUILD] ERROR: %s", e)
        return (False, None)


def run_tests():
    """Run non-UI tests.  Returns True if all pass."""
    log.info("    [TEST] Running tests ...")
    kill_stale_processes()

    try:
        r = _run(TEST_CMD, timeout=TEST_TIMEOUT)
        out = r.stdout or ""
        if "Failed!" in out or r.returncode != 0:
            m = re.search(r"Failed:\s+(\d+)", out)
            if m and int(m.group(1)) > 0:
                log.error("    [TEST] FAILED: %s", m.group(0))
                return False
        m = re.search(r"Passed:\s+(\d+)", out)
        if m:
            log.info("    [TEST] OK (%s passed)", m.group(1))
        kill_stale_processes()
        return r.returncode == 0
    except subprocess.TimeoutExpired:
        log.error("    [TEST] TIMEOUT (%ds)", TEST_TIMEOUT)
        kill_stale_processes()
        return False
    except Exception as e:
        log.error("    [TEST] ERROR: %s", e)
        kill_stale_processes()
        return False


# ── CORE: GIT ───────────────────────────────────────────────────────────────
def git_has_changes():
    r = _run(["git", "status", "--porcelain"])
    return bool((r.stdout or "").strip())


def git_commit(message):
    """Stage all + commit.  Returns commit hash or None."""
    if not git_has_changes():
        return None
    try:
        _run(["git", "add", "-A"])
        full_msg = f"{message}\n\nCo-Authored-By: Claude Opus 4.6 <noreply@anthropic.com>"
        _run(["git", "commit", "-m", full_msg])
        r = _run(["git", "rev-parse", "HEAD"])
        return (r.stdout or "").strip()
    except Exception as e:
        log.error("    [GIT] commit failed: %s", e)
        return None


def git_push():
    try:
        _run(["git", "push"], timeout=60)
        log.info("    [GIT] Pushed to origin")
    except Exception as e:
        log.warning("    [GIT] Push failed: %s", e)


def git_restore():
    """Revert all uncommitted changes."""
    try:
        _run(["git", "checkout", "--", "."])
        _run(["git", "clean", "-fd"])
        log.info("    [GIT] Reverted all changes")
    except Exception as e:
        log.warning("    [GIT] Restore failed: %s", e)


# ── CORE: CLAUDE SUB-AGENT ─────────────────────────────────────────────────
def claude_run(prompt, max_turns=15, json_output=False, timeout=CLAUDE_TIMEOUT):
    """Call claude -p (headless).  Returns stdout string.
    Uses CLAUDE_ENV to strip CLAUDECODE nesting guard."""
    cmd = ["claude", "-p", prompt, "--max-turns", str(max_turns)]
    if json_output:
        cmd += ["--output-format", "json"]
    try:
        r = subprocess.run(
            cmd,
            capture_output=True,
            text=True,
            timeout=timeout,
            cwd=str(REPO_ROOT),
            encoding="utf-8",
            errors="replace",
            env=CLAUDE_ENV,
        )
        kill_stale_processes()
        if r.returncode != 0:
            log.error("    [CLAUDE] exit %d: %s", r.returncode, (r.stderr or "")[:200])
            return None
        return r.stdout or ""
    except subprocess.TimeoutExpired:
        log.error("    [CLAUDE] TIMEOUT (%ds)", timeout)
        kill_stale_processes()
        return None
    except Exception as e:
        log.error("    [CLAUDE] ERROR: %s", e)
        kill_stale_processes()
        return None


# ── CORE: GITHUB COMMENTS ──────────────────────────────────────────────────
def post_github_comment(issue_num, commit_hash, description):
    """Post a fix-available comment on upstream issue."""
    comment = (
        f"**Fix available for testing**\n\n"
        f"**Commit:** [`{commit_hash[:8]}`]"
        f"(https://github.com/{FORK_REPO}/commit/{commit_hash})\n"
        f"**Branch:** `main`\n"
        f"**What changed:** {description}\n\n"
        f"**Download latest beta:** [{BETA_TAG}]({BETA_URL})\n\n"
        f"Please test and report if this resolves your issue.\n\n"
        f"---\n"
        f"_Automated by mRemoteNG Issue Intelligence System_"
    )
    try:
        _run(
            ["gh", "issue", "comment", str(issue_num),
             "--repo", UPSTREAM_REPO, "--body", comment],
            timeout=30,
        )
        log.info("    [GITHUB] Comment posted on #%d", issue_num)
        return True
    except Exception as e:
        log.warning("    [GITHUB] Comment failed on #%d: %s", issue_num, e)
        return False


def update_issue_json(issue_num, new_status, description=""):
    """Update issue status in local IIS JSON DB."""
    json_file = ISSUES_DB_DIR / f"{issue_num:04d}.json"
    if not json_file.exists():
        return
    try:
        data = json.loads(json_file.read_text(encoding="utf-8"))
        data["our_status"] = new_status
        if "iterations" not in data:
            data["iterations"] = []
        data["iterations"].append({
            "seq": len(data["iterations"]) + 1,
            "date": datetime.date.today().isoformat(),
            "type": new_status,
            "description": description,
        })
        json_file.write_text(
            json.dumps(data, indent=2, ensure_ascii=False), encoding="utf-8"
        )
    except Exception as e:
        log.warning("    [IIS] JSON update failed for #%d: %s", issue_num, e)


# ── CORE: WARNING PARSER ───────────────────────────────────────────────────
_WARN_RE = re.compile(r"(.+?)\((\d+),\d+\):\s*warning\s+(CS\d{4}):\s*(.+)")


def parse_warnings(build_output):
    """Parse MSBuild output.  Returns {file: [{line, code, message}]}."""
    result = {}
    for line in build_output.splitlines():
        m = _WARN_RE.search(line)
        if not m:
            continue
        fpath, lineno, code, msg = m.group(1).strip(), int(m.group(2)), m.group(3), m.group(4).strip()
        if code not in WARNING_CODES:
            continue
        result.setdefault(fpath, []).append(
            {"line": lineno, "code": code, "message": msg}
        )
    return result


# ── FLUX 1: OPEN ISSUES ────────────────────────────────────────────────────
def load_actionable_issues():
    """Load issues from JSON DB that need triage or implementation."""
    issues = []
    if not ISSUES_DB_DIR.exists():
        log.warning("Issues DB not found: %s", ISSUES_DB_DIR)
        return issues
    for f in sorted(ISSUES_DB_DIR.glob("*.json")):
        if f.name.startswith("_"):
            continue
        try:
            data = json.loads(f.read_text(encoding="utf-8"))
            if data.get("our_status", "new") in ("new", "triaged", "roadmap"):
                issues.append(data)
        except Exception:
            pass

    # Sort: P0 first
    prio = {"P0-critical": 0, "P1-security": 1, "P2-bug": 2, "P3-enhancement": 3, "P4-debt": 4}
    issues.sort(key=lambda x: prio.get(x.get("priority", "P4-debt"), 5))
    return issues


def ai_triage(issue):
    """Claude analyzes an issue and returns a triage decision."""
    num = issue["number"]
    title = issue.get("title", "")
    body = (issue.get("body") or "")[:2000]
    labels = ", ".join(issue.get("labels", []))
    comments = issue.get("comments", [])[-3:]
    comments_text = "\n".join(
        f"  [{c.get('author', '?')}]: {c.get('snippet', '')[:300]}" for c in comments
    )

    prompt = f"""You are a triage agent for mRemoteNG (.NET 10, WinForms, remote connections manager).
Analyze this GitHub issue and decide what to do.

Issue #{num}: {title}
Labels: {labels}
State: {issue.get('state', 'open')}

Body:
{body}

Recent comments:
{comments_text}

RESPOND ONLY with a single valid JSON object (no markdown, no text before/after):
{{"decision":"implement|wontfix|duplicate|needs_info","reason":"one sentence","priority":"P0|P1|P2|P3|P4","estimated_files":["relative/path.cs"],"approach":"brief fix description"}}

Rules:
- implement = clear bug/feature, we can fix it
- wontfix = by design, obsolete, or out of scope
- duplicate = already fixed in v1.79/v1.80 or duplicate of another issue
- needs_info = unclear, need user response
- Be conservative: if unclear, choose needs_info"""

    out = claude_run(prompt, max_turns=3, json_output=True, timeout=120)
    if not out:
        return None
    return _extract_json(out)


def implement_issue(issue, triage, status):
    """Use Claude to fix an issue, verify, commit, push, comment."""
    num = issue["number"]
    title = issue.get("title", "")
    body = (issue.get("body") or "")[:3000]
    approach = triage.get("approach", "")
    files = triage.get("estimated_files", [])

    status.set_task(type="issue_fix", issue=num, step="claude_fixing")
    log.info("  [FIX] Claude working on #%d ...", num)

    prompt = f"""Project: mRemoteNG (.NET 10, WinForms, COM references)
Working directory: D:\\github\\mRemoteNG
Branch: main

Fix GitHub issue #{num}: {title}

Description:
{body}

Recommended approach: {approach}
Likely files: {', '.join(files) if files else 'search the codebase'}

RULES (CRITICAL):
- Read code BEFORE modifying
- Do NOT change existing behavior — only fix the reported issue
- Do NOT create interactive tests (no dialogs, MessageBox, notepad.exe)
- After fixing, run build: powershell.exe -NoProfile -ExecutionPolicy Bypass -File "D:\\github\\mRemoteNG\\build.ps1"
- After build passes, run tests: dotnet test "D:\\github\\mRemoteNG\\mRemoteNGTests\\bin\\x64\\Release\\mRemoteNGTests.dll" --filter "{TEST_FILTER}" -- NUnit.DefaultTimeout=5000
- If YOUR change breaks tests, fix it.  If tests fail for unrelated reasons, ignore.
- Do ONLY the fix.  Nothing else."""

    out = claude_run(prompt, max_turns=25, timeout=CLAUDE_TIMEOUT)
    if out is None:
        status.add_error(f"issue_{num}", "claude", "returned None")
        git_restore()
        return False

    # ── Independent verification ──
    status.set_task(type="issue_fix", issue=num, step="building")
    build_ok, _ = run_build()
    if not build_ok:
        log.error("  [FIX] Build FAILED for #%d — reverting", num)
        status.add_error(f"issue_{num}", "build", "failed")
        git_restore()
        return False

    status.set_task(type="issue_fix", issue=num, step="testing")
    if not run_tests():
        log.error("  [FIX] Tests FAILED for #%d — reverting", num)
        status.add_error(f"issue_{num}", "test", "failed")
        git_restore()
        return False

    # ── Commit + push + comment ──
    status.set_task(type="issue_fix", issue=num, step="committing")
    short = (approach or title)[:60]
    msg = f"fix(#{num}): {short}"
    h = git_commit(msg)
    if not h:
        log.warning("  [FIX] No changes to commit for #%d", num)
        return False

    status.add_commit(h, msg, True)
    status.data["issues"]["implemented"] += 1
    log.info("  [FIX] Committed %s — %s", h[:8], msg)

    status.set_task(type="issue_fix", issue=num, step="pushing")
    git_push()

    status.set_task(type="issue_fix", issue=num, step="commenting")
    if post_github_comment(num, h, short):
        status.data["issues"]["commented_on_github"] += 1
    update_issue_json(num, "testing", f"Fix in {h[:8]}")

    status.clear_task()
    return True


def flux_issues(status, dry_run=False, max_issues=None):
    """FLUX 1: Sync, triage, implement open issues."""
    log.info("=" * 60)
    log.info("  FLUX 1: Open Issues")
    log.info("=" * 60)

    # Sync
    status.set_task(type="sync", step="syncing")
    sync_script = SCRIPTS_DIR / "Sync-Issues.ps1"
    if sync_script.exists():
        log.info("  Syncing issues from GitHub ...")
        try:
            _run(
                ["powershell.exe", "-NoProfile", "-ExecutionPolicy", "Bypass",
                 "-File", str(sync_script)],
                timeout=120,
            )
        except Exception as e:
            log.warning("  Sync failed: %s", e)

    # Load
    issues = load_actionable_issues()
    status.data["issues"]["total_synced"] = len(issues)
    status.save()
    log.info("  Found %d actionable issues", len(issues))

    if max_issues:
        issues = issues[:max_issues]

    # Triage + implement
    for i, issue in enumerate(issues, 1):
        num = issue["number"]
        title = issue.get("title", "")[:50]
        log.info("[%d/%d] Issue #%d: %s", i, len(issues), num, title)
        print_progress("ISSUES", i, len(issues), f"#{num} {title}", status)

        # Triage
        status.set_task(type="triage", issue=num, step="analyzing")
        if dry_run:
            log.info("  [DRY RUN] skip triage")
            continue

        triage = ai_triage(issue)
        if not triage:
            status.add_error(f"issue_{num}", "triage", "AI returned None")
            status.data["issues"]["failed"] += 1
            continue

        decision = triage.get("decision", "needs_info")
        status.data["issues"]["triaged"] += 1
        log.info("  Decision: %s — %s", decision, triage.get("reason", ""))

        if decision == "implement":
            status.data["issues"]["to_implement"] += 1
            implement_issue(issue, triage, status)
        elif decision == "wontfix":
            status.data["issues"]["skipped_wontfix"] += 1
            update_issue_json(num, "wontfix", triage.get("reason", ""))
        elif decision == "duplicate":
            status.data["issues"]["skipped_duplicate"] += 1
            update_issue_json(num, "duplicate", triage.get("reason", ""))
        elif decision == "needs_info":
            status.data["issues"]["skipped_needs_info"] += 1

        status.save()


# ── FLUX 2: WARNING CLEANUP ────────────────────────────────────────────────
def flux_warnings(status, dry_run=False, max_files=None):
    """FLUX 2: Extract warnings, fix file-by-file, verify, commit."""
    log.info("=" * 60)
    log.info("  FLUX 2: Warning Cleanup")
    log.info("=" * 60)

    # Build + extract warnings
    status.set_task(type="warnings", step="extracting")
    build_ok, output = run_build(capture_output=True)
    if not build_ok or not output:
        log.error("  Build failed — cannot extract warnings")
        return

    warnings = parse_warnings(output)
    total = sum(len(v) for v in warnings.values())
    status.data["warnings"]["total_start"] = total
    status.data["warnings"]["total_now"] = total

    # Count by type
    type_counts = {}
    for file_w in warnings.values():
        for w in file_w:
            type_counts[w["code"]] = type_counts.get(w["code"], 0) + 1
    for code in WARNING_CODES:
        cnt = type_counts.get(code, 0)
        if cnt:
            status.data["warnings"]["by_type"][code] = {"start": cnt, "now": cnt, "fixed": 0}
            log.info("  %s: %d", code, cnt)
    status.save()
    log.info("  Total: %d warnings across %d files", total, len(warnings))

    # Sort: most warnings first
    sorted_files = sorted(warnings.items(), key=lambda x: -len(x[1]))
    if max_files:
        sorted_files = sorted_files[:max_files]

    # Fix file by file
    for i, (fpath, file_warnings) in enumerate(sorted_files, 1):
        rel = os.path.relpath(fpath, REPO_ROOT)
        n = len(file_warnings)
        log.info("[%d/%d] %s (%d warnings)", i, len(sorted_files), rel, n)
        print_progress("WARNINGS", i, len(sorted_files), f"{rel} ({n}w)", status)

        status.set_task(type="warning_fix", file=rel, step="fixing", count=n)

        if dry_run:
            log.info("  [DRY RUN] skip")
            continue

        # Format warnings for Claude
        w_text = "\n".join(
            f"  Line {w['line']}: {w['code']} -- {w['message']}"
            for w in file_warnings[:50]
        )

        prompt = f"""Project: mRemoteNG (.NET 10, WinForms)
Working directory: D:\\github\\mRemoteNG
File: {rel}

Fix ALL these nullable reference type warnings:
{w_text}

CRITICAL RULES:
- When adding `?` to a field type, check ALL usages — add `?.` and `?? default`
- Do NOT generate new CS8602 warnings — fix the cascade immediately
- Do NOT change logic/behavior — only types and null checks
- Getter with .Trim() -> ?.Trim() ?? string.Empty
- Use `= null!` ONLY for fields guaranteed initialized in constructor
- GetPropertyValue pattern: result is TPropertyType typed ? typed : value
- Read the file FIRST, understand context, then fix
- Do NOT create new files or tests"""

        out = claude_run(prompt, max_turns=15, timeout=300)
        if out is None:
            status.add_error(rel, "claude", "returned None")
            git_restore()
            continue

        # Verify
        status.set_task(type="warning_fix", file=rel, step="building")
        build_ok, new_output = run_build(capture_output=True)
        if not build_ok:
            log.error("  Build FAILED for %s — reverting", rel)
            status.add_error(rel, "build", "failed")
            git_restore()
            continue

        status.set_task(type="warning_fix", file=rel, step="testing")
        if not run_tests():
            log.error("  Tests FAILED for %s — reverting", rel)
            status.add_error(rel, "test", "failed")
            git_restore()
            continue

        # Count improvement
        new_warnings = parse_warnings(new_output) if new_output else {}
        new_total = sum(len(v) for v in new_warnings.values())
        fixed = status.data["warnings"]["total_now"] - new_total

        if fixed <= 0:
            log.warning("  No improvement for %s (%d -> %d) — reverting", rel,
                        status.data["warnings"]["total_now"], new_total)
            git_restore()
            continue

        status.data["warnings"]["total_now"] = new_total
        status.data["warnings"]["fixed_this_session"] += fixed

        # Recalculate per-type counts from new build output
        new_type_counts = {}
        for file_w in new_warnings.values():
            for w in file_w:
                new_type_counts[w["code"]] = new_type_counts.get(w["code"], 0) + 1
        for code, d in status.data["warnings"]["by_type"].items():
            new_cnt = new_type_counts.get(code, 0)
            d["fixed"] = d["start"] - new_cnt
            d["now"] = new_cnt

        # Commit (no push for warnings — batch push at end)
        msg = f"chore: fix {fixed} nullable warnings in {os.path.basename(rel)}"
        h = git_commit(msg)
        if h:
            status.add_commit(h, msg, True)
            log.info("  Committed %s — fixed %d warnings (%d remaining)",
                     h[:8], fixed, new_total)
        status.save()

    # Batch push at end
    if not dry_run and status.data["commits"]:
        log.info("  Pushing all warning-fix commits ...")
        git_push()

    status.clear_task()


# ── DISPLAY ─────────────────────────────────────────────────────────────────
def print_progress(phase, current, total, detail, status):
    """Print live progress line."""
    bar_len = 20
    filled = int(bar_len * current / max(total, 1))
    bar = "#" * filled + "-" * (bar_len - filled)
    pct = int(100 * current / max(total, 1))

    w = status.data["warnings"]
    commits = len(status.data["commits"])
    errors = len(status.data["errors"])

    line = f"  [{bar}] {current}/{total} ({pct}%)  {detail[:45]:<45}"
    if w["total_start"]:
        line += f"  W:{w['total_start']}->{w['total_now']}(-{w['fixed_this_session']})"
    line += f"  C:{commits} E:{errors}"
    print(line)


def print_summary(status):
    """Print final summary."""
    s = status.data
    print()
    print("=" * 65)
    print("  IIS Orchestrator — Summary")
    print("=" * 65)

    iss = s["issues"]
    if iss["total_synced"]:
        print(f"\n  Issues:    {iss['implemented']} fixed / {iss['to_implement']} planned "
              f"/ {iss['total_synced']} synced")
        print(f"             {iss['skipped_wontfix']} wontfix, "
              f"{iss['skipped_duplicate']} duplicate, "
              f"{iss['skipped_needs_info']} needs-info, "
              f"{iss['failed']} failed")
        print(f"             {iss['commented_on_github']} GitHub comments posted")

    w = s["warnings"]
    if w["total_start"]:
        print(f"\n  Warnings:  {w['total_start']} -> {w['total_now']}  "
              f"(-{w['fixed_this_session']} fixed)")
        for code, d in sorted(w.get("by_type", {}).items()):
            print(f"             {code}: {d['start']} -> {d['now']}  (-{d['fixed']})")

    if s["commits"]:
        print(f"\n  Commits:   {len(s['commits'])}")
        for c in s["commits"][-10:]:
            tag = "OK" if c["tests_passed"] else "FAIL"
            print(f"    [{tag}] {c['hash'][:8]} {c['message'][:55]}")

    if s["errors"]:
        print(f"\n  Errors:    {len(s['errors'])}")
        for e in s["errors"][-5:]:
            print(f"    [{e['time']}] {e['task']}: {e['step']} -- {e['error'][:55]}")

    dur = ""
    try:
        t0 = datetime.datetime.fromisoformat(s["started_at"])
        dur = str(datetime.datetime.now() - t0).split(".")[0]
    except Exception:
        pass
    print(f"\n  Duration:  {dur}")
    print("=" * 65)


def show_status():
    """Read and display the current status file."""
    if not STATUS_FILE.exists():
        print("No status file found.  Orchestrator has not run yet.")
        return

    s = json.loads(STATUS_FILE.read_text(encoding="utf-8"))
    running = s.get("running", False)
    phase = s.get("current_phase", "?")
    task = s.get("current_task")

    print()
    print("=== IIS Orchestrator Status ===")
    print(f"  Running: {'YES' if running else 'no'}")
    print(f"  Phase:   {phase}")
    print(f"  Started: {s.get('started_at', '?')}")
    print(f"  Updated: {s.get('last_updated', '?')}")

    if task:
        print(f"\n  Current task:")
        for k, v in task.items():
            print(f"    {k}: {v}")

    iss = s.get("issues", {})
    if iss.get("total_synced"):
        print(f"\n  Issues: {iss.get('implemented', 0)} fixed / "
              f"{iss.get('to_implement', 0)} planned / "
              f"{iss.get('total_synced', 0)} synced")

    w = s.get("warnings", {})
    if w.get("total_start"):
        print(f"\n  Warnings: {w['total_start']} -> {w['total_now']} "
              f"(-{w.get('fixed_this_session', 0)})")

    commits = s.get("commits", [])
    errors = s.get("errors", [])
    print(f"\n  Commits: {len(commits)}  |  Errors: {len(errors)}")

    if errors:
        print(f"\n  Last errors:")
        for e in errors[-3:]:
            print(f"    [{e['time']}] {e['task']}: {e['step']} -- {e['error'][:60]}")
    print()


# ── MAIN ────────────────────────────────────────────────────────────────────
def main():
    parser = argparse.ArgumentParser(
        description="IIS Orchestrator — mRemoteNG automated issue & warning resolution"
    )
    parser.add_argument(
        "mode", nargs="?", default="all",
        choices=["all", "issues", "warnings", "status"],
        help="all (default), issues, warnings, or status",
    )
    parser.add_argument("--dry-run", action="store_true",
                        help="Simulate without making changes")
    parser.add_argument("--max-issues", type=int, default=None,
                        help="Max issues to process")
    parser.add_argument("--max-files", type=int, default=None,
                        help="Max files to fix (warnings mode)")

    args = parser.parse_args()

    if args.mode == "status":
        show_status()
        return

    # Preflight checks
    if not shutil.which("claude"):
        print("ERROR: 'claude' CLI not found in PATH.  Install Claude Code first.")
        sys.exit(1)
    if not shutil.which("gh"):
        print("ERROR: 'gh' CLI not found in PATH.  Install GitHub CLI first.")
        sys.exit(1)
    if not REPO_ROOT.exists():
        print(f"ERROR: Repo not found at {REPO_ROOT}")
        sys.exit(1)

    status = Status()

    try:
        if args.mode in ("all", "issues"):
            status.set_phase("issues")
            flux_issues(status, dry_run=args.dry_run, max_issues=args.max_issues)

        if args.mode in ("all", "warnings"):
            status.set_phase("warnings")
            flux_warnings(status, dry_run=args.dry_run, max_files=args.max_files)

        status.finish()
        log.info("=== Orchestrator finished ===")
        print_summary(status)

    except KeyboardInterrupt:
        log.warning("Interrupted by user (Ctrl+C)")
        status.finish()
        print_summary(status)

    except Exception as e:
        log.error("FATAL: %s", e, exc_info=True)
        status.add_error("orchestrator", "fatal", str(e))
        status.finish()


if __name__ == "__main__":
    main()
