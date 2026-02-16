#!/usr/bin/env python3
"""
IIS Orchestrator — Automated Issue Resolution & Warning Cleanup
Uses Claude Code CLI (-p headless) as sub-agent for AI triage and code fixes.
Also provides the full Issue Intelligence System (sync, analyze, update, report).

Usage — IIS (Issue Intelligence):
    python iis_orchestrator.py sync                               # sync both repos
    python iis_orchestrator.py sync --repos upstream --issues 2735,3044  # targeted sync
    python iis_orchestrator.py sync --include-closed              # include closed issues
    python iis_orchestrator.py analyze                            # show actionable items
    python iis_orchestrator.py analyze --waiting-only             # only waiting for us
    python iis_orchestrator.py analyze --priority P2-bug --status new
    python iis_orchestrator.py update --issue 3044 --status triaged --priority P2-bug
    python iis_orchestrator.py update --issue 3044 --status released --release v1.81.0 --post-comment
    python iis_orchestrator.py report                             # save markdown report
    python iis_orchestrator.py report --include-all --no-save     # full inventory to console

Usage — Orchestrator (AI-driven fix automation):
    python iis_orchestrator.py                  # run all (issues + warnings)
    python iis_orchestrator.py issues           # only open issues
    python iis_orchestrator.py warnings         # only CS8xxx warnings
    python iis_orchestrator.py status           # show current status
    python iis_orchestrator.py --dry-run        # simulate without changes
    python iis_orchestrator.py --max-issues 5   # limit issues processed
    python iis_orchestrator.py --max-files 10   # limit files fixed
    python iis_orchestrator.py --squash         # one commit per session
    python iis_orchestrator.py --max-passes 3   # limit multi-pass iterations
    python iis_orchestrator.py --parallel 5     # fix 5 files in parallel per batch
"""

import sys
if sys.platform == "win32":
    sys.stdout.reconfigure(encoding="utf-8", errors="replace")
    sys.stderr.reconfigure(encoding="utf-8", errors="replace")

import argparse
import concurrent.futures
import datetime
import json
import logging
import os
import re
import shutil
import subprocess
import threading
import time
from pathlib import Path

# ── CONFIG ──────────────────────────────────────────────────────────────────
REPO_ROOT = Path(r"D:\github\mRemoteNG")
SCRIPTS_DIR = REPO_ROOT / ".project-roadmap" / "scripts"
ISSUES_DB_DIR = REPO_ROOT / ".project-roadmap" / "issues-db" / "upstream"
ISSUES_DB_ROOT = REPO_ROOT / ".project-roadmap" / "issues-db"
ISSUES_DB_UPSTREAM = ISSUES_DB_ROOT / "upstream"
ISSUES_DB_FORK = ISSUES_DB_ROOT / "fork"
META_PATH = ISSUES_DB_ROOT / "_meta.json"
ROADMAP_PATH = ISSUES_DB_ROOT / "_roadmap.json"
REPORTS_DIR = ISSUES_DB_ROOT / "reports"
STATUS_FILE = SCRIPTS_DIR / "orchestrator-status.json"
LOG_FILE = SCRIPTS_DIR / "orchestrator.log"
CHAIN_CONTEXT_DIR = SCRIPTS_DIR / "chain-context"
TIMEOUT_HISTORY_FILE = CHAIN_CONTEXT_DIR / "_timeout_history.json"
TIMEOUT_ESCALATION_FACTOR = 1.5   # multiply timeout after each timeout failure
TIMEOUT_MAX_MULTIPLIER = 4.0      # cap — don't let timeouts grow past 4x estimated
TIMEOUT_MIN = 60                  # absolute minimum (seconds)
TIMEOUT_MAX = 3600                # absolute cap (1 hour)
TIMEOUT_HISTORY_MAX_SAMPLES = 50  # keep last N durations per agent/task for p80

BUILD_CMD = [
    "powershell.exe", "-NoProfile", "-ExecutionPolicy", "Bypass",
    "-File", str(REPO_ROOT / "build.ps1"),
    "-Rebuild",
]
# Parallel test runner: 5 processes grouped by namespace (2.1x speedup)
# Uses run-tests.ps1 -NoBuild (build is done separately by orchestrator)
TEST_CMD = [
    "powershell.exe", "-NoProfile", "-ExecutionPolicy", "Bypass",
    "-File", str(REPO_ROOT / "run-tests.ps1"),
    "-NoBuild",
]
# Legacy single-process filter (used only in Claude prompts for sub-agent builds)
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

UPSTREAM_REPO = "mRemoteNG/mRemoteNG"
FORK_REPO = "robertpopa22/mRemoteNG"
CSPROJ_PATH = REPO_ROOT / "mRemoteNG" / "mRemoteNG.csproj"


def _read_version_from_csproj():
    """Read <Version> from mRemoteNG.csproj (single source of truth)."""
    try:
        content = CSPROJ_PATH.read_text(encoding="utf-8")
        m = re.search(r"<Version>([^<]+)</Version>", content)
        if m:
            return m.group(1).strip()
    except Exception:
        pass
    return "0.0.0"


def get_beta_tag():
    """Return the current version tag (e.g. 'v1.81.0-beta.2') from csproj."""
    return f"v{_read_version_from_csproj()}"


def get_beta_url():
    """Return a stable URL to the fork's releases page.

    Uses the generic releases URL instead of a specific tag — this never
    breaks regardless of CI tag naming conventions (NB tags, date prefixes, etc.).
    """
    return f"https://github.com/{FORK_REPO}/releases"

# Warning codes to fix, in priority order
WARNING_CODES = [
    "CS8618", "CS8602", "CS8600", "CS8604",
    "CS8603", "CS8625", "CS8601", "CS8605",
]

BUILD_TIMEOUT = 300   # 5 min
TEST_TIMEOUT = 300    # 5 min
CLAUDE_TIMEOUT = 600  # 10 min per task
CLAUDE_RETRIES = 2    # retry on failure
CODEX_TIMEOUT = 1800  # 30 min — codex needs more time for complex implementations
CODEX_RETRIES = 1     # codex e scump; 1 retry

# Environment for Claude sub-process: strip nesting guard so claude -p works
CLAUDE_ENV = {k: v for k, v in os.environ.items()
              if k not in ("CLAUDECODE", "CLAUDE_CODE_ENTRYPOINT")}

# ── AGENT CONFIG ──────────────────────────────────────────────────────────
# Which AI agent to use for each task type: "codex", "claude", or "gemini"
AGENT_CONFIG = {
    "triage":               "codex",    # ai_triage() — JSON analysis
    "implement":            "codex",    # implement_issue() — full code fix
    "warning_fix":          "codex",    # _fix_single_file()
    "warning_fix_parallel": "codex",    # _claude_fix_file_only()
}
AGENT_CHAIN = ["codex", "gemini", "claude"]  # fallback order: primary → secondary → tertiary
GEMINI_MODEL = "gemini-3-pro-preview"   # overridable via --gemini-model
CODEX_MODEL = "gpt-5.3-codex"          # overridable via --codex-model
CODEX_REASONING = "xhigh"               # model_reasoning_effort for codex
AGENT_FALLBACK_ENABLED = True           # if primary fails, try the next agent in chain
_session_agents_used = set()            # tracks which agents contributed (for co-author)
_last_dispatch_timed_out = False        # set True by sub-agents on TimeoutExpired
_last_dispatch_partial_output = ""      # partial stdout captured before timeout

# Resolve full paths to CLI tools (Windows needs .CMD extension for subprocess)
GEMINI_CMD = shutil.which("gemini") or "gemini"
CODEX_CMD = shutil.which("codex") or "codex"


# ── TIMEOUT ESTIMATION (complexity + history + escalation) ───────────────
def _load_timeout_history():
    """Load timeout history from disk.
    Schema: {
        "durations": {"codex": {"triage": [12,15], "implement": [300,450]}, ...},
        "escalations": {"triage_739": 1.5, "impl_739": 2.25, ...}
    }"""
    if TIMEOUT_HISTORY_FILE.exists():
        try:
            data = json.loads(TIMEOUT_HISTORY_FILE.read_text(encoding="utf-8"))
            # Migration: old format was flat {issue_key: multiplier}
            if "durations" not in data:
                data = {"durations": {}, "escalations": data}
            return data
        except Exception:
            pass
    return {"durations": {}, "escalations": {}}


def _save_timeout_history(history):
    """Save timeout history to disk."""
    CHAIN_CONTEXT_DIR.mkdir(parents=True, exist_ok=True)
    TIMEOUT_HISTORY_FILE.write_text(
        json.dumps(history, indent=2, ensure_ascii=False) + "\n",
        encoding="utf-8",
    )


def _record_duration(agent, task_type, seconds):
    """Record an actual completion time for an agent/task_type pair."""
    h = _load_timeout_history()
    durations = h.setdefault("durations", {})
    agent_d = durations.setdefault(agent, {})
    task_list = agent_d.setdefault(task_type, [])
    task_list.append(round(seconds, 1))
    # Keep only last N samples
    if len(task_list) > TIMEOUT_HISTORY_MAX_SAMPLES:
        agent_d[task_type] = task_list[-TIMEOUT_HISTORY_MAX_SAMPLES:]
    _save_timeout_history(h)


def _get_history_p80(agent, task_type):
    """Get p80 of historical durations for agent/task_type. Returns None if no data."""
    h = _load_timeout_history()
    samples = h.get("durations", {}).get(agent, {}).get(task_type, [])
    if len(samples) < 3:
        return None  # not enough data
    sorted_s = sorted(samples)
    idx = int(len(sorted_s) * 0.8)
    return sorted_s[min(idx, len(sorted_s) - 1)]


def _get_escalation(issue_key):
    """Get the per-issue escalation multiplier (grows on repeated failures)."""
    h = _load_timeout_history()
    return h.get("escalations", {}).get(str(issue_key), 1.0)


def _bump_escalation(issue_key):
    """Increase per-issue escalation after failure/timeout. Returns new value."""
    h = _load_timeout_history()
    esc = h.setdefault("escalations", {})
    key = str(issue_key)
    current = esc.get(key, 1.0)
    new_val = min(current * TIMEOUT_ESCALATION_FACTOR, TIMEOUT_MAX_MULTIPLIER)
    esc[key] = round(new_val, 2)
    _save_timeout_history(h)
    return new_val


def _complexity_base_timeout(agent, task_type, triage=None):
    """Estimate base timeout from task complexity.
    triage dict may contain: estimated_files, priority, decision."""
    # Triage tasks are always fast — agents just analyze text
    if task_type == "triage":
        return 120 if agent == "codex" else 90

    # Implementation: scale with estimated file count and priority
    n_files = 0
    priority = "P3-enhancement"
    if triage:
        n_files = len(triage.get("estimated_files") or [])
        priority = triage.get("priority", "P3-enhancement")

    # Base by file count
    if n_files <= 1:
        base = 300
    elif n_files <= 3:
        base = 600
    else:
        base = 900

    # Priority multiplier (critical/security issues need more careful analysis)
    if priority in ("P0-critical", "P1-security"):
        base = int(base * 1.5)
    elif priority == "P2-bug":
        base = int(base * 1.2)

    # Agent speed factor: codex with xhigh is slower than gemini/claude
    if agent == "codex":
        base = int(base * 1.3)

    return base


def _estimate_timeout(agent, task_type, issue_key=None, triage=None,
                      chain_escalation=1.0):
    """Compute final timeout combining:
    1. Complexity-based estimate
    2. Historical p80 (if enough data)
    3. Per-issue escalation (from previous failures)
    4. Within-chain escalation (from current run failures)

    Returns int seconds, clamped to [TIMEOUT_MIN, TIMEOUT_MAX]."""
    # Step 1: complexity estimate
    complexity = _complexity_base_timeout(agent, task_type, triage)

    # Step 2: history — use p80 if available and higher than complexity
    p80 = _get_history_p80(agent, task_type)
    if p80 is not None:
        # Use p80 * 1.3 as headroom (we want to be above p80, not at it)
        history_based = int(p80 * 1.3)
        base = max(complexity, history_based)
    else:
        base = complexity

    # Step 3: per-issue escalation (from past failures on this specific issue)
    issue_esc = _get_escalation(issue_key) if issue_key else 1.0

    # Step 4: chain escalation (from timeouts earlier in THIS run)
    final = int(base * issue_esc * chain_escalation)

    # Clamp
    final = max(TIMEOUT_MIN, min(final, TIMEOUT_MAX))
    return final


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
            "current_pass": 1,
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
            "files_processed": [],
            "last_updated": None,
        }
        self._file_times = []  # track seconds per file for ETA

    def save(self):
        self.data["last_updated"] = _now_iso()
        content = json.dumps(self.data, indent=2, ensure_ascii=False)
        for attempt in range(3):
            try:
                STATUS_FILE.write_text(content, encoding="utf-8")
                return
            except OSError:
                time.sleep(0.5)
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

    def record_file_time(self, seconds):
        self._file_times.append(seconds)

    def eta_str(self, remaining_files):
        if not self._file_times:
            return "?"
        avg = sum(self._file_times) / len(self._file_times)
        eta_s = int(avg * remaining_files)
        return str(datetime.timedelta(seconds=eta_s))

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


def _restore_triage_contamination(modified):
    """After triage timeout, revert any files the agent modified.
    Triage is read-only — it should only return JSON, never touch files."""
    if not modified:
        return
    # Keep chain-context files (they are ours), restore everything else
    restore = [f for f in modified
               if not f.startswith(".project-roadmap/scripts/chain-context/")]
    if restore:
        try:
            subprocess.run(
                ["git", "checkout", "--"] + restore,
                capture_output=True, timeout=10,
                cwd=str(REPO_ROOT),
            )
            log.info("    [CHAIN] Restored %d contaminated files after triage timeout",
                     len(restore))
        except Exception as e:
            log.warning("    [CHAIN] Could not restore contaminated files: %s", e)


def _kill_process_tree(pid):
    """Kill a process and all its children on Windows using taskkill /T."""
    try:
        subprocess.run(
            ["taskkill", "//F", "//T", "//PID", str(pid)],
            capture_output=True, timeout=15,
        )
    except Exception:
        pass


