#!/usr/bin/env python3
"""Fork Intelligence - triage the upstream fork network for changes worth importing.

The upstream project has ~1600 forks. A small minority carry real work that was
never offered back upstream. This tool finds that work, filters out noise and
hostile code, and produces a ranked queue of import candidates.

Nothing is ever imported automatically: the pipeline only proposes.
Fork code is never executed - only metadata and text patches are fetched.

Pipeline (each stage writes JSON and is resumable):

    discover -> diverge -> screen -> triage -> report
                                              mark

Usage:
    python fork_intel.py discover [--since-months 6] [--limit N]
    python fork_intel.py diverge  [--limit N]
    python fork_intel.py screen   [--limit N]
    python fork_intel.py triage   [--limit N] [--agent claude|codex|gemini]
    python fork_intel.py report
    python fork_intel.py mark --sha <sha> --decision imported|rejected|deferred [--note "..."]
    python fork_intel.py status
"""

import sys

if sys.platform == "win32":
    sys.stdout.reconfigure(encoding="utf-8", errors="replace")
    sys.stderr.reconfigure(encoding="utf-8", errors="replace")

import argparse
import json
import re
import shutil
import subprocess
import time
from datetime import datetime, timedelta, timezone
from pathlib import Path

# ---------------------------------------------------------------- paths/config

REPO_ROOT = Path(r"D:\github\mRemoteNG")
BASE_DIR = REPO_ROOT / ".project-roadmap" / "fork-intel"
DB_DIR = BASE_DIR / "db"
FORKS_DIR = DB_DIR / "forks"
CANDIDATES_DIR = DB_DIR / "candidates"
REPORTS_DIR = BASE_DIR / "reports"
RULES_FILE = BASE_DIR / "rules" / "security_rules.json"
META_FILE = DB_DIR / "_meta.json"
EXCLUDE_FILE = BASE_DIR / "EXCLUDE.json"
IMPORT_QUEUE = BASE_DIR / "IMPORT_QUEUE.md"
ISSUES_DB_FORK = REPO_ROOT / ".project-roadmap" / "issues-db" / "fork"

UPSTREAM = "mRemoteNG/mRemoteNG"
OUR_FORK = "robertpopa22/mRemoteNG"

VERSION = "1.0.0"

# API calls are counted so a run can report its own cost against the 5000/h limit.
_api_calls = 0


# --------------------------------------------------------------------- helpers

def log(msg):
    print(msg, flush=True)


def read_json(path, default=None):
    p = Path(path)
    if not p.exists():
        return default
    try:
        return json.loads(p.read_text(encoding="utf-8-sig"))
    except (json.JSONDecodeError, OSError) as exc:
        log(f"  ! unreadable {p.name}: {exc}")
        return default