def _run_with_timeout(cmd, timeout, cwd=None, env=None, stdin_path=None):
    """Run a subprocess with reliable timeout on Windows.

    Uses Popen + CREATE_NEW_PROCESS_GROUP so we can kill the entire process
    tree on timeout (fixes the pipe-inheritance hang with subprocess.run).

    Returns (returncode, stdout, stderr) on success.
    Raises subprocess.TimeoutExpired on timeout.
    Raises Exception on other errors.
    """
    creationflags = 0
    if sys.platform == "win32":
        creationflags = subprocess.CREATE_NEW_PROCESS_GROUP

    stdin_file = None
    try:
        if stdin_path:
            stdin_file = open(stdin_path, "r", encoding="utf-8")

        proc = subprocess.Popen(
            cmd,
            stdout=subprocess.PIPE,
            stderr=subprocess.PIPE,
            stdin=stdin_file,
            text=True,
            cwd=cwd,
            env=env,
            encoding="utf-8",
            errors="replace",
            creationflags=creationflags,
        )

        try:
            stdout, stderr = proc.communicate(timeout=timeout)
            return (proc.returncode, stdout or "", stderr or "")
        except subprocess.TimeoutExpired:
            # Kill the ENTIRE process tree, not just the root
            _kill_process_tree(proc.pid)
            # Give children a moment to die, then collect any partial output
            try:
                stdout, stderr = proc.communicate(timeout=10)
            except (subprocess.TimeoutExpired, Exception):
                proc.kill()
                stdout, stderr = "", ""
            raise subprocess.TimeoutExpired(
                cmd, timeout, output=stdout, stderr=stderr
            )
    finally:
        if stdin_file:
            stdin_file.close()


def _now_iso():
    return datetime.datetime.now().isoformat(timespec="seconds")


def _capture_post_timeout_state():
    """After a timeout, capture what the agent modified in the working tree.
    Returns (modified_files: list[str], diff_summary: str)."""
    modified = []
    diff_summary = ""
    try:
        r = subprocess.run(
            ["git", "diff", "--name-only"],
            capture_output=True, text=True, timeout=10,
            cwd=str(REPO_ROOT), encoding="utf-8", errors="replace",
        )
        if r.stdout:
            modified = [f.strip() for f in r.stdout.strip().splitlines() if f.strip()]
    except Exception:
        pass
    try:
        r = subprocess.run(
            ["git", "diff", "--stat"],
            capture_output=True, text=True, timeout=10,
            cwd=str(REPO_ROOT), encoding="utf-8", errors="replace",
        )
        if r.stdout:
            diff_summary = r.stdout.strip()[:1000]
    except Exception:
        pass
    return modified, diff_summary


class ChainContext:
    """Accumulates attempts from each agent in a chain run for JSON handoff."""

    def __init__(self, task_type, task_id):
        self.task_type = task_type
        self.task_id = task_id
        self.attempts = []
        self.timeout_count = 0      # how many agents timed out in this run
        self.all_timed_out = False   # True if every agent timed out
        self.started_at = _now_iso()

    def add_attempt(self, agent, task, success, result=None, raw_output=None,
                    errors=None, files_modified=None, build_result=None,
                    test_result=None, timed_out=False, diff_summary=None):
        self.attempts.append({
            "agent": agent,
            "task": task,
            "success": success,
            "result": result,
            "raw_output": (raw_output or "")[:2000],
            "errors": errors,
            "files_modified": files_modified or [],
            "build_result": build_result,
            "test_result": test_result,
            "timed_out": timed_out,
            "diff_summary": diff_summary,
            "timestamp": _now_iso(),
        })
        if timed_out:
            self.timeout_count += 1

    def to_dict(self):
        self.all_timed_out = (
            self.timeout_count > 0
            and self.timeout_count == len(self.attempts)
            and not any(a["success"] for a in self.attempts)
        )
        return {
            "task_type": self.task_type,
            "task_id": self.task_id,
            "started_at": self.started_at,
            "finished_at": _now_iso(),
            "attempts": self.attempts,
            "timeout_count": self.timeout_count,
            "all_timed_out": self.all_timed_out,
            "final_success": any(a["success"] for a in self.attempts),
        }

    def save(self):
        CHAIN_CONTEXT_DIR.mkdir(parents=True, exist_ok=True)
        ts = datetime.datetime.now().strftime("%Y%m%d_%H%M%S")
        fname = f"{ts}_{self.task_type}_{self.task_id}.json"
        path = CHAIN_CONTEXT_DIR / fname
        path.write_text(
            json.dumps(self.to_dict(), indent=2, ensure_ascii=False) + "\n",
            encoding="utf-8",
        )
        log.info("    [CHAIN] Context saved to %s", fname)
        return path

    def format_for_prompt(self):
        """Format accumulated attempts as context for the next agent in the chain."""
        if not self.attempts:
            return ""
        lines = ["=== PREVIOUS AGENT ATTEMPTS ==="]
        for i, a in enumerate(self.attempts, 1):
            status = "TIMEOUT" if a.get("timed_out") else ("SUCCESS" if a["success"] else "FAILED")
            lines.append(f"\n--- Attempt {i}: {a['agent'].upper()} [{status}] ---")
            lines.append(f"Task: {a['task']}")
            if a.get("timed_out"):
                lines.append("NOTE: This agent TIMED OUT. It may have done partial work.")
                lines.append("Check the working tree — some files may already be modified.")
            if a.get("errors"):
                lines.append(f"Errors: {a['errors']}")
            if a.get("build_result"):
                lines.append(f"Build: {a['build_result']}")
            if a.get("test_result"):
                lines.append(f"Tests: {a['test_result']}")
            if a.get("files_modified"):
                lines.append(f"Files modified: {', '.join(a['files_modified'])}")
            if a.get("diff_summary"):
                lines.append(f"Diff summary:\n{a['diff_summary']}")
            if a.get("raw_output"):
                lines.append(f"Output (truncated):\n{a['raw_output'][:1000]}")
        lines.append("\n=== END PREVIOUS ATTEMPTS ===")
        return "\n".join(lines)


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
    """Extract a JSON object from Claude/Gemini output (may be wrapped in envelope)."""
    if not text:
        return None

    # Step 1: Try parsing the whole thing as JSON
    parsed = None
    try:
        parsed = json.loads(text)
    except (json.JSONDecodeError, TypeError):
        pass

    # Step 2a: If it's a Claude --output-format json envelope, unwrap it
    if isinstance(parsed, dict) and parsed.get("type") == "result" and "result" in parsed:
        inner = parsed["result"]
        if isinstance(inner, dict):
            return inner
        if isinstance(inner, str):
            return _extract_json(inner)
        return None

    # Step 2b: If it's a Gemini -o json envelope, unwrap it
    if isinstance(parsed, dict) and "response" in parsed and "session_id" in parsed:
        inner = parsed["response"]
        if isinstance(inner, dict):
            return inner
        if isinstance(inner, str):
            return _extract_json(inner)
        return None

    # Step 2c: If it's a Codex structured output envelope, unwrap it
    if isinstance(parsed, dict) and "output" in parsed and "model" in parsed:
        inner = parsed["output"]
        if isinstance(inner, dict):
            return inner
        if isinstance(inner, str):
            return _extract_json(inner)
        return None

    # Step 3: If it parsed as a dict with our expected keys, return it
    if isinstance(parsed, dict) and "decision" in parsed:
        return parsed

    # Step 4: Regex — find a JSON object with "decision" key in text
    m = re.search(r"\{[^{}]*\"decision\"[^{}]*\}", text, re.DOTALL)
    if m:
        try:
            return json.loads(m.group())
        except json.JSONDecodeError:
            pass

    # Step 5: Regex — find JSON in markdown code block
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
    kill_stale_processes()
    try:
        r = _run(BUILD_CMD, timeout=BUILD_TIMEOUT)
        full = (r.stdout or "") + "\n" + (r.stderr or "")
        ok = r.returncode == 0
        if not ok:
            log.error("    [BUILD] FAILED (exit %d)", r.returncode)
        else:
            log.info("    [BUILD] OK")
        kill_stale_processes()
        return (ok, full) if capture_output else (ok, None)
    except subprocess.TimeoutExpired:
        log.error("    [BUILD] TIMEOUT (%ds)", BUILD_TIMEOUT)
        kill_stale_processes()
        return (False, None)
    except Exception as e:
        log.error("    [BUILD] ERROR: %s", e)
        kill_stale_processes()
        return (False, None)


def run_tests():
    """Run non-UI tests via run-tests.ps1 (4 parallel processes).
    Returns True if all pass."""
    log.info("    [TEST] Running parallel tests (run-tests.ps1 -NoBuild) ...")
    kill_stale_processes()

    try:
        r = _run(TEST_CMD, timeout=TEST_TIMEOUT)
        out = (r.stdout or "") + "\n" + (r.stderr or "")

        # Parse run-tests.ps1 output: "Total: 1926/1926 passed, 0 failed"
        total_m = re.search(r"Total:\s+(\d+)/(\d+)\s+passed,\s+(\d+)\s+failed", out)
        if total_m:
            passed, total, failed = int(total_m.group(1)), int(total_m.group(2)), int(total_m.group(3))
            if failed > 0:
                log.error("    [TEST] FAILED: %d/%d passed, %d failed", passed, total, failed)
                return False
            log.info("    [TEST] OK (%d/%d passed, parallel)", passed, total)
            kill_stale_processes()
            return True

        # Fallback: parse single-process dotnet test output
        if "Failed!" in out or r.returncode != 0:
            m = re.search(r"Failed:\s+(\d+)", out)
            if m and int(m.group(1)) > 0:
                log.error("    [TEST] FAILED: %s", m.group(0))
                return False
        m = re.search(r"Passed:\s+(\d+)", out)
        if m:
            log.info("    [TEST] OK (%s passed)", m.group(1))

        # Also check for "ALL TESTS PASSED" from run-tests.ps1
        if "ALL TESTS PASSED" in out:
            log.info("    [TEST] OK (all tests passed)")
            kill_stale_processes()
            return True
        # Check for "TESTS FAILED" from run-tests.ps1
        if "TESTS FAILED" in out:
            log.error("    [TEST] FAILED (run-tests.ps1 reported failure)")
            kill_stale_processes()
            return False

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
        co_authors = []
        if "codex" in _session_agents_used:
            co_authors.append(f"Co-Authored-By: Codex ({CODEX_MODEL}) <noreply@openai.com>")
        if "claude" in _session_agents_used:
            co_authors.append("Co-Authored-By: Claude Opus 4.6 <noreply@anthropic.com>")
        if "gemini" in _session_agents_used:
            co_authors.append(f"Co-Authored-By: Gemini ({GEMINI_MODEL}) <noreply@google.com>")
        co_author_str = "\n".join(co_authors) if co_authors else "Co-Authored-By: AI Agent <noreply@example.com>"
        full_msg = f"{message}\n\n{co_author_str}"
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
    """Revert all uncommitted changes (except .project-roadmap/ and run-tests.ps1)."""
    try:
        # Restore only source code, not orchestrator scripts or issues-db
        _run(["git", "checkout", "--", "mRemoteNG/", "mRemoteNGTests/", "mRemoteNGSpecs/"])
        _run(["git", "clean", "-fd", "--", "mRemoteNG/", "mRemoteNGTests/", "mRemoteNGSpecs/"])
        log.info("    [GIT] Reverted source code changes")
    except Exception as e:
        log.warning("    [GIT] Restore failed: %s", e)


def git_squash_last(n, message):
    """Squash last N commits into one with given message."""
    if n <= 1:
        return
    try:
        co_authors = []
        if "codex" in _session_agents_used:
            co_authors.append(f"Co-Authored-By: Codex ({CODEX_MODEL}) <noreply@openai.com>")
        if "claude" in _session_agents_used:
            co_authors.append("Co-Authored-By: Claude Opus 4.6 <noreply@anthropic.com>")
        if "gemini" in _session_agents_used:
            co_authors.append(f"Co-Authored-By: Gemini ({GEMINI_MODEL}) <noreply@google.com>")
        co_author_str = "\n".join(co_authors) if co_authors else "Co-Authored-By: AI Agent <noreply@example.com>"
        full_msg = f"{message}\n\n{co_author_str}"
        _run(["git", "reset", "--soft", f"HEAD~{n}"])
        _run(["git", "commit", "-m", full_msg])
        r = _run(["git", "rev-parse", "HEAD"])
        h = (r.stdout or "").strip()
        log.info("    [GIT] Squashed %d commits into %s", n, h[:8])
        return h
    except Exception as e:
        log.error("    [GIT] Squash failed: %s", e)
        return None


# ── CORE: CLAUDE SUB-AGENT ─────────────────────────────────────────────────
def claude_run(prompt, max_turns=15, json_output=False, timeout=CLAUDE_TIMEOUT,
               retries=CLAUDE_RETRIES):
    """Call claude -p (headless) with retry.  Returns stdout string.
    Uses CLAUDE_ENV to strip CLAUDECODE nesting guard."""
    cmd = ["claude", "-p", prompt, "--max-turns", str(max_turns)]
    if json_output:
        cmd += ["--output-format", "json"]

    for attempt in range(1, retries + 1):
        try:
            rc, stdout, stderr = _run_with_timeout(
                cmd, timeout=timeout, cwd=str(REPO_ROOT), env=CLAUDE_ENV,
            )
            kill_stale_processes()
            if rc != 0:
                err_detail = (stderr or "")[:200] or (stdout or "")[:200] or "(empty output)"
                log.error("    [CLAUDE] attempt %d/%d exit %d: %s",
                          attempt, retries, rc, err_detail)
                if attempt < retries:
                    log.info("    [CLAUDE] Retrying in 5s ...")
                    time.sleep(5)
                    continue
                return None
            return stdout or ""
        except subprocess.TimeoutExpired as exc:
            global _last_dispatch_timed_out, _last_dispatch_partial_output
            _last_dispatch_timed_out = True
            log.error("    [CLAUDE] attempt %d/%d TIMEOUT (%ds)", attempt, retries, timeout)
            kill_stale_processes()
            partial = ""
            if hasattr(exc, "output") and exc.output:
                partial = exc.output if isinstance(exc.output, str) else exc.output.decode("utf-8", errors="replace")
            _last_dispatch_partial_output = (partial or "")[:3000]
            if partial:
                log.info("    [CLAUDE] Captured %d chars of partial output before timeout", len(partial))
            if attempt < retries:
                log.info("    [CLAUDE] Retrying in 5s ...")
                time.sleep(5)
                continue
            return None
        except Exception as e:
            log.error("    [CLAUDE] attempt %d/%d ERROR: %s", attempt, retries, e)
            kill_stale_processes()
            if attempt < retries:
                time.sleep(5)
                continue
            return None
    return None


# ── CORE: GEMINI SUB-AGENT ────────────────────────────────────────────────
def gemini_run(prompt, max_turns=15, json_output=False, timeout=CLAUDE_TIMEOUT,
               retries=CLAUDE_RETRIES):
    """Call gemini -p (headless) with retry.  Returns stdout string.
    Uses -y for auto-approve, -m for model selection."""
    cmd = [GEMINI_CMD, "-p", prompt, "-y", "-m", GEMINI_MODEL]
    if json_output:
        cmd += ["-o", "json"]

    for attempt in range(1, retries + 1):
        try:
            rc, stdout, stderr = _run_with_timeout(
                cmd, timeout=timeout, cwd=str(REPO_ROOT),
            )
            kill_stale_processes()
            if rc != 0:
                err_detail = (stderr or "")[:200] or (stdout or "")[:200] or "(empty output)"
                log.error("    [GEMINI] attempt %d/%d exit %d: %s",
                          attempt, retries, rc, err_detail)
                if attempt < retries:
                    log.info("    [GEMINI] Retrying in 5s ...")
                    time.sleep(5)
                    continue
                return None
            return stdout or ""
        except subprocess.TimeoutExpired as exc:
            global _last_dispatch_timed_out, _last_dispatch_partial_output
            _last_dispatch_timed_out = True
            log.error("    [GEMINI] attempt %d/%d TIMEOUT (%ds)", attempt, retries, timeout)
            kill_stale_processes()
            partial = ""
            if hasattr(exc, "output") and exc.output:
                partial = exc.output if isinstance(exc.output, str) else exc.output.decode("utf-8", errors="replace")
            _last_dispatch_partial_output = (partial or "")[:3000]
            if partial:
                log.info("    [GEMINI] Captured %d chars of partial output before timeout", len(partial))
            if attempt < retries:
                log.info("    [GEMINI] Retrying in 5s ...")
                time.sleep(5)
                continue
            return None
        except Exception as e:
            log.error("    [GEMINI] attempt %d/%d ERROR: %s", attempt, retries, e)
            kill_stale_processes()
            if attempt < retries:
                time.sleep(5)
                continue
            return None
    return None


# ── CORE: CODEX SUB-AGENT ─────────────────────────────────────────────────
def _extract_codex_last_message(jsonl_output):
    """Parse JSONL events from codex stdout, extract last assistant message."""
    last_msg = None
    for line in jsonl_output.splitlines():
        line = line.strip()
        if not line:
            continue
        try:
            event = json.loads(line)
            # Codex structured output: look for assistant messages
            if isinstance(event, dict):
                if event.get("role") == "assistant" and event.get("content"):
                    last_msg = event["content"]
                elif event.get("type") == "message" and event.get("content"):
                    last_msg = event["content"]
                # Also handle text content blocks
                if isinstance(event.get("content"), list):
                    for block in event["content"]:
                        if isinstance(block, dict) and block.get("text"):
                            last_msg = block["text"]
        except (json.JSONDecodeError, TypeError):
            continue
    return last_msg


def codex_run(prompt, timeout=CODEX_TIMEOUT, retries=CODEX_RETRIES):
    """Call codex exec (headless) with retry. Returns stdout string or None.
    Uses temp file for prompt via stdin, -o for output capture."""
    import tempfile

    for attempt in range(1, retries + 1):
        prompt_file = None
        output_file = None
        try:
            # Write prompt to temp file
            pf = tempfile.NamedTemporaryFile(mode="w", suffix=".txt", delete=False,
                                             encoding="utf-8",
                                             dir=str(SCRIPTS_DIR))
            pf.write(prompt)
            pf.close()
            prompt_file = pf.name

            # Create output temp file
            of = tempfile.NamedTemporaryFile(mode="w", suffix=".jsonl", delete=False,
                                             encoding="utf-8",
                                             dir=str(SCRIPTS_DIR))
            of.close()
            output_file = of.name

            cmd = [
                CODEX_CMD, "exec", "-",
                "--full-auto",
                "--color", "never",
                "--ephemeral",
                "-m", CODEX_MODEL,
                "-c", f'model_reasoning_effort="{CODEX_REASONING}"',
                "-C", str(REPO_ROOT),
                "-o", output_file,
            ]

            rc, stdout, stderr = _run_with_timeout(
                cmd, timeout=timeout, cwd=str(REPO_ROOT),
                stdin_path=prompt_file,
            )

            kill_stale_processes()

            if rc != 0:
                err_detail = (stderr or "")[:200] or (stdout or "")[:200] or "(empty output)"
                log.error("    [CODEX] attempt %d/%d exit %d: %s",
                          attempt, retries, rc, err_detail)
                if attempt < retries:
                    log.info("    [CODEX] Retrying in 10s ...")
                    time.sleep(10)
                    continue
                return None

            # Primary: read from -o output file
            result = None
            try:
                out_content = Path(output_file).read_text(encoding="utf-8", errors="replace")
                if out_content.strip():
                    # Try to extract last assistant message from JSONL
                    msg = _extract_codex_last_message(out_content)
                    if msg:
                        result = msg
                    else:
                        result = out_content
            except Exception:
                pass

            # Fallback: parse stdout JSONL
            if not result and stdout:
                msg = _extract_codex_last_message(stdout)
                if msg:
                    result = msg
                else:
                    result = stdout

            return result or ""

        except subprocess.TimeoutExpired as exc:
            global _last_dispatch_timed_out, _last_dispatch_partial_output
            _last_dispatch_timed_out = True
            log.error("    [CODEX] attempt %d/%d TIMEOUT (%ds)", attempt, retries, timeout)
            kill_stale_processes()
            # Capture partial output from -o file (agent may have written progress)
            partial = ""
            if output_file:
                try:
                    partial = Path(output_file).read_text(encoding="utf-8", errors="replace")
                except Exception:
                    pass
            # Also grab partial stdout from the exception
            if not partial and hasattr(exc, "output") and exc.output:
                partial = exc.output if isinstance(exc.output, str) else exc.output.decode("utf-8", errors="replace")
            _last_dispatch_partial_output = (partial or "")[:3000]
            if partial:
                log.info("    [CODEX] Captured %d chars of partial output before timeout", len(partial))
            if attempt < retries:
                log.info("    [CODEX] Retrying in 10s ...")
                time.sleep(10)
                continue
            return None
        except Exception as e:
            log.error("    [CODEX] attempt %d/%d ERROR: %s", attempt, retries, e)
            kill_stale_processes()
            if attempt < retries:
                time.sleep(10)
                continue
            return None
        finally:
            for fpath in (prompt_file, output_file):
                if fpath:
                    try:
                        os.unlink(fpath)
                    except OSError:
                        pass
    return None


# ── CORE: AGENT DISPATCH HELPER ──────────────────────────────────────────
def _agent_dispatch(agent, prompt, max_turns=15, json_output=False,
                    timeout=CLAUDE_TIMEOUT, retries=CLAUDE_RETRIES):
    """Dispatch a prompt to a specific agent. Returns stdout string or None.
    Sets _last_dispatch_timed_out if the agent timed out."""
    global _last_dispatch_timed_out, _last_dispatch_partial_output
    _last_dispatch_timed_out = False
    _last_dispatch_partial_output = ""
    _session_agents_used.add(agent)

    if agent == "codex":
        return codex_run(prompt, timeout=timeout, retries=min(retries, CODEX_RETRIES))

    if agent == "gemini":
        prompt_file = _write_prompt_file(prompt)
        try:
            rc, stdout, stderr = _run_with_timeout(
                [GEMINI_CMD, "-p", "", "-y", "-m", GEMINI_MODEL],
                timeout=timeout, cwd=str(REPO_ROOT),
                stdin_path=prompt_file,
            )
            kill_stale_processes()
            if rc == 0 and stdout:
                return stdout
            err_detail = (stderr or "")[:200] or (stdout or "")[:200] or "(empty)"
            log.error("    [GEMINI] dispatch exit %d: %s", rc, err_detail)
            return None
        except subprocess.TimeoutExpired:
            log.error("    [GEMINI] dispatch TIMEOUT (%ds)", timeout)
            kill_stale_processes()
            return None
        except Exception as e:
            log.error("    [GEMINI] dispatch ERROR: %s", e)
            kill_stale_processes()
            return None
        finally:
            try:
                os.unlink(prompt_file)
            except OSError:
                pass

    # Default: claude
    return claude_run(prompt, max_turns=max_turns, json_output=json_output,
                      timeout=timeout, retries=retries)


# ── CORE: AGENT SIMPLE DISPATCH (for warnings) ───────────────────────────
def agent_run(task_type, prompt, max_turns=15, json_output=False,
              timeout=CLAUDE_TIMEOUT, retries=CLAUDE_RETRIES):
    """3-tier dispatch: try configured agent, then fallback through AGENT_CHAIN.
    Used for warning fixes and simple tasks."""
    primary = AGENT_CONFIG.get(task_type, "codex")

    # Build chain: primary first, then remaining agents from AGENT_CHAIN
    chain = [primary] + [a for a in AGENT_CHAIN if a != primary]

    for i, agent in enumerate(chain):
        is_primary = (i == 0)

        # Only primary gets full retries; fallback agents get 1
        agent_retries = retries if is_primary else 1
        agent_timeout = timeout
        if agent == "codex":
            agent_timeout = max(timeout, CODEX_TIMEOUT)
            agent_retries = min(agent_retries, CODEX_RETRIES)

        log.info("    [AGENT] Trying %s for %s (attempt %d/%d in chain)",
                 agent, task_type, i + 1, len(chain))

        result = _agent_dispatch(agent, prompt, max_turns=max_turns,
                                 json_output=json_output, timeout=agent_timeout,
                                 retries=agent_retries)
        if result:
            return result

        # Don't fallback if disabled
        if not AGENT_FALLBACK_ENABLED:
            log.warning("    [AGENT] %s failed for %s — fallback disabled", agent, task_type)
            return None

        log.warning("    [AGENT] %s failed for %s — trying next in chain", agent, task_type)

    log.error("    [AGENT] All agents failed for %s", task_type)
    return None


# ── CORE: AGENT CHAIN ─────────────────────────────────────────────────────
def _write_prompt_file(prompt):
    """Write prompt to a temp file (Gemini handles long prompts better via stdin)."""
    import tempfile
    f = tempfile.NamedTemporaryFile(mode="w", suffix=".txt", delete=False,
                                    encoding="utf-8", dir=str(REPO_ROOT / ".project-roadmap" / "scripts"))
    f.write(prompt)
    f.close()
    return f.name


def chain_triage(issue):
    """Chain-of-agents triage: loops through AGENT_CHAIN until valid JSON.
    Each subsequent agent gets context from previous attempts.
    Returns (triage_dict, agent_used) or (None, None)."""
    num = issue["number"]
    title = issue.get("title", "")
    body = (issue.get("body") or "")[:2000]
    labels = ", ".join(issue.get("labels", []))
    comments = issue.get("comments", [])[-3:]
    comments_text = "\n".join(
        f"  [{c.get('author', '?')}]: {c.get('snippet', '')[:300]}" for c in comments
    )

    triage_prompt = f"""IMPORTANT: This is a READ-ONLY classification task. Do NOT modify any files.
Do NOT edit JSON files. Do NOT run scripts. Do NOT update any database.
ONLY read the issue below and return the JSON classification.

Triage this GitHub issue for mRemoteNG (.NET 10, WinForms, remote connections manager).

Issue #{num}: {title}
Labels: {labels}
State: {issue.get('state', 'open')}

Body:
{body}

Recent comments:
{comments_text}

Reply with ONLY a JSON object (no other text):
{{"decision":"implement","reason":"one sentence","priority":"P2-bug","estimated_files":["path.cs"],"approach":"brief fix"}}

Decisions: implement, wontfix, duplicate, needs_info
Priorities: P0-critical, P1-security, P2-bug, P3-enhancement, P4-debt
If unclear, use needs_info."""

    ctx = ChainContext("triage", str(num))
    issue_key = f"triage_{num}"
    chain_esc = 1.0  # grows within this run on each timeout

    for i, agent in enumerate(AGENT_CHAIN):
        _session_agents_used.add(agent)

        if i == 0:
            prompt = triage_prompt
        else:
            prompt = f"""You are refining a triage result for mRemoteNG issue #{num}: {title}

Issue body:
{body[:1000]}

{ctx.format_for_prompt()}

Extract or produce the correct triage JSON from the above attempts, or if they are unusable,
analyze the issue yourself and produce the triage.

Reply with ONLY a JSON object:
{{"decision":"implement|wontfix|duplicate|needs_info","reason":"one sentence","priority":"P0-critical|P1-security|P2-bug|P3-enhancement|P4-debt","estimated_files":["path.cs"],"approach":"brief fix"}}"""

        timeout = _estimate_timeout(agent, "triage", issue_key=issue_key,
                                    chain_escalation=chain_esc)
        log.info("    [CHAIN] Step %d: %s triage for #%d (timeout=%ds)",
                 i + 1, agent.capitalize(), num, timeout)

        t0 = time.time()
        raw_output = _agent_dispatch(agent, prompt, max_turns=3, timeout=timeout, retries=1)
        elapsed = time.time() - t0
        kill_stale_processes()

        if _last_dispatch_timed_out:
            modified, diff_summary = _capture_post_timeout_state()
            ctx.add_attempt(agent, "triage", False,
                            raw_output=_last_dispatch_partial_output,
                            errors=f"TIMEOUT after {timeout}s",
                            files_modified=modified, timed_out=True)
            if modified:
                log.info("    [CHAIN] %s timed out but modified %d files: %s",
                         agent, len(modified), ", ".join(modified[:5]))
                # CRITICAL: Clean up contaminated files so next agent starts clean
                _restore_triage_contamination(modified)
            chain_esc *= TIMEOUT_ESCALATION_FACTOR
        elif raw_output:
            # Record successful duration for future estimates
            _record_duration(agent, "triage", elapsed)
            log.info("    [CHAIN] %s raw (%d chars, %.0fs): %s",
                     agent, len(raw_output), elapsed,
                     raw_output[:150].replace("\n", " "))
            result = _extract_json(raw_output)
            if result and "decision" in result:
                ctx.add_attempt(agent, "triage", True, result=result, raw_output=raw_output)
                ctx.save()
                log.info("    [CHAIN] %s triage OK for #%d", agent.capitalize(), num)
                return result, agent
            else:
                ctx.add_attempt(agent, "triage", False, raw_output=raw_output,
                                errors="Could not extract valid JSON")
        else:
            ctx.add_attempt(agent, "triage", False, errors="Agent returned None")
            # Clean up any files modified even without timeout
            modified_check, _ = _capture_post_timeout_state()
            if modified_check:
                _restore_triage_contamination(modified_check)

        if not AGENT_FALLBACK_ENABLED:
            break

        log.warning("    [CHAIN] %s failed triage for #%d — trying next agent", agent, num)

    ctx.save()
    # Bump per-issue escalation if agents timed out (for next session)
    if ctx.timeout_count > 0:
        new_mult = _bump_escalation(issue_key)
        log.warning("    [CHAIN] %d timeouts for #%d — next run escalation: %.1fx",
                     ctx.timeout_count, num, new_mult)
    log.error("    [CHAIN] All agents failed triage for #%d", num)
    return None, None