def write_json(path, data):
    p = Path(path)
    p.parent.mkdir(parents=True, exist_ok=True)
    p.write_text(json.dumps(data, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")


def utc_now():
    return datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ")


def gh_json(endpoint, paginate=False):
    """Call the GitHub REST API through the gh CLI. Returns parsed JSON or None.

    Never raises: a failed call is a skipped item, not a crashed run.
    """
    global _api_calls
    args = ["gh", "api", endpoint]
    if paginate:
        args.append("--paginate")
    try:
        proc = subprocess.run(args, capture_output=True, text=True, timeout=120,
                              encoding="utf-8", errors="replace")
        _api_calls += 1
    except (subprocess.TimeoutExpired, FileNotFoundError) as exc:
        log(f"  ! gh api failed ({endpoint}): {exc}")
        return None
    if proc.returncode != 0:
        return None
    try:
        return json.loads(proc.stdout)
    except json.JSONDecodeError:
        return None


def git(args, cwd=REPO_ROOT):
    """Run a read-only git command in our own repository."""
    try:
        proc = subprocess.run(["git"] + args, cwd=str(cwd), capture_output=True,
                              text=True, timeout=120, encoding="utf-8", errors="replace")
    except (subprocess.TimeoutExpired, FileNotFoundError):
        return ""
    return proc.stdout if proc.returncode == 0 else ""


def fork_slug(full_name):
    """owner/repo -> owner__repo, safe as a file name."""
    return full_name.replace("/", "__")


def load_meta():
    return read_json(META_FILE, default={
        "system": "mRemoteNG Fork Intelligence",
        "version": VERSION,
        "upstream": UPSTREAM,
        "our_fork": OUR_FORK,
        "last_run": {},
    }) or {}


def save_meta(meta, stage, stats):
    meta["version"] = VERSION
    meta.setdefault("last_run", {})[stage] = {
        "at": utc_now(),
        "api_calls": _api_calls,
        **stats,
    }
    write_json(META_FILE, meta)


def load_exclude():
    return read_json(EXCLUDE_FILE, default={
        "_description": "Permanent denylist plus the memory of past decisions.",
        "owners": [],
        "commits": {},
    }) or {"owners": [], "commits": {}}


def load_rules():
    rules = read_json(RULES_FILE)
    if rules is None:
        log(f"! missing rules file: {RULES_FILE}")
        sys.exit(2)
    return rules


def iter_forks():
    for path in sorted(FORKS_DIR.glob("*.json")):
        data = read_json(path)
        if data:
            yield path, data


def iter_candidates():
    for path in sorted(CANDIDATES_DIR.glob("*.json")):
        data = read_json(path)
        if data:
            yield path, data


# -------------------------------------------------------------------- discover

def cmd_discover(args):
    """Enumerate upstream forks and keep the ones pushed within the window."""
    meta = load_meta()
    exclude = load_exclude()
    excluded_owners = {o.lower() for o in exclude.get("owners", [])}

    cutoff = (datetime.now(timezone.utc) - timedelta(days=30 * args.since_months))
    cutoff_str = cutoff.strftime("%Y-%m-%dT%H:%M:%SZ")
    log(f"Discovering forks of {UPSTREAM} pushed since {cutoff_str[:10]}")

    seen, kept, skipped_stale, skipped_excluded = set(), 0, 0, 0
    page = 1
    while True:
        batch = gh_json(f"repos/{UPSTREAM}/forks?per_page=100&page={page}&sort=newest")
        if not batch:
            break
        for repo in batch:
            full_name = repo.get("full_name")
            if not full_name or full_name in seen:
                continue
            seen.add(full_name)

            if full_name == OUR_FORK:
                continue
            owner = full_name.split("/", 1)[0]
            if owner.lower() in excluded_owners:
                skipped_excluded += 1
                continue

            pushed = repo.get("pushed_at") or ""
            if pushed < cutoff_str:
                skipped_stale += 1
                continue

            path = FORKS_DIR / f"{fork_slug(full_name)}.json"
            existing = read_json(path, default={}) or {}
            record = {
                "full_name": full_name,
                "owner": owner,
                "html_url": repo.get("html_url"),
                "default_branch": repo.get("default_branch"),
                "pushed_at": pushed,
                "created_at": repo.get("created_at"),
                "size_kb": repo.get("size"),
                "stars": repo.get("stargazers_count", 0),
                "open_issues": repo.get("open_issues_count", 0),
                "archived": repo.get("archived", False),
                # divergence fields are filled by `diverge`
                "status": existing.get("status", "discovered"),
                "ahead_by": existing.get("ahead_by"),
                "behind_by": existing.get("behind_by"),
                "commits": existing.get("commits", []),
                "discovered_at": existing.get("discovered_at", utc_now()),
                "last_seen": utc_now(),
            }
            # A fork that moved since the last diverge run must be re-examined.
            if existing.get("pushed_at") and existing["pushed_at"] != pushed:
                record["status"] = "discovered"
            write_json(path, record)
            kept += 1
            if args.limit and kept >= args.limit:
                break
        if args.limit and kept >= args.limit:
            break
        if len(batch) < 100:
            break
        page += 1

    log(f"  forks seen:      {len(seen)}")
    log(f"  candidates kept: {kept}")
    log(f"  stale skipped:   {skipped_stale}")
    log(f"  excluded owners: {skipped_excluded}")
    log(f"  api calls:       {_api_calls}")
    save_meta(meta, "discover", {
        "cutoff": cutoff_str, "forks_seen": len(seen), "candidates": kept,
        "skipped_stale": skipped_stale, "skipped_excluded": skipped_excluded,
    })
    return 0


# --------------------------------------------------------------------- diverge

def cmd_diverge(args):
    """Compare each candidate fork against upstream and record its own commits."""
    meta = load_meta()
    upstream_repo = gh_json(f"repos/{UPSTREAM}")
    base_branch = (upstream_repo or {}).get("default_branch")
    if not base_branch:
        log("! cannot read upstream default branch")
        return 2
    log(f"Comparing forks against {UPSTREAM}@{base_branch}")

    processed = ahead = flat = failed = 0
    for path, fork in iter_forks():
        if fork.get("status") not in ("discovered", None):
            continue
        if args.limit and processed >= args.limit:
            break
        processed += 1

        branch = fork.get("default_branch")
        if not branch:
            detail = gh_json(f"repos/{fork['full_name']}")
            branch = (detail or {}).get("default_branch")
            fork["default_branch"] = branch
        if not branch:
            fork["status"] = "error"
            fork["error"] = "no default branch"
            write_json(path, fork)
            failed += 1
            continue

        owner = fork["owner"]
        cmp_data = gh_json(f"repos/{UPSTREAM}/compare/{base_branch}...{owner}:{branch}")
        if cmp_data is None:
            fork["status"] = "error"
            fork["error"] = "compare failed"
            write_json(path, fork)
            failed += 1
            continue

        fork["base_branch"] = base_branch
        fork["ahead_by"] = cmp_data.get("ahead_by", 0)
        fork["behind_by"] = cmp_data.get("behind_by", 0)
        fork["merge_base"] = (cmp_data.get("merge_base_commit") or {}).get("sha")
        fork["compared_at"] = utc_now()
        fork.pop("error", None)

        commits = []
        for c in cmp_data.get("commits", []):
            commit = c.get("commit", {})
            author = commit.get("author", {}) or {}
            commits.append({
                "sha": c.get("sha"),
                "subject": (commit.get("message") or "").split("\n", 1)[0],
                "author_name": author.get("name"),
                "author_login": (c.get("author") or {}).get("login"),
                "date": author.get("date"),
                "parents": len(c.get("parents") or []),
                "html_url": c.get("html_url"),
            })
        fork["commits"] = commits

        if fork["ahead_by"] == 0:
            fork["status"] = "no-divergence"
            flat += 1
        else:
            fork["status"] = "diverged"
            ahead += 1
        write_json(path, fork)
        log(f"  {fork['full_name']:<45} ahead={fork['ahead_by']:<5} commits={len(commits)}")

    log(f"  processed:     {processed}")
    log(f"  diverged:      {ahead}")
    log(f"  no divergence: {flat}")
    log(f"  failed:        {failed}")
    log(f"  api calls:     {_api_calls}")
    save_meta(meta, "diverge", {
        "processed": processed, "diverged": ahead,
        "no_divergence": flat, "failed": failed,
    })
    return 0


# ---------------------------------------------------------------------- screen

def normalize_subject(subject):
    """Lowercase, strip conventional-commit prefixes, issue refs and punctuation.

    Used to recognise a change we already carry, even when the wording differs
    slightly from the fork's commit message.
    """
    s = (subject or "").lower().strip()
    s = re.sub(r"^(fix|feat|chore|docs|refactor|perf|test|style|build|ci)(\([^)]*\))?:\s*", "", s)
    s = re.sub(r"#\d+", " ", s)
    s = re.sub(r"\b[0-9a-f]{7,40}\b", " ", s)
    s = re.sub(r"[^a-z0-9 ]+", " ", s)
    return " ".join(s.split())


def our_history_subjects():
    """Normalized subjects of every commit we already carry."""
    out = git(["log", "--all", "--format=%s"])
    return {normalize_subject(line) for line in out.splitlines() if line.strip()}


def is_noise(commit, rules, our_subjects):
    """Layer A. Return a reason string when the commit should be dropped."""
    noise = rules["noise"]
    subject = commit.get("subject") or ""
    norm = normalize_subject(subject)

    if commit.get("parents", 1) >= 2:
        return "merge commit"

    author = f"{commit.get('author_login') or ''} {commit.get('author_name') or ''}".lower()
    for bot in noise["bot_authors"]:
        if bot.lower() in author:
            return f"bot author ({bot})"

    for maintainer in noise["upstream_maintainers"]:
        if maintainer.lower() == (commit.get("author_login") or "").lower() or \
           maintainer.lower() == (commit.get("author_name") or "").lower():
            return f"upstream maintainer ({maintainer}) - merge-base artifact"

    for pattern in noise["subject_patterns"]:
        if re.search(pattern, subject.strip(), re.IGNORECASE):
            return f"noise subject pattern ({pattern})"

    if len(subject.strip()) < noise["min_subject_length"]:
        return "subject too short to describe a change"

    if norm and norm in our_subjects:
        return "already in our history (subject match)"

    return None


def screen_files(files, rules):
    """Layer B. Return (flags, stats) for a commit's file list."""
    sec = rules["security"]
    flags = []
    stats = {"files": len(files), "additions": 0, "deletions": 0, "binary_files": 0}

    for f in files:
        name = f.get("filename", "")
        lower = name.lower()
        stats["additions"] += f.get("additions", 0) or 0
        stats["deletions"] += f.get("deletions", 0) or 0
        patch = f.get("patch")

        for rule in sec["path_rules"]:
            if any(g.lower() in lower for g in rule["glob"]):
                flags.append({"id": rule["id"], "severity": rule["severity"],
                              "file": name, "reason": rule["reason"]})

        if any(lower.endswith(ext) for ext in sec["binary_extensions"]):
            stats["binary_files"] += 1
            flags.append({"id": "binary-artifact", "severity": "critical", "file": name,
                          "reason": "committed binary cannot be reviewed (OpenSSF Scorecard)"})
        elif patch is None and f.get("status") == "added":
            stats["binary_files"] += 1
            flags.append({"id": "opaque-file", "severity": "high", "file": name,
                          "reason": "added file has no reviewable text diff"})

        if not patch:
            continue
        added_lines = "\n".join(l[1:] for l in patch.splitlines() if l.startswith("+"))
        for rule in sec["content_patterns"]:
            if re.search(rule["regex"], added_lines):
                flags.append({"id": rule["id"], "severity": rule["severity"], "file": name,
                              "reason": rule["reason"]})

    # de-duplicate (rule id, file) pairs
    seen, unique = set(), []
    for fl in flags:
        key = (fl["id"], fl["file"])
        if key not in seen:
            seen.add(key)
            unique.append(fl)
    return unique, stats


def cmd_screen(args):
    """Drop noise deterministically, then security-screen the survivors."""
    meta = load_meta()
    rules = load_rules()
    our_subjects = our_history_subjects()
    log(f"Screening against {len(our_subjects)} known commit subjects from our history")

    total = dropped = kept = quarantined = reused = 0
    drop_reasons = {}

    for path, fork in iter_forks():
        if fork.get("status") not in ("diverged", "screened"):
            continue
        screened_shas = []
        for commit in fork.get("commits", []):
            sha = commit.get("sha")
            if not sha:
                continue
            total += 1
            if args.limit and kept >= args.limit:
                break

            cand_path = CANDIDATES_DIR / f"{sha[:10]}.json"
            existing = read_json(cand_path)
            if existing and not args.refresh:
                reused += 1
                if existing.get("status") != "dropped":
                    screened_shas.append(sha)
                continue

            reason = is_noise(commit, rules, our_subjects)
            if reason:
                dropped += 1
                drop_reasons[reason.split("(")[0].strip()] = \
                    drop_reasons.get(reason.split("(")[0].strip(), 0) + 1
                write_json(cand_path, {
                    "sha": sha, "fork": fork["full_name"], "owner": fork["owner"],
                    "subject": commit.get("subject"), "author_name": commit.get("author_name"),
                    "author_login": commit.get("author_login"), "date": commit.get("date"),
                    "html_url": commit.get("html_url"),
                    "status": "dropped", "drop_reason": reason,
                    "screened_at": utc_now(),
                })
                continue

            detail = gh_json(f"repos/{fork['full_name']}/commits/{sha}")
            if detail is None:
                continue
            files = detail.get("files") or []
            flags, stats = screen_files(files, rules)
            status = "quarantine" if flags else "screened"
            if flags:
                quarantined += 1
            else:
                kept += 1
            screened_shas.append(sha)

            write_json(cand_path, {
                "sha": sha,
                "fork": fork["full_name"],
                "owner": fork["owner"],
                "subject": commit.get("subject"),
                "body": (detail.get("commit", {}).get("message") or "")[:2000],
                "author_name": commit.get("author_name"),
                "author_login": commit.get("author_login"),
                "date": commit.get("date"),
                "html_url": commit.get("html_url"),
                "stats": stats,
                "files": [{"filename": f.get("filename"), "status": f.get("status"),
                           "additions": f.get("additions"), "deletions": f.get("deletions")}
                          for f in files],
                "patch": "\n".join(
                    f"--- {f.get('filename')}\n{f.get('patch') or '(no text diff)'}"
                    for f in files[:20])[:60000],
                "security_flags": flags,
                "status": status,
                "screened_at": utc_now(),
            })
            log(f"  {status:<10} {sha[:8]} {fork['owner']:<18} {(commit.get('subject') or '')[:52]}")

        fork["status"] = "screened"
        fork["screened_shas"] = screened_shas
        write_json(path, fork)

    log(f"  commits seen:  {total}")
    log(f"  reused cache:  {reused}")
    log(f"  dropped:       {dropped}")
    for reason, n in sorted(drop_reasons.items(), key=lambda kv: -kv[1]):
        log(f"      {n:>4}  {reason}")
    log(f"  quarantined:   {quarantined}")
    log(f"  clean:         {kept}")
    log(f"  api calls:     {_api_calls}")
    save_meta(meta, "screen", {
        "commits_seen": total, "dropped": dropped, "quarantined": quarantined,
        "clean": kept, "reused": reused,
    })
    return 0


# ---------------------------------------------------------------------- triage

# Prompts are fed through stdin, never as arguments: a diff-sized prompt blows past
# the Windows command-line limit (WinError 206). Executables are resolved with
# shutil.which so npm shims (codex.cmd, gemini.cmd) are found too.
AGENT_ARGS = {
    "claude": ["-p", "--output-format", "json"],
    "codex": ["exec"],
    "gemini": ["-y"],
}


GROK_MODEL = "grok-4.5"
GROK_ENDPOINT = "https://api.x.ai/v1/chat/completions"


def grok_run(prompt, timeout=600, model=GROK_MODEL):
    """Ask xAI Grok through the OpenAI-compatible REST API.

    Grok has no CLI, so this posts JSON with curl. The key comes from the
    XAI_API_KEY environment variable and is never written to disk or logged.
    """
    import os
    import tempfile

    if not os.environ.get("XAI_API_KEY"):
        log("  ! XAI_API_KEY is not set, skipping grok")
        return None
    payload = {"model": model, "messages": [{"role": "user", "content": prompt}]}
    # The body goes through a temp file: a diff-sized payload does not fit in an
    # argument, and shells here mangle '$' inside inline JSON.
    with tempfile.NamedTemporaryFile("w", suffix=".json", delete=False,
                                     encoding="utf-8") as handle:
        json.dump(payload, handle)
        body_path = handle.name
    try:
        proc = subprocess.run(
            ["curl", "-s", "-S", GROK_ENDPOINT,
             "-H", "Authorization: Bearer " + os.environ["XAI_API_KEY"],
             "-H", "Content-Type: application/json",
             "--data-binary", "@" + body_path],
            capture_output=True, text=True, timeout=timeout,
            encoding="utf-8", errors="replace")
    except (subprocess.TimeoutExpired, OSError) as exc:
        log(f"  ! grok failed: {exc}")
        return None
    finally:
        try:
            Path(body_path).unlink()
        except OSError:
            pass

    if proc.returncode != 0:
        log(f"  ! grok curl exit {proc.returncode}: {(proc.stderr or '').strip()[:160]}")
        return None
    try:
        data = json.loads(proc.stdout)
    except json.JSONDecodeError:
        log("  ! grok returned non-JSON")
        return None
    if "error" in data:
        log(f"  ! grok api error: {str(data['error'])[:160]}")
        return None
    try:
        return data["choices"][0]["message"]["content"]
    except (KeyError, IndexError):
        log("  ! grok response had no message content")
        return None


def agent_run(agent, prompt, timeout=600):
    """Run one AI CLI with the prompt on stdin. Returns its text answer, or None."""
    if agent == "grok":
        return grok_run(prompt, timeout=timeout)
    args = AGENT_ARGS.get(agent)
    if args is None:
        return None
    exe = shutil.which(agent)
    if not exe:
        log(f"  ! {agent} is not on PATH, skipping")
        return None
    try:
        proc = subprocess.run([exe] + args, input=prompt, capture_output=True, text=True,
                              timeout=timeout, encoding="utf-8", errors="replace")
    except (subprocess.TimeoutExpired, OSError) as exc:
        log(f"  ! {agent} failed: {exc}")
        return None
    if proc.returncode != 0:
        log(f"  ! {agent} exit {proc.returncode}: {(proc.stderr or '').strip()[:160]}")
        return None
    out = (proc.stdout or "").strip()
    if agent == "claude":
        try:
            return json.loads(out).get("result", out)
        except json.JSONDecodeError:
            return out
    return out


def extract_json_array(text):
    """Pull the first JSON array out of a model answer."""
    if not text:
        return None
    fence = re.search(r"```(?:json)?\s*(\[.*?\])\s*```", text, re.DOTALL)
    raw = fence.group(1) if fence else None
    if raw is None:
        start, depth = text.find("["), 0
        if start < 0:
            return None
        for i in range(start, len(text)):
            depth += (text[i] == "[") - (text[i] == "]")
            if depth == 0:
                raw = text[start:i + 1]
                break
    try:
        parsed = json.loads(raw) if raw else None
    except json.JSONDecodeError:
        return None
    return parsed if isinstance(parsed, list) else None


def our_open_issue_titles(limit=60):
    titles = []
    for path in sorted(ISSUES_DB_FORK.glob("[0-9]*.json")):
        data = read_json(path)
        if data and data.get("state") == "open":
            titles.append(f"#{data['number']} {data.get('title', '')[:90]}")
        if len(titles) >= limit:
            break
    return titles


def related_history(subject, max_lines=8):
    """Commit subjects from our history that share keywords with this change."""
    words = [w for w in normalize_subject(subject).split() if len(w) > 4][:4]
    hits = []
    for word in words:
        out = git(["log", "--all", "--format=%h %s", f"--grep={word}", "-i", "-8"])
        hits.extend(line for line in out.splitlines() if line.strip())
    seen, unique = set(), []
    for line in hits:
        if line not in seen:
            seen.add(line)
            unique.append(line)
    return unique[:max_lines]


def build_triage_prompt(batch, issue_titles):
    parts = [
        "You are triaging commits found in third-party forks of mRemoteNG, to decide "
        "whether our own fork (robertpopa22/mRemoteNG, ~1600 commits ahead of upstream) "
        "should import them.",
        "",
        "Our fork already fixed a great deal upstream never did, so the most common correct "
        "answer is that a change is already covered or no longer applies. Be strict.",
        "",
        "Open issues in our tracker:",
        *(f"  {t}" for t in issue_titles),
        "",
        "For EACH commit below return one JSON object. Output ONLY a JSON array, nothing else:",
        '[{"sha":"<sha>","category":"bugfix|feature|perf|security|docs|refactor|chore",',
        ' "maps_to_issue":<our issue number or null>,"already_in_our_fork":true|false,',
        ' "value":1-5,"effort":1-5,"risk":1-5,"applies_cleanly":"likely|conflict|rewrite",',
        ' "action":"IMPORT|REIMPLEMENT|WATCH|REJECT","rationale":"<= 30 words"}]',
        "",
        "value: user-visible benefit to our fork. effort: work to land it here. "
        "risk: chance it breaks something or needs deep review.",
        "",
    ]
    for cand in batch:
        parts.append("=" * 70)
        parts.append(f"sha: {cand['sha']}")
        parts.append(f"fork: {cand['fork']}")
        parts.append(f"subject: {cand.get('subject')}")
        stats = cand.get("stats", {})
        parts.append(f"files: {stats.get('files')} (+{stats.get('additions')}/-{stats.get('deletions')})")
        parts.append("touched: " + ", ".join(f["filename"] for f in cand.get("files", [])[:12]))
        if cand.get("security_flags"):
            parts.append("security flags: " + ", ".join(
                f"{f['id']}({f['severity']})" for f in cand["security_flags"]))
        rel = related_history(cand.get("subject", ""))
        if rel:
            parts.append("similar commits already in our history:")
            parts.extend(f"  {line}" for line in rel)
        parts.append("diff (truncated):")
        parts.append((cand.get("patch") or "")[:6000])
    return "\n".join(parts)


def cmd_triage(args):
    """Ask an AI agent to judge each screened commit against our fork."""
    meta = load_meta()
    pending = [(p, c) for p, c in iter_candidates()
               if c.get("status") in ("screened", "quarantine") and
               (args.refresh or not c.get("triage"))]
    if args.limit:
        pending = pending[:args.limit]
    if not pending:
        log("Nothing to triage.")
        return 0

    issue_titles = our_open_issue_titles()
    log(f"Triaging {len(pending)} candidates with {args.agent} (batch {args.batch})")

    judged = failed = 0
    for i in range(0, len(pending), args.batch):
        chunk = pending[i:i + args.batch]
        prompt = build_triage_prompt([c for _, c in chunk], issue_titles)
        verdicts = None
        for agent in [args.agent] + [a for a in ("codex", "gemini", "claude") if a != args.agent]:
            verdicts = extract_json_array(agent_run(agent, prompt))
            if verdicts:
                break
            log(f"  ! {agent} returned no usable JSON, trying next agent")
        if not verdicts:
            failed += len(chunk)
            continue

        by_sha = {v.get("sha"): v for v in verdicts if isinstance(v, dict)}
        for path, cand in chunk:
            verdict = by_sha.get(cand["sha"]) or by_sha.get(cand["sha"][:10])
            if not verdict:
                failed += 1
                continue
            cand["triage"] = {
                "category": verdict.get("category"),
                "maps_to_issue": verdict.get("maps_to_issue"),
                "already_in_our_fork": bool(verdict.get("already_in_our_fork")),
                "value": int(verdict.get("value") or 0),
                "effort": int(verdict.get("effort") or 0),
                "risk": int(verdict.get("risk") or 0),
                "applies_cleanly": verdict.get("applies_cleanly"),
                "action": verdict.get("action"),
                "rationale": verdict.get("rationale"),
                "agent": agent,
                "at": utc_now(),
            }
            write_json(path, cand)
            judged += 1
            log(f"  {verdict.get('action', '?'):<10} v{verdict.get('value')} "
                f"e{verdict.get('effort')} r{verdict.get('risk')}  "
                f"{cand['sha'][:8]} {(cand.get('subject') or '')[:48]}")
        time.sleep(1)

    log(f"  judged: {judged}   failed: {failed}")
    save_meta(meta, "triage", {"judged": judged, "failed": failed, "agent": args.agent})
    return 0


# ------------------------------------------------------------------ preapprove

# Reviewers are asked to judge against where the project is actually going, not
# against the abstract merit of the patch.
PROJECT_DIRECTION = """\
Our fork robertpopa22/mRemoteNG is a maintained community fork of mRemoteNG:
- .NET 10, WinForms, builds only through build.ps1 (COM references break dotnet build)
- ~1600 commits ahead of upstream; zero analyzer warnings; 6341 tests must stay green
- priorities: correctness and stability of existing protocols (RDP/SSH/VNC), SQL/MariaDB
  connection storage, credential security, startup performance, DPI and focus handling
- we do NOT want: new external dependencies, telemetry, interactive tests, large
  speculative rewrites, features that duplicate what we already implemented differently
- every import must survive full build + full test suite and be maintainable by us"""

REVIEW_TEMPLATE = """\
Read-only review - this is a COUNTER-OPINION ONLY. Do NOT modify any files, do NOT build, \
do NOT run git add/git commit/git push or any other repository-mutating command. \
Judge independently; do not assume the analysis you are shown is correct.

{direction}

A commit from a third-party fork is proposed for import into our fork. Decide whether it \
should be pre-approved for a human to land.

fork: {fork}
commit: {sha}
subject: {subject}
files ({nfiles}, +{adds}/-{dels}): {files}
prior automated triage (may be wrong): {triage}

diff (truncated):
{patch}

Answer with EXACTLY one JSON object and nothing else:
{{"vote":"APPROVE|REJECT|NEEDS_HUMAN","aligned_with_direction":true|false,\
"concern":"<the single biggest risk, <= 25 words>","reason":"<why, <= 30 words>"}}

Vote APPROVE only if the change is genuinely useful to THIS fork, is unlikely to already \
be implemented here, and is small and clear enough that a maintainer can verify it quickly. \
Vote NEEDS_HUMAN when it is valuable but needs judgement. Default to REJECT when unsure."""


def extract_json_object(text):
    """Pull the first JSON object out of a model answer."""
    if not text:
        return None
    fence = re.search(r"```(?:json)?\s*(\{.*?\})\s*```", text, re.DOTALL)
    raw = fence.group(1) if fence else None
    if raw is None:
        start, depth = text.find("{"), 0
        if start < 0:
            return None
        for i in range(start, len(text)):
            depth += (text[i] == "{") - (text[i] == "}")
            if depth == 0:
                raw = text[start:i + 1]
                break
    try:
        parsed = json.loads(raw) if raw else None
    except json.JSONDecodeError:
        return None
    return parsed if isinstance(parsed, dict) else None


def votes_are_split(votes):
    """True when reviewers disagree - the case a third opinion can settle.

    Unanimous approval or unanimous refusal needs no arbiter. A reviewer that
    failed to answer is not a disagreement either: nothing was said.
    """
    answered = [v.get("vote") for v in votes if v.get("vote") not in (None, "NO_ANSWER")]
    if len(answered) < 2:
        return False
    approvals = sum(1 for v in answered if v == "APPROVE")
    return 0 < approvals < len(answered)


def consensus_decision(votes, has_security_flags):
    """Decide whether a change may skip a full manual investigation.

    Without an arbiter the rule is unanimity. When a third opinion was fetched to
    settle a split, a clear majority is enough - that is the whole point of asking.
    A missing or unparsable answer counts as dissent, never as consent, and a
    security flag can never be voted away.
    """
    if not votes or has_security_flags:
        return "manual-review"
    if any(v.get("aligned") is False for v in votes):
        return "manual-review"

    approvals = sum(1 for v in votes if v.get("vote") == "APPROVE")
    if approvals == len(votes):
        return "pre-approved"
    if any(v.get("arbiter") for v in votes) and approvals > len(votes) / 2:
        return "pre-approved"
    return "manual-review"


def cmd_preapprove(args):
    """Ask independent model families to vote on each import candidate.

    Pre-approval is a consensus gate, not an import: unanimous APPROVE from every
    reviewer, plus no security flag, means a maintainer can land the change after a
    quick read instead of a full investigation. Any dissent routes it to manual review
    and the dissenting reason is kept and shown.
    """
    meta = load_meta()
    rules = load_rules()
    reviewers = [a.strip() for a in args.reviewers.split(",") if a.strip()]
    if len(reviewers) < 2:
        log("! pre-approval needs at least two independent reviewers")
        return 2

    pending = []
    for path, cand in iter_candidates():
        if cand.get("status") == "dropped" or not cand.get("triage"):
            continue
        if cand.get("preapproval") and not args.refresh:
            continue
        _, tier, _ = score_candidate(cand, rules)
        # Quarantined changes are reviewed too. They can never be pre-approved
        # (consensus_decision blocks on a security flag), but the votes tell the
        # maintainer whether the diff is worth their reading time at all.
        if tier in ("A", "B", "Q"):
            pending.append((path, cand, tier))
    if args.limit:
        pending = pending[:args.limit]
    if not pending:
        log("Nothing to pre-approve.")
        return 0

    log(f"Pre-approving {len(pending)} candidates with reviewers: {', '.join(reviewers)}")
    approved = manual = 0
    for path, cand, tier in pending:
        stats = cand.get("stats") or {}
        triage = cand.get("triage") or {}
        prompt = REVIEW_TEMPLATE.format(
            direction=PROJECT_DIRECTION,
            fork=cand["fork"], sha=cand["sha"], subject=cand.get("subject"),
            nfiles=stats.get("files"), adds=stats.get("additions"), dels=stats.get("deletions"),
            files=", ".join(f["filename"] for f in cand.get("files", [])[:12]),
            triage=f"{triage.get('category')} value={triage.get('value')} "
                   f"risk={triage.get('risk')} action={triage.get('action')}",
            patch=(cand.get("patch") or "")[:8000])

        def collect(reviewer, arbiter=False):
            verdict = extract_json_object(agent_run(reviewer, prompt, timeout=args.timeout))
            return {
                "reviewer": reviewer,
                "vote": (verdict or {}).get("vote", "NO_ANSWER"),
                "aligned": (verdict or {}).get("aligned_with_direction"),
                "concern": (verdict or {}).get("concern"),
                "reason": (verdict or {}).get("reason"),
                "arbiter": arbiter,
            }

        votes = [collect(reviewer) for reviewer in reviewers]

        # A split is exactly the case a third, unrelated model can settle. Asking
        # it on every candidate would just add cost and noise, so it is fetched
        # only when the first two actually disagree.
        if args.arbiter and votes_are_split(votes):
            log(f"    split verdict - asking {args.arbiter} to arbitrate")
            votes.append(collect(args.arbiter, arbiter=True))

        decision = consensus_decision(votes, bool(cand.get("security_flags")))

        cand["preapproval"] = {
            "decision": decision,
            "tier": tier,
            "votes": votes,
            "dissent": [f"{v['reviewer']}: {v['vote']} - {v.get('reason') or v.get('concern') or ''}"
                        for v in votes if v["vote"] != "APPROVE"],
            "at": utc_now(),
        }
        write_json(path, cand)
        if decision == "pre-approved":
            approved += 1
        else:
            manual += 1
        log(f"  {decision:<14} [{'/'.join(v['vote'][:4] for v in votes)}] "
            f"{cand['sha'][:8]} {(cand.get('subject') or '')[:46]}")

    log(f"  pre-approved: {approved}   manual review: {manual}")
    save_meta(meta, "preapprove", {"pre_approved": approved, "manual": manual,
                                   "reviewers": reviewers})
    return 0


# --------------------------------------------------------------- report / mark

def score_candidate(cand, rules):
    """Deterministic score and tier. The AI supplies inputs; this decides.

    Hard gates come first so no triage verdict, however enthusiastic, can push a
    flagged change into the ready-to-import tier.
    """
    triage = cand.get("triage") or {}
    thresholds = rules["thresholds"]
    value = triage.get("value") or 0
    effort = triage.get("effort") or 0
    risk = triage.get("risk") or 0
    novelty = 0 if triage.get("already_in_our_fork") else 2

    score = 3 * value + 2 * novelty - 2 * risk - effort

    if triage.get("already_in_our_fork") or triage.get("action") == "REJECT":
        return score, "D", "already covered or rejected at triage"
    if cand.get("security_flags"):
        worst = max((f["severity"] for f in cand["security_flags"]),
                    key=lambda s: {"critical": 3, "high": 2, "medium": 1}.get(s, 0))
        return score, "Q", f"security review required ({worst})"

    stats = cand.get("stats") or {}
    too_big = (stats.get("files", 0) > thresholds["max_files_for_auto_tier_a"] or
               (stats.get("additions", 0) + stats.get("deletions", 0))
               > thresholds["max_lines_for_auto_tier_a"])

    if triage.get("action") == "REIMPLEMENT" or triage.get("applies_cleanly") == "rewrite":
        return score, "B", "port the idea, the patch will not apply"
    if score >= thresholds["score_tier_a"] and not too_big:
        return score, "A", "ready to cherry-pick"
    if score >= thresholds["score_tier_b"]:
        return score, "B", "worth doing, needs work"
    if score >= thresholds["score_tier_c"]:
        return score, "C", "keep an eye on it"
    return score, "D", "not worth it"


TIER_TITLES = {
    "A": "Tier A - ready to cherry-pick",
    "B": "Tier B - worth porting by hand",
    "C": "Tier C - watch list",
    "Q": "Quarantine - security review required before anything else",
    "D": "Tier D - rejected",
}


def cmd_report(args):
    """Rank every judged candidate and write the report plus the import queue."""
    meta = load_meta()
    rules = load_rules()
    exclude = load_exclude()
    decided = exclude.get("commits", {})

    tiers = {t: [] for t in TIER_TITLES}
    untriaged = 0
    for _, cand in iter_candidates():
        if cand.get("status") == "dropped":
            continue
        if cand["sha"] in decided:
            continue
        if not cand.get("triage"):
            untriaged += 1
            continue
        score, tier, why = score_candidate(cand, rules)
        cand["_score"], cand["_tier"], cand["_why"] = score, tier, why
        tiers[tier].append(cand)

    for tier in tiers:
        tiers[tier].sort(key=lambda c: -c["_score"])

    today = datetime.now(timezone.utc).strftime("%Y-%m-%d")
    report_path = REPORTS_DIR / f"{today}_fork-radar.md"

    lines = [
        f"# Fork Radar - {today}",
        "",
        f"Upstream `{UPSTREAM}` - forks scanned for changes worth importing into `{OUR_FORK}`.",
        "",
        "| Tier | Count |",
        "|---|---|",
    ]
    for tier in ("A", "B", "C", "Q", "D"):
        lines.append(f"| {TIER_TITLES[tier]} | {len(tiers[tier])} |")
    if untriaged:
        lines.append(f"| not yet triaged | {untriaged} |")
    lines.append("")

    for tier in ("A", "B", "Q", "C", "D"):
        if not tiers[tier]:
            continue
        lines.append(f"## {TIER_TITLES[tier]}")
        lines.append("")
        for cand in tiers[tier]:
            triage = cand.get("triage") or {}
            lines.append(f"### `{cand['sha'][:10]}` {cand.get('subject')}")
            lines.append("")
            lines.append(f"- **fork:** [{cand['fork']}]({cand.get('html_url')}) "
                         f"by {cand.get('author_name') or cand.get('author_login')}")
            stats = cand.get("stats") or {}
            lines.append(f"- **size:** {stats.get('files', '?')} files "
                         f"(+{stats.get('additions', '?')}/-{stats.get('deletions', '?')})")
            lines.append(f"- **score {cand['_score']}** - {cand['_why']}")
            lines.append(f"- **triage:** {triage.get('category')} | value {triage.get('value')} "
                         f"| effort {triage.get('effort')} | risk {triage.get('risk')} "
                         f"| applies {triage.get('applies_cleanly')} | {triage.get('action')}")
            if triage.get("maps_to_issue"):
                lines.append(f"- **our issue:** #{triage['maps_to_issue']}")
            if triage.get("rationale"):
                lines.append(f"- **why:** {triage['rationale']}")
            pre = cand.get("preapproval")
            if pre:
                votes = " / ".join(f"{v['reviewer']}:{v['vote']}" for v in pre["votes"])
                lines.append(f"- **pre-approval:** **{pre['decision'].upper()}** ({votes})")
                for line in pre.get("dissent", []):
                    lines.append(f"  - dissent - {line}")
            if cand.get("security_flags"):
                lines.append("- **security flags:**")
                for flag in cand["security_flags"]:
                    lines.append(f"  - `{flag['id']}` ({flag['severity']}) "
                                 f"in `{flag['file']}` - {flag['reason']}")
            lines.append("")

    lines.append("---")
    lines.append("")
    lines.append("Generated by `.project-roadmap/fork-intel/fork_intel.py report`. "
                 "Nothing here has been imported: every entry needs a human decision.")
    report_path.write_text("\n".join(lines) + "\n", encoding="utf-8")

    queue = [
        "# Import Queue",
        "",
        f"Generated {today} from the fork radar. **Nothing is applied automatically.**",
        "",
        "Both mRemoteNG and its forks are GPL-2.0, so importing is licence-compatible. "
        "`git cherry-pick` preserves the original author and `-x` records the source commit; "
        "add a `Ported-from:` trailer with the upstream URL so the origin stays visible.",
        "",
        "After a decision, record it so future runs stop proposing it:",
        "",
        "```bash",
        "python .project-roadmap/fork-intel/fork_intel.py mark --sha <sha> "
        "--decision imported|rejected|deferred --note \"why\"",
        "```",
        "",
    ]
    if not tiers["A"] and not tiers["B"]:
        queue.append("_Nothing queued in this run._")
    for tier in ("A", "B"):
        if not tiers[tier]:
            continue
        queue.append(f"## {TIER_TITLES[tier]}")
        queue.append("")
        for cand in tiers[tier]:
            triage = cand.get("triage") or {}
            owner = cand["owner"]
            pre = cand.get("preapproval")
            badge = ""
            if pre:
                badge = " - **PRE-APPROVED**" if pre["decision"] == "pre-approved" \
                    else " - needs manual review"
            queue.append(f"### `{cand['sha'][:10]}` {cand.get('subject')}{badge}")
            queue.append("")
            queue.append(f"{triage.get('rationale', '')}  ")
            queue.append(f"Source: {cand.get('html_url')}")
            if pre:
                queue.append("")
                queue.append("Counter-opinions: " +
                             " / ".join(f"{v['reviewer']} **{v['vote']}**" for v in pre["votes"]))
                for line in pre.get("dissent", []):
                    queue.append(f"- {line}")
            queue.append("")
            if tier == "A":
                queue.append("```bash")
                queue.append(f"git remote add fi-{owner} https://github.com/{cand['fork']}.git")
                queue.append(f"git fetch fi-{owner} --depth=50 {cand['sha']}")
                queue.append(f"git cherry-pick -x {cand['sha']}")
                queue.append("# then: build.ps1 + run-tests.ps1 -Headless before committing anything")
                queue.append("```")
            else:
                queue.append("Port by hand - the patch will not apply cleanly over our tree. "
                             "Read the source diff, reimplement, and credit the original author "
                             "in the commit body.")
            queue.append("")
    IMPORT_QUEUE.write_text("\n".join(queue) + "\n", encoding="utf-8")

    for tier in ("A", "B", "C", "Q", "D"):
        log(f"  {TIER_TITLES[tier]:<52} {len(tiers[tier])}")
    if untriaged:
        log(f"  {'not yet triaged':<52} {untriaged}")
    log(f"  report: {report_path}")
    log(f"  queue:  {IMPORT_QUEUE}")
    save_meta(meta, "report", {t: len(tiers[t]) for t in tiers})
    return 0


def cmd_mark(args):
    """Record a human decision so the candidate stops resurfacing."""
    exclude = load_exclude()
    exclude.setdefault("commits", {})[args.sha] = {
        "decision": args.decision,
        "note": args.note or "",
        "at": utc_now(),
    }
    write_json(EXCLUDE_FILE, exclude)
    log(f"Recorded {args.sha[:10]} as {args.decision}")
    return 0


# ---------------------------------------------------------------------- status

def cmd_status(args):
    meta = load_meta()
    forks = list(iter_forks())
    by_status = {}
    for _, f in forks:
        by_status[f.get("status", "?")] = by_status.get(f.get("status", "?"), 0) + 1

    cands = list(iter_candidates())
    by_cand = {}
    for _, c in cands:
        by_cand[c.get("status", "?")] = by_cand.get(c.get("status", "?"), 0) + 1

    log(f"Fork Intelligence v{VERSION}")
    log(f"  upstream: {UPSTREAM}")
    log(f"  forks tracked: {len(forks)}")
    for k in sorted(by_status):
        log(f"    {k:<18} {by_status[k]}")
    log(f"  candidates: {len(cands)}")
    for k in sorted(by_cand):
        log(f"    {k:<18} {by_cand[k]}")
    for stage, info in (meta.get("last_run") or {}).items():
        log(f"  last {stage:<9} {info.get('at')}  (api {info.get('api_calls')})")
    return 0


# ------------------------------------------------------------------------ main

def build_parser():
    parser = argparse.ArgumentParser(
        prog="fork_intel.py",
        description="Triage the upstream fork network for changes worth importing.")
    sub = parser.add_subparsers(dest="command", required=True)

    p_disc = sub.add_parser("discover", help="enumerate active forks of upstream")
    p_disc.add_argument("--since-months", type=int, default=6,
                        help="only keep forks pushed within this window (default 6)")
    p_disc.add_argument("--limit", type=int, default=0, help="stop after N candidates")
    p_disc.set_defaults(func=cmd_discover)

    p_div = sub.add_parser("diverge", help="compare candidate forks against upstream")
    p_div.add_argument("--limit", type=int, default=0, help="stop after N forks")
    p_div.set_defaults(func=cmd_diverge)

    p_scr = sub.add_parser("screen", help="drop noise and security-screen the rest")
    p_scr.add_argument("--limit", type=int, default=0, help="stop after N clean commits")
    p_scr.add_argument("--refresh", action="store_true",
                       help="re-screen commits that already have a candidate file")
    p_scr.set_defaults(func=cmd_screen)

    p_tri = sub.add_parser("triage", help="AI judgement on screened commits")
    p_tri.add_argument("--limit", type=int, default=0, help="stop after N candidates")
    p_tri.add_argument("--batch", type=int, default=5, help="commits per AI call (default 5)")
    p_tri.add_argument("--agent", default="claude", choices=list(AGENT_ARGS),
                       help="primary agent (others are used as fallback)")
    p_tri.add_argument("--refresh", action="store_true", help="re-triage already judged commits")
    p_tri.set_defaults(func=cmd_triage)

    p_pre = sub.add_parser("preapprove",
                           help="independent counter-opinions vote on import candidates")
    p_pre.add_argument("--reviewers", default="codex,gemini",
                       help="comma-separated agents, at least two (default codex,gemini)")
    p_pre.add_argument("--arbiter", default="grok",
                       help="third model family asked only when reviewers disagree "
                            "(default grok; empty string disables)")
    p_pre.add_argument("--limit", type=int, default=0, help="stop after N candidates")
    p_pre.add_argument("--timeout", type=int, default=600, help="per-reviewer timeout in seconds")
    p_pre.add_argument("--refresh", action="store_true", help="re-run on already voted candidates")
    p_pre.set_defaults(func=cmd_preapprove)

    p_rep = sub.add_parser("report", help="rank candidates and write report + import queue")
    p_rep.set_defaults(func=cmd_report)

    p_mark = sub.add_parser("mark", help="record a human decision on a candidate")
    p_mark.add_argument("--sha", required=True)
    p_mark.add_argument("--decision", required=True,
                        choices=["imported", "rejected", "deferred"])
    p_mark.add_argument("--note", default="")
    p_mark.set_defaults(func=cmd_mark)

    p_stat = sub.add_parser("status", help="show what the local database holds")
    p_stat.set_defaults(func=cmd_status)

    return parser


def main(argv=None):
    for d in (FORKS_DIR, CANDIDATES_DIR, REPORTS_DIR):
        d.mkdir(parents=True, exist_ok=True)
    args = build_parser().parse_args(argv)
    return args.func(args)


if __name__ == "__main__":
    sys.exit(main())