def chain_implement(issue, triage, status):
    """Chain-of-agents implementation: loops through AGENT_CHAIN.
    Each agent gets context from previous attempts. If build/test fail,
    leaves modifications in working tree for next agent.
    Returns True if fix was committed."""
    num = issue["number"]
    title = issue.get("title", "")
    body = (issue.get("body") or "")[:3000]
    approach = triage.get("approach", "")
    files = triage.get("estimated_files", [])

    impl_prompt = f"""Project: mRemoteNG (.NET 10, WinForms, COM references)
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
- After build passes, run tests: powershell.exe -NoProfile -ExecutionPolicy Bypass -File "D:\\github\\mRemoteNG\\run-tests.ps1" -NoBuild
- If YOUR change breaks tests, fix it.  If tests fail for unrelated reasons, ignore.
- Do ONLY the fix.  Nothing else."""

    ctx = ChainContext("implement", str(num))
    issue_key = f"impl_{num}"
    chain_esc = 1.0  # grows within this run on each timeout

    for i, agent in enumerate(AGENT_CHAIN):
        is_last = (i == len(AGENT_CHAIN) - 1)
        _session_agents_used.add(agent)

        # Build prompt: first agent gets base, others get base + chain context
        if i == 0:
            prompt = impl_prompt
        else:
            prompt = f"""Project: mRemoteNG (.NET 10, WinForms, COM references)
Working directory: D:\\github\\mRemoteNG
Branch: main

Fix GitHub issue #{num}: {title}
Description: {body[:2000]}
Approach: {approach}
Likely files: {', '.join(files) if files else 'search the codebase'}

IMPORTANT — Previous agents already attempted this fix.
Their changes may still be in the working tree.

{ctx.format_for_prompt()}

Review the current state of the code, correct any issues, and make the fix work.

RULES: Read code first. Do NOT change existing behavior. Run build.ps1 then run-tests.ps1 -NoBuild.
Do ONLY the fix. Nothing else."""

        timeout = _estimate_timeout(agent, "implement", issue_key=issue_key,
                                    triage=triage, chain_escalation=chain_esc)
        log.info("  [CHAIN] Step %d: %s implementing #%d (timeout=%ds) ...",
                 i + 1, agent.capitalize(), num, timeout)
        status.set_task(type="issue_fix", issue=num, step=f"{agent}_fixing")

        t0 = time.time()
        agent_out = _agent_dispatch(agent, prompt, max_turns=25, timeout=timeout, retries=1)
        elapsed = time.time() - t0
        kill_stale_processes()

        if agent_out is None:
            if _last_dispatch_timed_out:
                # Timeout — capture partial work (files modified, partial output)
                modified, diff_summary = _capture_post_timeout_state()
                ctx.add_attempt(agent, f"implement #{num}", False,
                                raw_output=_last_dispatch_partial_output,
                                errors=f"TIMEOUT after {timeout}s",
                                files_modified=modified, timed_out=True,
                                diff_summary=diff_summary)
                if modified:
                    log.info("  [CHAIN] %s timed out but modified %d files: %s",
                             agent, len(modified), ", ".join(modified[:5]))
                    # Don't restore — leave partial work for next agent
                else:
                    git_restore()
                chain_esc *= TIMEOUT_ESCALATION_FACTOR
            else:
                ctx.add_attempt(agent, f"implement #{num}", False,
                                errors="Agent returned None after retries")
                git_restore()

            if is_last:
                status.add_error(f"issue_{num}", agent, "returned None")
                git_restore()
                ctx.save()
                if ctx.timeout_count > 0:
                    new_mult = _bump_escalation(issue_key)
                    log.warning("  [CHAIN] %d timeouts for #%d — next run escalation: %.1fx",
                                ctx.timeout_count, num, new_mult)
                return False
            if not AGENT_FALLBACK_ENABLED:
                git_restore()
                ctx.save()
                return False
            log.warning("  [CHAIN] %s returned nothing for #%d — next agent", agent, num)
            continue

        # Record successful agent duration for future estimates
        _record_duration(agent, "implement", elapsed)

        # Check build
        status.set_task(type="issue_fix", issue=num, step=f"building_{agent}")
        build_ok, build_output = run_build(capture_output=True)

        if build_ok:
            # Build OK — run tests
            status.set_task(type="issue_fix", issue=num, step=f"testing_{agent}")
            test_ok = run_tests()

            if test_ok:
                # Full success — commit, push, comment
                ctx.add_attempt(agent, f"implement #{num}", True,
                                build_result="OK", test_result="OK")
                ctx.save()

                status.set_task(type="issue_fix", issue=num, step="committing")
                short = (approach or title)[:60]
                msg = f"fix(#{num}): {short}"
                h = git_commit(msg)
                if not h:
                    log.warning("  [CHAIN] No changes to commit for #%d", num)
                    return False
                status.add_commit(h, msg, True)
                status.data["issues"]["implemented"] += 1
                log.info("  [CHAIN] %s fix committed %s", agent.capitalize(), h[:8])

                status.set_task(type="issue_fix", issue=num, step="pushing")
                git_push()
                if post_github_comment(num, h, short):
                    status.data["issues"]["commented_on_github"] += 1
                update_issue_json(num, "testing", f"Fix in {h[:8]}")
                status.clear_task()
                return True
            else:
                # Tests fail — leave changes for next agent to fix
                ctx.add_attempt(agent, f"implement #{num}", False,
                                build_result="OK", test_result="FAIL",
                                errors="Build OK but tests failed. Changes left in working tree.")
                log.warning("  [CHAIN] %s fix builds but tests FAIL for #%d", agent, num)
        else:
            # Build fail — leave changes for next agent to fix
            err_snippet = (build_output or "")[-500:]
            ctx.add_attempt(agent, f"implement #{num}", False,
                            build_result="FAIL", test_result="N/A",
                            errors=f"Build failed:\n{err_snippet}")
            log.warning("  [CHAIN] %s fix has build errors for #%d", agent, num)

        # Last agent failed — revert
        if is_last:
            log.error("  [CHAIN] All agents failed implementation for #%d — reverting", num)
            status.add_error(f"issue_{num}", "chain", "all agents failed")
            git_restore()
            ctx.save()
            return False

        if not AGENT_FALLBACK_ENABLED:
            git_restore()
            ctx.save()
            return False

        log.warning("  [CHAIN] %s failed for #%d — passing to next agent", agent, num)

    ctx.save()
    return False


# ── CORE: GITHUB COMMENTS ──────────────────────────────────────────────────
def post_github_comment(issue_num, commit_hash, description):
    """Post a fix-available comment on upstream issue."""
    beta_tag = get_beta_tag()
    beta_url = get_beta_url()
    comment = (
        f"**Fix available for testing**\n\n"
        f"**Commit:** [`{commit_hash[:8]}`]"
        f"(https://github.com/{FORK_REPO}/commit/{commit_hash})\n"
        f"**Branch:** `main`\n"
        f"**What changed:** {description}\n\n"
        f"**Download latest beta:** [{beta_tag}]({beta_url})\n\n"
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


def update_issue_json(issue_num, new_status, description="", *,
                      priority=None, notes=None):
    """Update issue status in local IIS JSON DB.

    Args:
        priority: e.g. "P2-bug" — written to data["priority"] if provided.
        notes:    appended to data["notes"] if provided.
    """
    json_file = ISSUES_DB_DIR / f"{issue_num:04d}.json"
    if not json_file.exists():
        return
    try:
        data = json.loads(json_file.read_text(encoding="utf-8-sig"))
        data["our_status"] = new_status
        if priority:
            data["priority"] = priority
        if notes:
            prev = (data.get("notes") or "").rstrip()
            data["notes"] = f"{prev}\n{notes}".strip() if prev else notes
        if "iterations" not in data:
            data["iterations"] = []
        data["iterations"].append({
            "seq": len(data["iterations"]) + 1,
            "date": datetime.date.today().isoformat(),
            "type": new_status,
            "description": description,
        })
        json_file.write_text(
            json.dumps(data, indent=2, ensure_ascii=False) + "\n",
            encoding="utf-8",
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


def find_dependents(fpath, all_warnings):
    """Find files that might have cascade warnings from changes to fpath.
    Returns list of (file, warning_count) for files importing types from fpath."""
    basename = os.path.basename(fpath).replace(".cs", "")
    # Extract class/type names from the basename (e.g. ConnectionInfo -> ConnectionInfo)
    dependents = []
    for other_fpath, other_warnings in all_warnings.items():
        if other_fpath == fpath:
            continue
        # Check if any warning message references types from our file
        for w in other_warnings:
            if basename in w["message"]:
                dependents.append(os.path.basename(other_fpath))
                break
    return dependents[:5]  # limit to 5 most relevant


# ── FLUX 1: OPEN ISSUES ────────────────────────────────────────────────────
def load_actionable_issues():
    """Load issues from JSON DB that need triage or implementation.

    Skips issues already triaged by AI (have 'AI triage:' in notes)
    to avoid re-processing on orchestrator restart.
    """
    issues = []
    if not ISSUES_DB_DIR.exists():
        log.warning("Issues DB not found: %s", ISSUES_DB_DIR)
        return issues
    for f in sorted(ISSUES_DB_DIR.glob("*.json")):
        if f.name.startswith("_"):
            continue
        try:
            data = json.loads(f.read_text(encoding="utf-8-sig"))
            status = data.get("our_status", "new")
            if status not in ("new", "triaged", "roadmap"):
                continue
            # Skip issues already processed by AI orchestrator
            notes = data.get("notes") or ""
            if status == "triaged" and "AI triage:" in notes:
                continue
            issues.append(data)
        except Exception:
            pass

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

    prompt = f"""You are an issue triage assistant for the mRemoteNG project (.NET 10, WinForms, remote connections manager).

Your task: analyze this GitHub issue and produce a triage decision as a JSON object.

=== ISSUE DATA ===
Issue #{num}: {title}
Labels: {labels}
State: {issue.get('state', 'open')}

Body:
{body}

Recent comments:
{comments_text}
=== END ISSUE DATA ===

Respond with ONLY a JSON object in this exact format (no other text):
{{"decision":"implement","reason":"one sentence explaining why","priority":"P2-bug","estimated_files":["relative/path.cs"],"approach":"brief fix description"}}

Valid decisions:
- "implement" = clear bug or feature request that we can fix in code
- "wontfix" = by design, obsolete, out of scope, or cannot reproduce
- "duplicate" = already fixed in v1.79.0/v1.80.0/v1.80.1 or duplicate of another issue
- "needs_info" = unclear issue, needs more information from reporter

Valid priorities: "P0-critical", "P1-security", "P2-bug", "P3-enhancement", "P4-debt"

Be conservative: if the issue is unclear, use "needs_info"."""

    out = agent_run("triage", prompt, max_turns=3, json_output=False, timeout=120)
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
    log.info("  [FIX] %s working on #%d ...", AGENT_CONFIG.get("implement", "claude").capitalize(), num)

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
- After build passes, run tests: powershell.exe -NoProfile -ExecutionPolicy Bypass -File "D:\\github\\mRemoteNG\\run-tests.ps1" -NoBuild
- If YOUR change breaks tests, fix it.  If tests fail for unrelated reasons, ignore.
- Do ONLY the fix.  Nothing else."""

    out = agent_run("implement", prompt, max_turns=25, timeout=CLAUDE_TIMEOUT)
    if out is None:
        status.add_error(f"issue_{num}", AGENT_CONFIG.get("implement", "claude"),
                         "returned None after retries")
        git_restore()
        return False

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

    status.set_task(type="sync", step="syncing")
    log.info("  Syncing issues from GitHub ...")
    try:
        iis_sync(repos="both")
    except Exception as e:
        log.warning("  Sync failed: %s", e)

    issues = load_actionable_issues()
    status.data["issues"]["total_synced"] = len(issues)
    status.save()
    log.info("  Found %d actionable issues", len(issues))

    if max_issues:
        issues = issues[:max_issues]

    consecutive_failures = 0
    for i, issue in enumerate(issues, 1):
        num = issue["number"]
        title = issue.get("title", "")[:50]
        log.info("[%d/%d] Issue #%d: %s", i, len(issues), num, title)
        print_progress("ISSUES", i, len(issues), f"#{num} {title}", status)

        status.set_task(type="triage", issue=num, step="analyzing")
        if dry_run:
            log.info("  [DRY RUN] skip triage")
            continue

        # Rate-limit: pause between API calls (2s normal, 30s after failures)
        if i > 1:
            delay = 30 if consecutive_failures >= 3 else 2
            time.sleep(delay)

        triage, triage_agent = chain_triage(issue)
        if not triage:
            status.add_error(f"issue_{num}", "triage", "chain failed (both agents)")
            status.data["issues"]["failed"] += 1
            consecutive_failures += 1
            if consecutive_failures >= 10:
                log.error("  [CIRCUIT BREAKER] %d consecutive failures — stopping triage", consecutive_failures)
                break
            continue

        consecutive_failures = 0  # reset on success
        decision = triage.get("decision", "needs_info")
        ai_priority = triage.get("priority")
        ai_reason = triage.get("reason", "")
        ai_approach = triage.get("approach", "")
        ai_notes = f"AI triage ({triage_agent}): {ai_reason}" + (
            f"\nApproach: {ai_approach}" if ai_approach else ""
        )
        status.data["issues"]["triaged"] += 1
        log.info("  Decision: %s [%s] — %s", decision, triage_agent, ai_reason)

        if decision == "implement":
            status.data["issues"]["to_implement"] += 1
            update_issue_json(num, "triaged", ai_reason,
                              priority=ai_priority, notes=ai_notes)
            chain_implement(issue, triage, status)
        elif decision == "wontfix":
            status.data["issues"]["skipped_wontfix"] += 1
            update_issue_json(num, "wontfix", ai_reason,
                              priority=ai_priority, notes=ai_notes)
        elif decision == "duplicate":
            status.data["issues"]["skipped_duplicate"] += 1
            update_issue_json(num, "duplicate", ai_reason,
                              priority=ai_priority, notes=ai_notes)
        elif decision == "needs_info":
            status.data["issues"]["skipped_needs_info"] += 1
            update_issue_json(num, "triaged", ai_reason,
                              priority=ai_priority, notes=ai_notes)

        status.save()


# ── FLUX 2: WARNING CLEANUP ────────────────────────────────────────────────
def _fix_single_file(fpath, file_warnings, all_warnings, status, squash_mode):
    """Fix warnings in a single file. Returns (success: bool, fixed_count: int)."""
    rel = os.path.relpath(fpath, REPO_ROOT)
    n = len(file_warnings)

    status.set_task(type="warning_fix", file=rel, step="fixing", count=n)

    # Format warnings for Claude
    w_text = "\n".join(
        f"  Line {w['line']}: {w['code']} -- {w['message']}"
        for w in file_warnings[:50]
    )

    # Find dependent files for cascade awareness
    dependents = find_dependents(fpath, all_warnings)
    cascade_hint = ""
    if dependents:
        cascade_hint = (
            f"\n\nCASCADE AWARENESS: Files that use types from this file: "
            f"{', '.join(dependents)}. "
            f"When you add `?` to a field or property, other files that reference it "
            f"will get CS8602 warnings. Make sure your changes are safe for all callers."
        )

    prompt = f"""Project: mRemoteNG (.NET 10, WinForms)
Working directory: D:\\github\\mRemoteNG
File: {rel}

Fix ALL these nullable reference type warnings:
{w_text}

CRITICAL RULES:
- When adding `?` to a field type, check ALL usages in THIS file — add `?.` and `?? default`
- Do NOT generate new CS8602 warnings — fix the cascade immediately
- Do NOT change logic/behavior — only types and null checks
- Getter with .Trim() -> ?.Trim() ?? string.Empty
- Use `= null!` ONLY for fields guaranteed initialized in constructor
- GetPropertyValue pattern: result is TPropertyType typed ? typed : value
- Read the file FIRST, understand context, then fix
- Do NOT create new files or tests{cascade_hint}"""

    out = agent_run("warning_fix", prompt, max_turns=15, timeout=300)
    if out is None:
        status.add_error(rel, AGENT_CONFIG.get("warning_fix", "claude"),
                         "returned None after retries")
        git_restore()
        return False, 0

    # Verify: build
    status.set_task(type="warning_fix", file=rel, step="building")
    build_ok, new_output = run_build(capture_output=True)
    if not build_ok:
        log.error("  Build FAILED for %s — reverting", rel)
        status.add_error(rel, "build", "failed")
        git_restore()
        return False, 0

    # Verify: tests
    status.set_task(type="warning_fix", file=rel, step="testing")
    if not run_tests():
        log.error("  Tests FAILED for %s — reverting", rel)
        status.add_error(rel, "test", "failed")
        git_restore()
        return False, 0

    # Count improvement
    new_warnings = parse_warnings(new_output) if new_output else {}
    new_total = sum(len(v) for v in new_warnings.values())
    fixed = status.data["warnings"]["total_now"] - new_total

    if fixed <= 0:
        log.warning("  No improvement for %s (%d -> %d) — reverting", rel,
                    status.data["warnings"]["total_now"], new_total)
        git_restore()
        return False, 0

    status.data["warnings"]["total_now"] = new_total
    status.data["warnings"]["fixed_this_session"] += fixed

    # Recalculate per-type counts
    new_type_counts = {}
    for file_w in new_warnings.values():
        for w in file_w:
            new_type_counts[w["code"]] = new_type_counts.get(w["code"], 0) + 1
    for code, d in status.data["warnings"]["by_type"].items():
        new_cnt = new_type_counts.get(code, 0)
        d["fixed"] = d["start"] - new_cnt
        d["now"] = new_cnt

    # Commit (unless squash mode — then we accumulate)
    if not squash_mode:
        msg = f"chore: fix {fixed} nullable warnings in {os.path.basename(rel)}"
        h = git_commit(msg)
        if h:
            status.add_commit(h, msg, True)
            log.info("  Committed %s — fixed %d warnings (%d remaining)",
                     h[:8], fixed, new_total)

    status.data["files_processed"].append(rel)
    status.save()
    return True, fixed


def _claude_fix_file_only(fpath, file_warnings, all_warnings):
    """Run Claude to fix warnings in one file. No build/test — just code edits.
    Returns (fpath, success: bool, claude_output: str|None).
    Thread-safe: only calls Claude + git read, no shared state mutation."""
    rel = os.path.relpath(fpath, REPO_ROOT)
    n = len(file_warnings)

    w_text = "\n".join(
        f"  Line {w['line']}: {w['code']} -- {w['message']}"
        for w in file_warnings[:50]
    )

    dependents = find_dependents(fpath, all_warnings)
    cascade_hint = ""
    if dependents:
        cascade_hint = (
            f"\n\nCASCADE AWARENESS: Files that use types from this file: "
            f"{', '.join(dependents)}. "
            f"When you add `?` to a field or property, other files that reference it "
            f"will get CS8602 warnings. Make sure your changes are safe for all callers."
        )

    prompt = f"""Project: mRemoteNG (.NET 10, WinForms)
Working directory: D:\\github\\mRemoteNG
File: {rel}

Fix ALL these nullable reference type warnings:
{w_text}

CRITICAL RULES:
- When adding `?` to a field type, check ALL usages in THIS file — add `?.` and `?? default`
- Do NOT generate new CS8602 warnings — fix the cascade immediately
- Do NOT change logic/behavior — only types and null checks
- Getter with .Trim() -> ?.Trim() ?? string.Empty
- Use `= null!` ONLY for fields guaranteed initialized in constructor
- GetPropertyValue pattern: result is TPropertyType typed ? typed : value
- Read the file FIRST, understand context, then fix
- Do NOT create new files or tests
- Do NOT run build or tests — only edit the file{cascade_hint}"""

    out = agent_run("warning_fix_parallel", prompt, max_turns=15, timeout=300)
    return (fpath, out is not None, out)


def _group_independent_files(sorted_files, all_warnings, batch_size):
    """Group files into batches of independent files (no cross-references).
    Files that reference each other's types should NOT be in the same batch."""
    batches = []
    remaining = list(sorted_files)

    while remaining:
        batch = []
        batch_basenames = set()

        for item in remaining[:]:
            fpath = item[0]
            basename = os.path.basename(fpath).replace(".cs", "")

            # Check if this file conflicts with any already in the batch
            conflict = False
            if batch:
                # Check if this file's warnings reference types from batch files
                for w in item[1]:
                    for bname in batch_basenames:
                        if bname in w["message"]:
                            conflict = True
                            break
                    if conflict:
                        break

                # Check if batch files' warnings reference this file's types
                if not conflict:
                    for bfpath, bwarnings in batch:
                        for w in bwarnings:
                            if basename in w["message"]:
                                conflict = True
                                break
                        if conflict:
                            break

            if not conflict:
                batch.append(item)
                batch_basenames.add(basename)
                remaining.remove(item)

            if len(batch) >= batch_size:
                break

        if batch:
            batches.append(batch)
        else:
            # All remaining files conflict with each other — take them one by one
            batches.append([remaining.pop(0)])

    return batches


def _fix_batch_parallel(batch, all_warnings, status, squash_mode):
    """Fix a batch of files in parallel with Claude, then one build + test.
    Returns (total_fixed: int, files_fixed: list[str])."""
    batch_files = [os.path.relpath(f, REPO_ROOT) for f, _ in batch]
    log.info("  [PARALLEL] Batch of %d files: %s",
             len(batch), ", ".join(os.path.basename(f) for f, _ in batch))

    # Phase 1: Launch parallel Claude instances
    results = {}
    with concurrent.futures.ThreadPoolExecutor(max_workers=len(batch)) as executor:
        futures = {
            executor.submit(_claude_fix_file_only, fpath, fwarnings, all_warnings): fpath
            for fpath, fwarnings in batch
        }
        for future in concurrent.futures.as_completed(futures):
            fpath = futures[future]
            try:
                _, success, _ = future.result()
                results[fpath] = success
            except Exception as e:
                log.error("  [PARALLEL] Claude exception for %s: %s",
                          os.path.relpath(fpath, REPO_ROOT), e)
                results[fpath] = False

    # Check how many Claude runs succeeded
    succeeded = [f for f, ok in results.items() if ok]
    failed_claude = [f for f, ok in results.items() if not ok]

    if failed_claude:
        log.warning("  [PARALLEL] %d/%d Claude runs failed: %s",
                    len(failed_claude), len(batch),
                    ", ".join(os.path.basename(f) for f in failed_claude))

    if not succeeded:
        log.error("  [PARALLEL] All Claude runs failed — skipping batch")
        git_restore()
        return 0, []

    # Phase 2: One build for the entire batch
    status.set_task(type="warning_fix_batch", step="building",
                    files=", ".join(os.path.basename(f) for f in succeeded))
    build_ok, new_output = run_build(capture_output=True)

    if not build_ok:
        log.warning("  [PARALLEL] Build FAILED for batch — falling back to serial")
        git_restore()
        # Serial fallback: fix files one by one
        total_fixed = 0
        files_fixed = []
        for fpath, fwarnings in batch:
            success, fixed = _fix_single_file(fpath, fwarnings, all_warnings, status, squash_mode)
            if success:
                total_fixed += fixed
                files_fixed.append(os.path.relpath(fpath, REPO_ROOT))
        return total_fixed, files_fixed

    # Phase 3: One test for the entire batch
    status.set_task(type="warning_fix_batch", step="testing",
                    files=", ".join(os.path.basename(f) for f in succeeded))
    if not run_tests():
        log.warning("  [PARALLEL] Tests FAILED for batch — falling back to serial")
        git_restore()
        total_fixed = 0
        files_fixed = []
        for fpath, fwarnings in batch:
            success, fixed = _fix_single_file(fpath, fwarnings, all_warnings, status, squash_mode)
            if success:
                total_fixed += fixed
                files_fixed.append(os.path.relpath(fpath, REPO_ROOT))
        return total_fixed, files_fixed

    # Phase 4: Count improvement
    new_warnings = parse_warnings(new_output) if new_output else {}
    new_total = sum(len(v) for v in new_warnings.values())
    fixed = status.data["warnings"]["total_now"] - new_total

    if fixed <= 0:
        log.warning("  [PARALLEL] No improvement for batch (%d -> %d) — reverting",
                    status.data["warnings"]["total_now"], new_total)
        git_restore()
        return 0, []

    status.data["warnings"]["total_now"] = new_total
    status.data["warnings"]["fixed_this_session"] += fixed

    # Recalculate per-type counts
    new_type_counts = {}
    for file_w in new_warnings.values():
        for w in file_w:
            new_type_counts[w["code"]] = new_type_counts.get(w["code"], 0) + 1
    for code, d in status.data["warnings"]["by_type"].items():
        new_cnt = new_type_counts.get(code, 0)
        d["fixed"] = d["start"] - new_cnt
        d["now"] = new_cnt

    # Phase 5: Commit
    if not squash_mode:
        batch_names = ", ".join(os.path.basename(f) for f, _ in batch)
        msg = f"chore: fix {fixed} nullable warnings in {len(succeeded)} files ({batch_names})"
        if len(msg) > 120:
            msg = f"chore: fix {fixed} nullable warnings in {len(succeeded)} files (batch)"
        h = git_commit(msg)
        if h:
            status.add_commit(h, msg, True)
            log.info("  [PARALLEL] Committed %s — fixed %d warnings (%d remaining)",
                     h[:8], fixed, new_total)

    files_fixed = [os.path.relpath(f, REPO_ROOT) for f in succeeded]
    for rel in files_fixed:
        if rel not in status.data["files_processed"]:
            status.data["files_processed"].append(rel)
    status.save()

    return fixed, files_fixed


def flux_warnings(status, dry_run=False, max_files=None, squash=False, max_passes=10,
                  parallel=0):
    """FLUX 2: Extract warnings, fix file-by-file, verify, commit.
    Multi-pass: repeats until convergence (no improvement between passes).
    When parallel > 0, fixes files in batches of `parallel` using concurrent Claude agents."""

    use_parallel = parallel > 1

    for pass_num in range(1, max_passes + 1):
        status.data["current_pass"] = pass_num
        log.info("=" * 60)
        log.info("  FLUX 2: Warning Cleanup — Pass %d%s", pass_num,
                 f" (parallel={parallel})" if use_parallel else "")
        log.info("=" * 60)

        # Build + extract warnings
        status.set_task(type="warnings", step="extracting")
        build_ok, output = run_build(capture_output=True)
        if not build_ok or not output:
            log.error("  Build failed — cannot extract warnings")
            return

        warnings = parse_warnings(output)
        total = sum(len(v) for v in warnings.values())

        if pass_num == 1:
            status.data["warnings"]["total_start"] = total
        status.data["warnings"]["total_now"] = total

        # Count by type
        type_counts = {}
        for file_w in warnings.values():
            for w in file_w:
                type_counts[w["code"]] = type_counts.get(w["code"], 0) + 1
        for code in WARNING_CODES:
            cnt = type_counts.get(code, 0)
            if cnt or code in status.data["warnings"]["by_type"]:
                if code not in status.data["warnings"]["by_type"]:
                    status.data["warnings"]["by_type"][code] = {"start": cnt, "now": cnt, "fixed": 0}
                else:
                    status.data["warnings"]["by_type"][code]["now"] = cnt
                    if pass_num == 1:
                        status.data["warnings"]["by_type"][code]["start"] = cnt
                log.info("  %s: %d", code, cnt)
        status.save()

        if total == 0:
            log.info("  No warnings remaining!")
            break

        log.info("  Total: %d warnings across %d files (pass %d)", total, len(warnings), pass_num)

        # Sort: most warnings first
        sorted_files = sorted(warnings.items(), key=lambda x: -len(x[1]))
        if max_files:
            sorted_files = sorted_files[:max_files]

        pass_fixed_total = 0
        pass_start_total = total

        if use_parallel:
            # ── PARALLEL MODE: batch files, fix in parallel, one build+test per batch ──
            batches = _group_independent_files(sorted_files, warnings, parallel)
            log.info("  Grouped %d files into %d batches (batch_size=%d)",
                     len(sorted_files), len(batches), parallel)

            for bi, batch in enumerate(batches, 1):
                kill_stale_processes()
                batch_names = ", ".join(
                    f"{os.path.basename(f)}({len(w)}w)" for f, w in batch
                )
                batch_total_w = sum(len(w) for _, w in batch)
                remaining_batches = len(batches) - bi
                remaining_files = sum(len(b) for b in batches[bi:])
                eta = status.eta_str(remaining_files)
                log.info("[P%d B%d/%d] %d files (%d warnings): %s  ETA: %s",
                         pass_num, bi, len(batches), len(batch), batch_total_w,
                         batch_names, eta)
                print_progress(f"WARNINGS P{pass_num}", bi, len(batches),
                               f"batch {bi} ({len(batch)} files, {batch_total_w}w) ETA:{eta}",
                               status)

                if dry_run:
                    log.info("  [DRY RUN] skip batch")
                    continue

                batch_start = time.time()
                fixed, files_fixed = _fix_batch_parallel(
                    batch, warnings, status, squash
                )
                batch_elapsed = time.time() - batch_start
                # Record average time per file in this batch
                if files_fixed:
                    per_file = batch_elapsed / len(files_fixed)
                    for _ in files_fixed:
                        status.record_file_time(per_file)

                if fixed > 0:
                    pass_fixed_total += fixed
                    log.info("  [BATCH %.0fs] Fixed %d in %d files (total session: %d)",
                             batch_elapsed, fixed, len(files_fixed),
                             status.data["warnings"]["fixed_this_session"])
        else:
            # ── SERIAL MODE: fix file by file (original behavior) ──
            for i, (fpath, file_warnings) in enumerate(sorted_files, 1):
                kill_stale_processes()
                rel = os.path.relpath(fpath, REPO_ROOT)
                n = len(file_warnings)
                remaining = len(sorted_files) - i
                eta = status.eta_str(remaining)
                log.info("[P%d %d/%d] %s (%d warnings) ETA: %s",
                         pass_num, i, len(sorted_files), rel, n, eta)
                print_progress(f"WARNINGS P{pass_num}", i, len(sorted_files),
                               f"{rel} ({n}w) ETA:{eta}", status)

                if dry_run:
                    log.info("  [DRY RUN] skip")
                    continue

                file_start = time.time()
                success, fixed = _fix_single_file(fpath, file_warnings, warnings, status, squash)
                file_elapsed = time.time() - file_start
                status.record_file_time(file_elapsed)

                if success:
                    pass_fixed_total += fixed
                    log.info("  [%.0fs] Fixed %d (total session: %d)",
                             file_elapsed, fixed, status.data["warnings"]["fixed_this_session"])

        # End of pass: push (or squash)
        if not dry_run:
            if squash and pass_fixed_total > 0:
                # Squash all uncommitted changes into one commit
                if git_has_changes():
                    msg = (f"chore: fix {status.data['warnings']['fixed_this_session']} "
                           f"nullable warnings across {len(status.data['files_processed'])} files "
                           f"(pass {pass_num})")
                    h = git_commit(msg)
                    if h:
                        status.add_commit(h, msg, True)
                        log.info("  Squash committed %s", h[:8])

            if status.data["commits"]:
                log.info("  Pushing commits (pass %d) ...", pass_num)
                git_push()

        # Check convergence: if this pass fixed nothing, stop
        if pass_fixed_total == 0:
            log.info("  Pass %d: no improvement — convergence reached", pass_num)
            break
        else:
            improvement_pct = pass_fixed_total * 100 // max(pass_start_total, 1)
            log.info("  Pass %d: fixed %d warnings (%d%% of %d)",
                     pass_num, pass_fixed_total, improvement_pct, pass_start_total)
            # If improvement is tiny (<2%), stop to avoid diminishing returns
            if improvement_pct < 2 and pass_num > 1:
                log.info("  Marginal improvement (<2%%) — stopping")
                break

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
        pct = w["fixed_this_session"] * 100 // max(w["total_start"], 1)
        print(f"\n  Warnings:  {w['total_start']} -> {w['total_now']}  "
              f"(-{w['fixed_this_session']} fixed, {pct}%)")
        for code, d in sorted(w.get("by_type", {}).items()):
            print(f"             {code}: {d['start']} -> {d['now']}  (-{d['fixed']})")

    passes = s.get("current_pass", 1)
    if passes > 1:
        print(f"\n  Passes:    {passes}")

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

    if status._file_times:
        avg = sum(status._file_times) / len(status._file_times)
        print(f"  Avg/file:  {avg:.0f}s ({len(status._file_times)} files)")

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
    print(f"  Pass:    {s.get('current_pass', '?')}")
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
        pct = w.get("fixed_this_session", 0) * 100 // max(w["total_start"], 1)
        print(f"\n  Warnings: {w['total_start']} -> {w['total_now']} "
              f"(-{w.get('fixed_this_session', 0)}, {pct}%)")
        for code, d in sorted(w.get("by_type", {}).items()):
            print(f"    {code}: {d['start']} -> {d['now']}  (-{d['fixed']})")

    commits = s.get("commits", [])
    errors = s.get("errors", [])
    print(f"\n  Commits: {len(commits)}  |  Errors: {len(errors)}")

    if errors:
        print(f"\n  Last errors:")
        for e in errors[-3:]:
            print(f"    [{e['time']}] {e['task']}: {e['step']} -- {e['error'][:60]}")
    print()


# ── IIS: JSON DB HELPERS ───────────────────────────────────────────────────
def iis_read_json(path):
    """Read JSON with utf-8-sig fallback (handles PS-generated BOM files)."""
    return json.loads(Path(path).read_text(encoding="utf-8-sig"))


def iis_write_json(path, data):
    """Write JSON as utf-8 (no BOM). Normalized output."""
    Path(path).write_text(
        json.dumps(data, indent=2, ensure_ascii=False) + "\n",
        encoding="utf-8",
    )


def iis_load_all_issues(repos=("upstream", "fork")):
    """Load all issue JSONs from specified repo DB directories."""
    issues = []
    for repo_key in repos:
        d = ISSUES_DB_ROOT / repo_key
        if not d.exists():
            continue
        for f in sorted(d.glob("[0-9]*.json")):
            try:
                data = iis_read_json(f)
                data["_repo_key"] = repo_key
                data["_file_path"] = str(f)
                issues.append(data)
            except Exception:
                pass
    return issues


# ── IIS: GITHUB CLI HELPERS ───────────────────────────────────────────────
def gh_run_json(args, timeout=60):
    """Run gh CLI command expecting JSON output. Returns parsed JSON or None."""
    cmd = ["gh"] + args
    try:
        r = subprocess.run(
            cmd, capture_output=True, text=True,
            timeout=timeout, encoding="utf-8", errors="replace",
        )
        if r.returncode != 0:
            log.warning("  [GH] %s failed: %s", " ".join(args[:3]), (r.stderr or "")[:200])
            return None
        return json.loads(r.stdout)
    except Exception as e:
        log.warning("  [GH] %s error: %s", " ".join(args[:3]), e)
        return None


def gh_post_comment(repo, num, body):
    """Post a comment on a GitHub issue. Returns True on success."""
    try:
        r = _run(
            ["gh", "issue", "comment", str(num), "--repo", repo, "--body", body],
            timeout=30,
        )
        return r.returncode == 0
    except Exception:
        return False


# ── IIS: SYNC ─────────────────────────────────────────────────────────────
def iis_sync(repos="both", issue_numbers=None, include_closed=False, max_issues=1000):
    """Sync issues from GitHub into local JSON DB.
    Replaces Sync-Issues.ps1."""
    meta = iis_read_json(META_PATH)
    upstream_repo = meta["repos"]["upstream"]
    fork_repo = meta["repos"]["fork"]
    our_user = meta["our_github_user"]
    sync_start = datetime.datetime.now(datetime.timezone.utc)

    print("=== Issue Intelligence System - Sync ===")
    print(f"Time: {sync_start.strftime('%Y-%m-%d %H:%M:%S')} UTC")
    print(f"Repos: {repos}")
    if issue_numbers:
        print(f"Targeted issues: {', '.join(str(n) for n in issue_numbers)}")
    print()

    repos_to_sync = []
    if repos in ("both", "upstream"):
        repos_to_sync.append(("upstream", upstream_repo))
    if repos in ("both", "fork"):
        repos_to_sync.append(("fork", fork_repo))

    stats = {
        "repos_synced": [],
        "issues_new": 0,
        "issues_updated": 0,
        "issues_error": 0,
        "comments_new": 0,
        "needs_action": 0,
        "waiting_for_us": 0,
    }

    for repo_key, repo_name in repos_to_sync:
        print(f"--- Syncing: {repo_name} ({repo_key}) ---")

        if issue_numbers:
            issues_list = [{"number": n} for n in issue_numbers]
        else:
            state_arg = "all" if include_closed else "open"
            print(f"  Fetching issue list (state={state_arg}, limit={max_issues})...")
            data = gh_run_json([
                "issue", "list", "--repo", repo_name,
                "--state", state_arg, "--limit", str(max_issues),
                "--json", "number,title,updatedAt",
            ], timeout=120)
            if data is None:
                print(f"  Failed to list issues from {repo_name}")
                continue
            issues_list = data
            print(f"  Found {len(issues_list)} issues")

        repo_dir = ISSUES_DB_ROOT / repo_key
        repo_dir.mkdir(parents=True, exist_ok=True)

        skipped_unchanged = 0
        for idx, issue_stub in enumerate(issues_list, 1):
            num = issue_stub["number"]
            pct = int(idx * 100 / max(len(issues_list), 1))

            # Skip if local JSON is up-to-date (same updatedAt timestamp)
            padded = f"{num:04d}"
            file_path = repo_dir / f"{padded}.json"
            gh_updated_at = issue_stub.get("updatedAt", "")
            if file_path.exists() and gh_updated_at:
                try:
                    local = iis_read_json(file_path)
                    if local.get("github_updated_at") == gh_updated_at:
                        skipped_unchanged += 1
                        stats["issues_updated"] += 1
                        continue
                except Exception:
                    pass  # corrupted JSON — re-fetch

            print(f"  [{pct}%] Processing #{num}...", end="", flush=True)

            # Fetch full issue with comments
            gh_full = gh_run_json([
                "issue", "view", str(num), "--repo", repo_name,
                "--json", "number,title,state,labels,createdAt,updatedAt,body,author,comments",
            ], timeout=30)

            if gh_full is None:
                stats["issues_error"] += 1
                print(" ERROR")
                continue

            # Build comments array
            gh_comments = []
            for c in (gh_full.get("comments") or []):
                body_text = c.get("body") or ""
                snippet = body_text[:500] + "..." if len(body_text) > 500 else body_text
                is_ours = (c.get("author", {}).get("login") == our_user)
                gh_comments.append({
                    "id": c.get("id"),
                    "author": c.get("author", {}).get("login", ""),
                    "date": c.get("createdAt", ""),
                    "snippet": snippet,
                    "is_ours": is_ours,
                    "analyzed": False,
                    "action_needed": False,
                })

            # Load existing JSON if present (padded/file_path already set above)
            existing = None
            is_new = False
            new_comment_count = 0

            if file_path.exists():
                try:
                    existing = iis_read_json(file_path)
                except Exception:
                    existing = None

            if existing:
                # Detect new comments by comparing IDs
                existing_ids = {ec.get("id") for ec in (existing.get("comments") or [])}
                for gc in gh_comments:
                    if gc["id"] not in existing_ids:
                        new_comment_count += 1
                        gc["analyzed"] = False
                        gc["action_needed"] = not gc["is_ours"]
                    else:
                        # Preserve analyzed/action_needed from existing
                        for ec in (existing.get("comments") or []):
                            if ec.get("id") == gc["id"]:
                                gc["analyzed"] = ec.get("analyzed", False)
                                gc["action_needed"] = ec.get("action_needed", False)
                                break
            else:
                is_new = True
                new_comment_count = len(gh_comments)
                for gc in gh_comments:
                    gc["action_needed"] = not gc["is_ours"]

            # Build labels
            labels = [lbl.get("name", "") for lbl in (gh_full.get("labels") or [])]

            # Determine needs_action
            unread_count = sum(
                1 for gc in gh_comments if not gc["analyzed"] and not gc["is_ours"]
            )
            needs_action = unread_count > 0 or is_new

            # Determine waiting_for_us (last comment is from someone else)
            last_comment = gh_comments[-1] if gh_comments else None
            waiting_for_us = bool(last_comment and not last_comment["is_ours"])

            # Body snippet
            body_raw = gh_full.get("body") or ""
            body_snippet = body_raw[:500] + "..." if len(body_raw) > 500 else body_raw

            # Preserve our fields from existing
            prev = existing or {}

            issue_obj = {
                "number": num,
                "repo": repo_name,
                "title": gh_full.get("title", ""),
                "state": (gh_full.get("state") or "open").lower(),
                "labels": labels,
                "author": gh_full.get("author", {}).get("login", ""),
                "created_at": gh_full.get("createdAt", ""),
                "github_updated_at": gh_full.get("updatedAt", ""),
                "body_snippet": body_snippet,
                "our_status": prev.get("our_status", "new"),
                "priority": prev.get("priority"),
                "target_release": prev.get("target_release"),
                "our_branch": prev.get("our_branch"),
                "our_pr": prev.get("our_pr"),
                "iterations": prev.get("iterations", []),
                "comments": gh_comments,
                "comments_cursor": gh_full.get("updatedAt", ""),
                "unread_comments": unread_count,
                "needs_action": needs_action,
                "waiting_for_us": waiting_for_us,
                "last_synced": sync_start.strftime("%Y-%m-%dT%H:%M:%SZ"),
                "notes": prev.get("notes", ""),
            }

            iis_write_json(file_path, issue_obj)

            # Print status
            if is_new:
                stats["issues_new"] += 1
                print(" NEW", end="")
            else:
                stats["issues_updated"] += 1
                print(" updated", end="")

            if new_comment_count > 0:
                stats["comments_new"] += new_comment_count
                print(f" (+{new_comment_count} comments)", end="")

            if waiting_for_us:
                stats["waiting_for_us"] += 1
                print(" [WAITING FOR US]", end="")
            elif needs_action:
                stats["needs_action"] += 1
                print(" [needs action]", end="")

            print()

        if skipped_unchanged > 0:
            print(f"  Skipped {skipped_unchanged} unchanged issues (same updatedAt)")
        stats["repos_synced"].append(repo_name)
        print()

    # Update _meta.json
    duration = (datetime.datetime.now(datetime.timezone.utc) - sync_start).total_seconds()
    meta["last_sync"] = sync_start.strftime("%Y-%m-%dT%H:%M:%SZ")
    meta["last_sync_stats"] = {
        "repos_synced": stats["repos_synced"],
        "issues_new": stats["issues_new"],
        "issues_updated": stats["issues_updated"],
        "issues_error": stats["issues_error"],
        "comments_new": stats["comments_new"],
        "needs_action": stats["needs_action"],
        "waiting_for_us": stats["waiting_for_us"],
        "duration_sec": round(duration, 1),
    }
    iis_write_json(META_PATH, meta)

    # Summary
    print("=== Sync Complete ===")
    print(f"Duration: {duration:.1f}s")
    print(f"New issues:      {stats['issues_new']}")
    print(f"Updated issues:  {stats['issues_updated']}")
    print(f"New comments:    {stats['comments_new']}")
    print(f"Errors:          {stats['issues_error']}")
    print()
    if stats["waiting_for_us"] > 0:
        print(f"!! {stats['waiting_for_us']} issues WAITING FOR OUR RESPONSE !!")
    if stats["needs_action"] > 0:
        print(f">> {stats['needs_action']} issues need action (run: iis_orchestrator.py analyze)")
    print()


# ── IIS: ANALYZE ──────────────────────────────────────────────────────────
def _auto_classify(issue):
    """Auto-classify an issue based on labels and iteration status."""
    labels = issue.get("labels") or []
    priority = None
    action = None

    if "critical" in labels or "Security" in labels:
        priority = "P0-critical"
    elif "Bug" in labels:
        priority = "P2-bug" if "1.78.*" in labels else "P3-enhancement"
    elif "Enhancement" in labels:
        priority = "P3-enhancement"
    elif "Duplicate" in labels:
        priority = "P4-debt"
        action = "verify-duplicate"
    elif "Need 2 check" in labels:
        priority = "P2-bug"
        action = "needs-verification"
    else:
        priority = "P3-enhancement"

    # Check iteration status — user feedback after our fix?
    iterations = issue.get("iterations") or []
    if iterations:
        last_iter = iterations[-1]
        if last_iter.get("type") == "user-feedback" or last_iter.get("result") == "partial":
            action = "iteration-needed"

    if issue.get("waiting_for_us") and not action:
        action = "respond"

    return {"priority": priority, "action": action}


def iis_analyze(show_all=False, waiting_only=False, priority_filter=None,
                status_filter=None):
    """Analyze issues and show what needs attention.
    Replaces Analyze-Issues.ps1."""
    meta = iis_read_json(META_PATH)

    print("=== Issue Intelligence System - Analysis ===")
    if meta.get("last_sync"):
        print(f"Last sync: {meta['last_sync']}")
    else:
        print("WARNING: No sync has been run yet. Run: iis_orchestrator.py sync")
        return
    print()

    all_issues = iis_load_all_issues()
    print(f"Total issues in DB: {len(all_issues)}")

    # Filter
    filtered = all_issues
    if waiting_only:
        filtered = [i for i in filtered if i.get("waiting_for_us")]
    elif not show_all:
        filtered = [i for i in filtered
                    if i.get("needs_action") or i.get("our_status") == "new"
                    or i.get("waiting_for_us")]

    if priority_filter:
        filtered = [i for i in filtered if i.get("priority") == priority_filter]
    if status_filter:
        filtered = [i for i in filtered if i.get("our_status") == status_filter]

    # Categorize
    urgent, iteration, respond, triage, roadmap, other = [], [], [], [], [], []

    for issue in filtered:
        cl = _auto_classify(issue)
        item = {"issue": issue, "classification": cl}

        if issue.get("waiting_for_us") and cl["priority"] in ("P0-critical", "P1-security"):
            urgent.append(item)
        elif cl["action"] == "iteration-needed":
            iteration.append(item)
        elif issue.get("waiting_for_us"):
            respond.append(item)
        elif issue.get("our_status") == "new":
            triage.append(item)
        elif issue.get("our_status") == "roadmap":
            roadmap.append(item)
        else:
            other.append(item)

    # Display helper
    def print_row(item):
        i = item["issue"]
        cl = item["classification"]
        iter_count = len(i.get("iterations") or [])
        unread = i.get("unread_comments", 0)

        line = (f"  #{i['number']:<5} [{i.get('our_status', 'new'):^8}] "
                f"[{(cl['priority'] or ''):^14}] {i.get('title', '')}")
        if len(line) > 120:
            line = line[:117] + "..."
        print(line, end="")
        if iter_count > 0:
            print(f" (iter:{iter_count})", end="")
        if unread > 0:
            print(f" [+{unread} unread]", end="")
        if cl["action"]:
            print(f" -> {cl['action']}", end="")
        print()

    if urgent:
        print("!! URGENT - Waiting for us (critical/security) !!")
        for item in urgent:
            print_row(item)
        print()
    if iteration:
        print(">> ITERATION NEEDED - User feedback after our fix <<")
        for item in iteration:
            print_row(item)
        print()
    if respond:
        print(">> RESPOND - Waiting for our response <<")
        for item in respond:
            print_row(item)
        print()
    if triage:
        print("-- NEW - Needs triage --")
        for item in triage:
            print_row(item)
        print()
    if roadmap:
        print("-- ROADMAP - Tracked for next release --")
        for item in roadmap:
            print_row(item)
        print()
    if other:
        print("-- OTHER --")
        for item in other:
            print_row(item)
        print()

    # Summary
    print("=== Summary ===")
    print(f"Total in DB:       {len(all_issues)}")
    print(f"Shown:             {len(filtered)}")
    print(f"Urgent:            {len(urgent)}")
    print(f"Iteration needed:  {len(iteration)}")
    print(f"Awaiting response: {len(respond)}")
    print(f"New (triage):      {len(triage)}")
    print(f"In roadmap:        {len(roadmap)}")

    # Status distribution
    print()
    print("--- Status Distribution ---")
    status_counts = {}
    for i in all_issues:
        s = i.get("our_status", "new")
        status_counts[s] = status_counts.get(s, 0) + 1
    for s, c in sorted(status_counts.items(), key=lambda x: -x[1]):
        print(f"  {s}: {c}")

    # Suggested next steps
    print()
    print("--- Suggested Next Steps ---")
    if urgent:
        print(f"  1. RESPOND to {len(urgent)} urgent issues immediately")
    if iteration:
        print(f"  2. RE-FIX {len(iteration)} issues with user feedback (iteration loop)")
    if respond:
        print(f"  3. Reply to {len(respond)} issues waiting for response")
    if triage:
        print(f"  4. Triage {len(triage)} new issues"
              " (run: iis_orchestrator.py update --issue <N> --status triaged)")
    print("  Run: iis_orchestrator.py report to create markdown report")


# ── IIS: UPDATE ───────────────────────────────────────────────────────────
VALID_TRANSITIONS = {
    "new":         ["triaged", "roadmap", "in-progress", "released", "wontfix", "duplicate"],
    "triaged":     ["roadmap", "in-progress", "wontfix", "duplicate"],
    "roadmap":     ["in-progress", "wontfix"],
    "in-progress": ["testing", "roadmap", "wontfix"],
    "testing":     ["released", "in-progress"],
    "released":    ["in-progress"],
    "wontfix":     ["new", "triaged"],
    "duplicate":   ["new", "triaged"],
}


def iis_update(issue_num, new_status, repo="upstream", description=None,
               pr=None, branch=None, release=None, release_url=None,
               post_comment=False, priority=None, notes=None,
               add_to_roadmap=False):
    """Update issue lifecycle status with iteration tracking.
    Replaces Update-Status.ps1."""
    meta = iis_read_json(META_PATH)
    repo_full = meta["repos"].get(repo, meta["repos"]["upstream"])

    padded = f"{issue_num:04d}"
    file_path = ISSUES_DB_ROOT / repo / f"{padded}.json"

    if not file_path.exists():
        print(f"ERROR: Issue #{issue_num} not found in {repo} DB. Run sync first.")
        return False

    issue_data = iis_read_json(file_path)
    old_status = issue_data.get("our_status", "new")

    print(f"=== Issue #{issue_num} Status Update ===")
    print(f"Title:      {issue_data.get('title', '')}")
    print(f"Old status: {old_status}")
    print(f"New status: {new_status}")
    print()

    # Validate transition
    valid = VALID_TRANSITIONS.get(old_status, [])
    if new_status not in valid:
        print(f"WARNING: Non-standard transition: {old_status} -> {new_status}")
        print(f"Standard transitions from '{old_status}': {', '.join(valid)}")

    # Detect iteration loop
    is_iteration = old_status in ("testing", "released") and new_status == "in-progress"
    if is_iteration:
        print(">> ITERATION LOOP detected: user feedback after fix, re-opening <<")

    # Update fields
    issue_data["our_status"] = new_status
    if priority:
        issue_data["priority"] = priority
    if branch:
        issue_data["our_branch"] = branch
    if pr:
        issue_data["our_pr"] = pr
    if release:
        issue_data["target_release"] = release
    if notes:
        issue_data["notes"] = notes

    # Add iteration entry
    iterations = issue_data.get("iterations") or []
    iter_seq = max((it.get("seq", 0) for it in iterations), default=0) + 1

    iter_type = "iteration-reopen" if is_iteration else new_status
    iter_entry = {
        "seq": iter_seq,
        "date": datetime.date.today().isoformat(),
        "type": iter_type,
        "description": description or f"Status changed to {new_status}",
        "pr": pr,
        "branch": branch,
        "release": release,
        "comment_posted": False,
    }
    iterations.append(iter_entry)
    issue_data["iterations"] = iterations

    # Build comment from template
    templates = meta.get("comment_templates", {})
    comment_body = None

    if new_status == "roadmap":
        comment_body = templates.get("triaged_to_roadmap")
    elif new_status == "in-progress":
        comment_body = (templates.get("iteration_ack") if is_iteration
                        else templates.get("in_progress"))
    elif new_status == "testing":
        pr_url = f"https://github.com/{repo_full}/pull/{pr}" if pr else "(PR pending)"
        tpl = templates.get("testing", "")
        comment_body = tpl.replace("{pr_url}", pr_url) if tpl else None
    elif new_status == "released":
        tpl = templates.get("released", "")
        if tpl:
            comment_body = tpl.replace("{release_tag}", release or "").replace(
                "{release_url}", release_url or ""
            )

    # Post comment
    if comment_body:
        if post_comment:
            print(f"Posting comment to #{issue_num} on {repo_full}...")
            print("--- Comment preview ---")
            print(comment_body)
            print("--- End preview ---")

            ok = gh_post_comment(repo_full, issue_num, comment_body)
            if ok:
                print("Comment posted successfully.")
                iterations[-1]["comment_posted"] = True
            else:
                print("WARNING: Failed to post comment.")
        else:
            print("--- Suggested comment (not posted, use --post-comment to send) ---")
            print(comment_body)
            print("--- End suggestion ---")

    # Update roadmap
    if add_to_roadmap or new_status == "roadmap":
        if ROADMAP_PATH.exists():
            try:
                roadmap_data = iis_read_json(ROADMAP_PATH)
                items = roadmap_data.get("items", [])
                existing_item = next(
                    (it for it in items
                     if it.get("number") == issue_num and it.get("repo") == repo_full),
                    None,
                )
                if not existing_item:
                    items.append({
                        "number": issue_num,
                        "repo": repo_full,
                        "title": issue_data.get("title", ""),
                        "priority": issue_data.get("priority"),
                        "our_status": new_status,
                        "added_date": datetime.date.today().isoformat(),
                        "target_release": issue_data.get("target_release"),
                    })
                    roadmap_data["items"] = items
                    print("Added to roadmap.")
                else:
                    existing_item["our_status"] = new_status
                    existing_item["priority"] = issue_data.get("priority")
                    print("Updated roadmap entry.")
                roadmap_data["last_updated"] = (
                    datetime.datetime.now(datetime.timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ")
                )
                iis_write_json(ROADMAP_PATH, roadmap_data)
            except Exception as e:
                print(f"WARNING: Roadmap update failed: {e}")

    # Save
    issue_data["last_synced"] = datetime.datetime.now(datetime.timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ")
    iis_write_json(file_path, issue_data)

    print()
    print(f"Issue #{issue_num} updated: {old_status} -> {new_status}")
    if is_iteration:
        print(f"Iteration #{iter_seq} recorded (feedback loop)")
    print(f"File: {file_path}")
    return True


# ── IIS: REPORT ───────────────────────────────────────────────────────────
def iis_report(include_all=False, no_save=False):
    """Generate markdown report from issue DB.
    Replaces Generate-Report.ps1."""
    meta = iis_read_json(META_PATH)
    now = datetime.datetime.now(datetime.timezone.utc)
    report_date = now.strftime("%Y-%m-%d")

    all_issues = iis_load_all_issues()

    # Load roadmap
    roadmap_data = None
    if ROADMAP_PATH.exists():
        try:
            roadmap_data = iis_read_json(ROADMAP_PATH)
        except Exception:
            pass

    # Categorize
    urgent = [i for i in all_issues
              if i.get("waiting_for_us")
              and i.get("priority") in ("P0-critical", "P1-security")]
    iteration = [i for i in all_issues
                 if (i.get("iterations") or [])
                 and (i["iterations"][-1].get("type") in
                      ("iteration-reopen", "user-feedback"))]
    waiting = [i for i in all_issues if i.get("waiting_for_us")]
    new_issues = [i for i in all_issues if i.get("our_status") == "new"]
    in_progress = [i for i in all_issues if i.get("our_status") == "in-progress"]
    testing = [i for i in all_issues if i.get("our_status") == "testing"]
    released = [i for i in all_issues if i.get("our_status") == "released"]
    roadmap_items = [i for i in all_issues if i.get("our_status") == "roadmap"]

    lines = []
    lines.append(f"# Issue Intelligence Report - {report_date}")
    lines.append("")
    lines.append(f"Generated: {now.strftime('%Y-%m-%d %H:%M:%S')} UTC")
    lines.append(f"Last sync: {meta.get('last_sync', 'never')}")
    lines.append(f"Total issues tracked: {len(all_issues)}")
    lines.append("")

    # Status summary
    lines.append("## Status Summary")
    lines.append("")
    lines.append("| Status | Count |")
    lines.append("|--------|-------|")
    status_counts = {}
    for i in all_issues:
        s = i.get("our_status", "new")
        status_counts[s] = status_counts.get(s, 0) + 1
    for s, c in sorted(status_counts.items(), key=lambda x: -x[1]):
        lines.append(f"| {s} | {c} |")
    lines.append("")

    # Urgent
    if urgent:
        lines.append("## !! URGENT - Critical/Security Waiting for Response")
        lines.append("")
        for i in urgent:
            lines.append(f"- **#{i['number']}** [{i.get('priority', '')}]"
                         f" {i.get('title', '')}")
            lines.append(f"  - Repo: {i.get('repo', '')} | Status:"
                         f" {i.get('our_status', '')} | Updated:"
                         f" {i.get('github_updated_at', '')}")
        lines.append("")

    # Iteration needed
    if iteration:
        lines.append("## Iteration Needed - User Feedback After Fix")
        lines.append("")
        lines.append("> These issues had a fix applied but user reported"
                     " it's incomplete.")
        lines.append("")
        for i in iteration:
            iters = i.get("iterations") or []
            lines.append(f"- **#{i['number']}** [{i.get('priority', '')}]"
                         f" {i.get('title', '')} - iteration {len(iters)}")
            for it in iters:
                lines.append(f"  - [{it.get('date', '')}] {it.get('type', '')}:"
                             f" {it.get('description', '')}")
        lines.append("")

    # Waiting for response
    if waiting:
        lines.append("## Waiting for Our Response")
        lines.append("")
        for i in sorted(waiting, key=lambda x: x.get("github_updated_at", "")):
            comments = i.get("comments") or []
            lines.append(f"- **#{i['number']}** [{i.get('our_status', '')}]"
                         f" {i.get('title', '')}")
            if comments:
                last = comments[-1]
                snippet = last.get("snippet", "")
                if len(snippet) > 100:
                    snippet = snippet[:100] + "..."
                lines.append(f"  - Last: @{last.get('author', '')}"
                             f" ({last.get('date', '')}): {snippet}")
        lines.append("")

    # New issues
    if new_issues:
        lines.append("## New Issues - Need Triage")
        lines.append("")
        for i in sorted(new_issues,
                        key=lambda x: x.get("github_updated_at", ""),
                        reverse=True):
            labels = ", ".join(i.get("labels") or []) or "none"
            lines.append(f"- **#{i['number']}** {i.get('title', '')}")
            lines.append(f"  - Labels: {labels} | Author: @{i.get('author', '')}"
                         f" | Created: {i.get('created_at', '')}")
        lines.append("")

    # Roadmap
    if roadmap_items or (roadmap_data and roadmap_data.get("items")):
        target = roadmap_data.get("target_release", "") if roadmap_data else ""
        lines.append(f"## Roadmap - Target: {target}")
        lines.append("")
        lines.append("| # | Priority | Title | Status |")
        lines.append("|---|----------|-------|--------|")
        if roadmap_data and roadmap_data.get("items"):
            for item in roadmap_data["items"]:
                issue = next(
                    (i for i in all_issues
                     if i.get("number") == item.get("number")
                     and i.get("repo") == item.get("repo")),
                    None,
                )
                status = (issue.get("our_status") if issue
                          else item.get("our_status", ""))
                lines.append(f"| #{item.get('number', '')} |"
                             f" {item.get('priority', '')} |"
                             f" {item.get('title', '')} | {status} |")
        lines.append("")

    # In progress
    if in_progress:
        lines.append("## In Progress")
        lines.append("")
        for i in in_progress:
            br = f"`{i['our_branch']}`" if i.get("our_branch") else "no branch"
            lines.append(f"- **#{i['number']}** {i.get('title', '')} - {br}")
        lines.append("")

    # Testing
    if testing:
        lines.append("## In Testing")
        lines.append("")
        for i in testing:
            pr_str = f"PR #{i['our_pr']}" if i.get("our_pr") else "no PR"
            lines.append(f"- **#{i['number']}** {i.get('title', '')} - {pr_str}")
        lines.append("")

    # Recently released (last 30 days)
    thirty_days_ago = now.replace(tzinfo=None) - datetime.timedelta(days=30)
    recent_released = []
    for i in released:
        for it in (i.get("iterations") or []):
            if it.get("type") == "released" and it.get("date"):
                try:
                    d = datetime.datetime.strptime(it["date"], "%Y-%m-%d")
                    if d > thirty_days_ago:
                        recent_released.append(i)
                        break
                except ValueError:
                    pass
    if recent_released:
        lines.append("## Recently Released (last 30 days)")
        lines.append("")
        for i in recent_released:
            rel_iter = next(
                (it for it in reversed(i.get("iterations") or [])
                 if it.get("type") == "released"),
                None,
            )
            rel_tag = rel_iter.get("release", "") if rel_iter else ""
            lines.append(f"- **#{i['number']}** {i.get('title', '')} - {rel_tag}")
        lines.append("")

    # Full inventory
    if include_all:
        lines.append("## Full Issue Inventory")
        lines.append("")
        lines.append("| # | Repo | Status | Priority | Title | Iterations |")
        lines.append("|---|------|--------|----------|-------|------------|")
        for i in sorted(all_issues, key=lambda x: x.get("number", 0)):
            iter_count = len(i.get("iterations") or [])
            title = i.get("title", "")
            if len(title) > 60:
                title = title[:57] + "..."
            repo_key = i.get("_repo_key", "")
            lines.append(f"| #{i.get('number', '')} | {repo_key} |"
                         f" {i.get('our_status', '')} | {i.get('priority', '')} |"
                         f" {title} | {iter_count} |")
        lines.append("")

    # Footer
    lines.append("---")
    lines.append("*Generated by Issue Intelligence System v2.0.0 (Python)*")
    lines.append("*Next: run `iis_orchestrator.py sync` to refresh,"
                 " `iis_orchestrator.py analyze` to review,"
                 " `iis_orchestrator.py update` to transition*")

    report_text = "\n".join(lines)

    if no_save:
        print(report_text)
    else:
        REPORTS_DIR.mkdir(parents=True, exist_ok=True)
        report_path = REPORTS_DIR / f"{report_date}_sync.md"
        if report_path.exists():
            report_path = REPORTS_DIR / (
                f"{report_date}_{now.strftime('%H%M')}_sync.md"
            )
        report_path.write_text(report_text, encoding="utf-8")
        print(f"Report saved to: {report_path}")
        print()
        print("Quick stats:")
        print(f"  Total tracked:    {len(all_issues)}")
        print(f"  Urgent:           {len(urgent)}")
        print(f"  Iteration needed: {len(iteration)}")
        print(f"  Waiting for us:   {len(waiting)}")
        print(f"  New (triage):     {len(new_issues)}")


# ── MAIN ────────────────────────────────────────────────────────────────────
def main():
    parser = argparse.ArgumentParser(
        description="IIS Orchestrator — mRemoteNG Issue Intelligence System"
                    " + automated issue & warning resolution",
    )
    parser.add_argument(
        "mode", nargs="?", default="all",
        choices=["all", "issues", "warnings", "status",
                 "sync", "analyze", "update", "report"],
        help="sync/analyze/update/report (IIS), or all/issues/warnings/status (orchestrator)",
    )
    # ── Orchestrator args ──
    parser.add_argument("--dry-run", action="store_true",
                        help="Simulate without making changes")
    parser.add_argument("--max-issues", type=int, default=None,
                        help="Max issues to process (orchestrator)")
    parser.add_argument("--max-files", type=int, default=None,
                        help="Max files to fix per pass (warnings mode)")
    parser.add_argument("--squash", action="store_true",
                        help="Squash all warning fixes into one commit per pass")
    parser.add_argument("--max-passes", type=int, default=10,
                        help="Max multi-pass iterations (default: 10)")
    parser.add_argument("--parallel", type=int, default=0,
                        help="Fix N files in parallel per batch (0=serial)")
    # ── Agent args ──
    parser.add_argument("--agent", default=None,
                        choices=["codex", "claude", "gemini"],
                        help="Override agent for ALL tasks (ignores AGENT_CONFIG)")
    parser.add_argument("--gemini-model", default=None,
                        help="Override Gemini model (default: gemini-3-pro-preview)")
    parser.add_argument("--codex-model", default=None,
                        help="Override Codex model (default: gpt-5.3-codex)")
    # ── IIS sync args ──
    parser.add_argument("--repos", default="both",
                        choices=["both", "upstream", "fork"],
                        help="Which repos to sync (default: both)")
    parser.add_argument("--issues", default=None,
                        help="Comma-separated issue numbers for targeted sync")
    parser.add_argument("--include-closed", action="store_true",
                        help="Include closed issues in sync")
    # ── IIS analyze args ──
    parser.add_argument("--waiting-only", action="store_true",
                        help="Show only issues waiting for our response")
    parser.add_argument("--priority", default=None,
                        help="Filter by priority (e.g. P2-bug)")
    parser.add_argument("--status", default=None,
                        help="Filter status (analyze) or new status (update)")
    parser.add_argument("--show-all", action="store_true",
                        help="Show all issues in analyze mode")
    # ── IIS update args ──
    parser.add_argument("--issue", type=int, default=None,
                        help="Issue number for update")
    parser.add_argument("--repo", default="upstream",
                        choices=["upstream", "fork"],
                        help="Target repo for update (default: upstream)")
    parser.add_argument("--description", default=None,
                        help="Description for update iteration entry")
    parser.add_argument("--pr", type=int, default=None,
                        help="PR number for update")
    parser.add_argument("--branch", default=None,
                        help="Branch name for update")
    parser.add_argument("--release", default=None,
                        help="Release tag for update (e.g. v1.81.0)")
    parser.add_argument("--release-url", default=None,
                        help="Release download URL for update")
    parser.add_argument("--post-comment", action="store_true",
                        help="Post templated comment to GitHub")
    parser.add_argument("--notes", default=None,
                        help="Notes to add/update on the issue")
    parser.add_argument("--add-to-roadmap", action="store_true",
                        help="Add issue to _roadmap.json")
    # ── IIS report args ──
    parser.add_argument("--include-all", action="store_true",
                        help="Include full issue inventory in report")
    parser.add_argument("--no-save", action="store_true",
                        help="Print report to console only, do not save file")

    args = parser.parse_args()

    # ── IIS subcommands (no Claude/build preflight needed) ──
    if args.mode == "sync":
        issue_numbers = None
        if args.issues:
            issue_numbers = [int(n.strip()) for n in args.issues.split(",")]
        iis_sync(repos=args.repos, issue_numbers=issue_numbers,
                 include_closed=args.include_closed)
        return

    if args.mode == "analyze":
        iis_analyze(show_all=args.show_all, waiting_only=args.waiting_only,
                    priority_filter=args.priority, status_filter=args.status)
        return

    if args.mode == "update":
        if not args.issue:
            print("ERROR: --issue is required for update mode")
            sys.exit(1)
        if not args.status:
            print("ERROR: --status is required for update mode")
            sys.exit(1)
        iis_update(
            issue_num=args.issue, new_status=args.status,
            repo=args.repo, description=args.description,
            pr=args.pr, branch=args.branch,
            release=args.release, release_url=args.release_url,
            post_comment=args.post_comment,
            priority=args.priority, notes=args.notes,
            add_to_roadmap=args.add_to_roadmap,
        )
        return

    if args.mode == "report":
        iis_report(include_all=args.include_all, no_save=args.no_save)
        return

    if args.mode == "status":
        show_status()
        return

    # ── Orchestrator modes (all, issues, warnings) ──
    # Apply agent CLI overrides
    global GEMINI_MODEL, CODEX_MODEL
    if args.agent:
        for key in AGENT_CONFIG:
            AGENT_CONFIG[key] = args.agent
        log.info("Agent override: ALL tasks using %s", args.agent)
    if args.gemini_model:
        GEMINI_MODEL = args.gemini_model
        log.info("Gemini model override: %s", GEMINI_MODEL)
    if args.codex_model:
        CODEX_MODEL = args.codex_model
        log.info("Codex model override: %s", CODEX_MODEL)

    # Preflight checks
    agents_needed = set(AGENT_CONFIG.values())
    if "codex" in agents_needed and not shutil.which("codex"):
        print("ERROR: 'codex' CLI not found in PATH.  Install: npm i -g @openai/codex")
        sys.exit(1)
    if "claude" in agents_needed and not shutil.which("claude"):
        print("ERROR: 'claude' CLI not found in PATH.  Install Claude Code first.")
        sys.exit(1)
    if "gemini" in agents_needed and not shutil.which("gemini"):
        print("ERROR: 'gemini' CLI not found in PATH.  Install Gemini CLI first.")
        sys.exit(1)
    if AGENT_FALLBACK_ENABLED:
        for cli_name in ("codex", "claude", "gemini"):
            if not shutil.which(cli_name):
                log.warning("%s CLI not found — fallback to %s disabled", cli_name, cli_name)
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
            flux_warnings(status, dry_run=args.dry_run, max_files=args.max_files,
                          squash=args.squash, max_passes=args.max_passes,
                          parallel=args.parallel)

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
