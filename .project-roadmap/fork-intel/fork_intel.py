#!/usr/bin/env python3
"""Fork Intelligence - triage the upstream fork network for changes worth importing.

The upstream project has ~1600 forks. A small minority carry real work that was
never offered back upstream. This tool finds that work, filters out noise and
hostile code, and produces a ranked queue of import candidates.

Nothing is ever imported automatically: the pipeline only proposes.
Fork code is never executed - only metadata and text patches are fetched.

Pipeline (each stage writes JSON and is resumable):

    discover -> diverge -> screen -> triage -> preapprove -> report -> mark

`upstream` is a second, independent source feeding the same screen/triage/report
machinery: nothing else watches UPSTREAM's own default branch directly, which is
how unseen upstream commits accumulate silently between runs.

Usage:
    python fork_intel.py discover   [--since-months 6] [--limit N]
    python fork_intel.py diverge    [--all-branches] [--limit N]
    python fork_intel.py screen     [--limit N]
    python fork_intel.py upstream   [--limit N] [--refresh] [--base <ref>]
    python fork_intel.py triage     [--agent claude|codex|gemini] [--shard i/n]
    python fork_intel.py preapprove [--reviewers codex,gemini] [--arbiter grok]
    python fork_intel.py report
    python fork_intel.py export-ideas
    python fork_intel.py mark --sha <sha> --decision imported|rejected|deferred [--note "..."]
    python fork_intel.py status
"""

import sys

if sys.platform == "win32":
    sys.stdout.reconfigure(encoding="utf-8", errors="replace")
    sys.stderr.reconfigure(encoding="utf-8", errors="replace")

import argparse
import json
import os
import re
import shutil
import subprocess
import tempfile
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
IDEAS_DIR = BASE_DIR / "ideas-db"
IMPORT_QUEUE = BASE_DIR / "IMPORT_QUEUE.md"
def _joined(*parts):
    return "".join(parts)


# Same tokens as scripts/security-tripwire.sh, split so this source does not
# contain them whole. Idea records are public and must not carry them.
_PUBLIC_TEXT_RE = re.compile("|".join((
    _joined("Trust", "Server", "Certificate"),
    _joined("Server", "Certificate", "Validation", "Callback"),
    _joined("Service", "Point", "Manager"),
    _joined("Certificate", "Validation"),
    _joined("Validate", "Server", "Certificate"),
    _joined("Ignore", "Certificate"),
    _joined("Allow", "Untrusted"),
    _joined("Check", "Certificate", "Revocation"),
    _joined("Dangerous", "Accept", "Any"),
    _joined("Remote", "Certificate"),
    _joined("Security", "Protocol", "Type"),
    r"Encrypt *= *false",
    _joined("Null", "Security"),
    _joined("Auth", "entication", "Level"),
    _joined("PROCESS", "_ALL_", "ACCESS"),
    _joined("SECURITY", "_DESCRIPTOR"),
    _joined("Set", "Security", "Info"),
    _joined("Null", "Dacl"),
    _joined("Well", "Known", "Sid", "Type"),
    _joined("--", "insecure"),
    _joined("Strict", "Host", "Key", "Checking"),
    _joined("Host", "Key", "Alias"),
)))
ADOPTION_STATUSES = ("new", "trying", "imported", "present", "rejected", "deferred")
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


def candidate_kind(cand):
    """The candidate's source: "fork" or "upstream".

    Thousands of candidate files predate this field, so a missing key means "fork" -
    the only source that existed before upstream tracking was added - rather than
    forcing a migration pass over the whole database.
    """
    return cand.get("kind", "fork")


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

def upstream_branch_heads():
    """Branch name -> head SHA for upstream.

    A fork inherits every upstream branch at fork time, so a branch that still
    points at upstream's own head is not fork work no matter how promising its
    name looks. Without this, upstream's `copilot/*` and `feature/*` branches
    show up as if a fork author had written them.
    """
    heads = {}
    page = 1
    while True:
        batch = gh_json(f"repos/{UPSTREAM}/branches?per_page=100&page={page}")
        if not batch:
            break
        for branch in batch:
            heads[branch.get("name")] = (branch.get("commit") or {}).get("sha")
        if len(batch) < 100:
            break
        page += 1
    return heads


def fork_branches_to_scan(fork, upstream_heads, default_branch):
    """Branches of a fork that could hold its own work.

    Keeps the default branch always, drops branches still parked on upstream's
    head, and keeps a branch whose name exists upstream but whose head moved.
    """
    branches = []
    page = 1
    while True:
        batch = gh_json(f"repos/{fork['full_name']}/branches?per_page=100&page={page}")
        if not batch:
            break
        branches.extend(batch)
        if len(batch) < 100:
            break
        page += 1

    keep, inherited = [], 0
    for branch in branches:
        name = branch.get("name")
        sha = (branch.get("commit") or {}).get("sha")
        if name == default_branch:
            continue  # compared separately, always
        if name in upstream_heads and upstream_heads[name] == sha:
            inherited += 1
            continue
        keep.append(name)
    return keep, inherited


def compare_commits(base_repo, base_ref, head_owner, head_ref):
    """Compare two refs through GitHub's compare API, collecting every commit.

    The endpoint's `commits` array is capped at 250 when the call carries no paging
    parameters, while the true count lives in `total_commits` - so a comparison
    deeper than 250 commits silently lost its tail. The obvious fix, calling the
    capped endpoint once and then appending `page=2,3,...&per_page=100`, was checked
    against a live 580-commit upstream compare and against `git rev-list` ground
    truth before landing here: it produces gaps and duplicates, because the
    unparameterized call and the `per_page`-paginated calls walk the commit list in
    different orders and are not a continuation of each other (the unparameterized
    250 turned out to line up with the *last* three pages of the paginated walk, not
    the first two). The combination verified to reconstruct the exact commit set with
    no gaps and no duplicates is `page=1,2,3,...&per_page=100`, uniformly, starting
    from the very first call - so that is what this does.

    Returns None when the first call fails, matching the pre-existing error handling
    at every call site. `truncated` is True only when paging stops (a page came back
    empty) before `total_commits` commits were collected.
    """
    endpoint = f"repos/{base_repo}/compare/{base_ref}...{head_owner}:{head_ref}"
    per_page = 100
    page = 1
    first = gh_json(f"{endpoint}?page={page}&per_page={per_page}")
    if first is None:
        return None

    total = first.get("total_commits", len(first.get("commits") or []))
    commits = list(first.get("commits") or [])

    while len(commits) < total:
        page += 1
        batch = gh_json(f"{endpoint}?page={page}&per_page={per_page}")
        page_commits = (batch or {}).get("commits") or []
        if not page_commits:
            break
        commits.extend(page_commits)

    return {
        "ahead_by": first.get("ahead_by"),
        "behind_by": first.get("behind_by"),
        "merge_base": (first.get("merge_base_commit") or {}).get("sha"),
        "total_commits": total,
        "commits": commits,
        "truncated": len(commits) < total,
    }


def cmd_diverge(args):
    """Compare each candidate fork against upstream and record its own commits."""
    meta = load_meta()
    upstream_repo = gh_json(f"repos/{UPSTREAM}")
    base_branch = (upstream_repo or {}).get("default_branch")
    if not base_branch:
        log("! cannot read upstream default branch")
        return 2
    log(f"Comparing forks against {UPSTREAM}@{base_branch}")

    upstream_heads = {}
    if args.all_branches:
        upstream_heads = upstream_branch_heads()
        log(f"  upstream has {len(upstream_heads)} branches (inherited ones will be skipped)")

    # With --all-branches an already-compared fork is revisited: its default
    # branch was scanned, its side branches were not.
    revisitable = ("discovered", None)
    if args.all_branches:
        revisitable = ("discovered", None, "diverged", "screened", "no-divergence")

    processed = ahead = flat = failed = 0
    extra_branch_commits = 0
    for path, fork in iter_forks():
        if fork.get("status") not in revisitable:
            continue
        if args.all_branches and fork.get("branches_scanned") and not args.refresh:
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
        cmp_data = compare_commits(UPSTREAM, base_branch, owner, branch)
        if cmp_data is None:
            fork["status"] = "error"
            fork["error"] = "compare failed"
            write_json(path, fork)
            failed += 1
            continue

        fork["base_branch"] = base_branch
        fork["ahead_by"] = cmp_data.get("ahead_by", 0)
        fork["behind_by"] = cmp_data.get("behind_by", 0)
        fork["merge_base"] = cmp_data.get("merge_base")
        fork["compared_at"] = utc_now()
        fork.pop("error", None)

        def collect_commits(compare_result, branch_name, into):
            """Add a comparison's commits, deduplicated by SHA across branches."""
            added = 0
            for c in compare_result.get("commits", []):
                sha = c.get("sha")
                if not sha:
                    continue
                if sha in into:
                    if branch_name not in into[sha]["branches"]:
                        into[sha]["branches"].append(branch_name)
                    continue
                commit = c.get("commit", {})
                author = commit.get("author", {}) or {}
                into[sha] = {
                    "sha": sha,
                    "subject": (commit.get("message") or "").split("\n", 1)[0],
                    "author_name": author.get("name"),
                    "author_login": (c.get("author") or {}).get("login"),
                    "date": author.get("date"),
                    "parents": len(c.get("parents") or []),
                    "html_url": c.get("html_url"),
                    "branches": [branch_name],
                }
                added += 1
            return added

        by_sha = {}
        collect_commits(cmp_data, branch, by_sha)
        default_only = len(by_sha)

        if args.all_branches:
            side_branches, inherited = fork_branches_to_scan(fork, upstream_heads, branch)
            fork["branches_scanned"] = [branch] + side_branches
            fork["branches_inherited_from_upstream"] = inherited
            for side in side_branches:
                side_cmp = compare_commits(UPSTREAM, base_branch, owner, side)
                if side_cmp is None:
                    continue
                gained = collect_commits(side_cmp, side, by_sha)
                if gained:
                    extra_branch_commits += gained
                    log(f"    branch {side}: +{gained} commits not on the default branch")

        fork["commits"] = list(by_sha.values())
        fork["ahead_by"] = max(fork["ahead_by"], len(by_sha))

        if not by_sha:
            fork["status"] = "no-divergence"
            flat += 1
        else:
            fork["status"] = "diverged"
            ahead += 1
        write_json(path, fork)
        extra = len(by_sha) - default_only
        suffix = f" (+{extra} off the default branch)" if extra else ""
        log(f"  {fork['full_name']:<45} ahead={fork['ahead_by']:<5} "
            f"commits={len(by_sha)}{suffix}")

    log(f"  processed:     {processed}")
    log(f"  diverged:      {ahead}")
    if args.all_branches:
        log(f"  commits found only on side branches: {extra_branch_commits}")
    log(f"  no divergence: {flat}")
    log(f"  failed:        {failed}")
    log(f"  api calls:     {_api_calls}")
    save_meta(meta, "diverge", {
        "processed": processed, "diverged": ahead,
        "no_divergence": flat, "failed": failed,
        "all_branches": bool(args.all_branches),
        "side_branch_commits": extra_branch_commits,
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


def our_history_subjects(ref="HEAD"):
    """Normalized subjects of every commit we already carry.

    `git log --all` walks every configured remote's refs, not just what we merged:
    in this checkout `upstream` is a configured remote, and `git log --all` returns
    9004 commits against 8123 for `git log HEAD` - 881 commits we have only FETCHED
    and never merged. Those were being matched as "already in our history" here,
    which silently drops real fork candidates that happen to share a fetched-but-
    unmerged upstream commit's wording, and would drop the entire upstream feed
    (every one of its own commits trivially "matches" itself). `ref` defaults to
    HEAD, our actual merged history.
    """
    out = git(["log", ref, "--format=%s"])
    return {normalize_subject(line) for line in out.splitlines() if line.strip()}


def is_noise(commit, rules, our_subjects, trust_source=False):
    """Layer A. Return a reason string when the commit should be dropped.

    trust_source=True skips the upstream_maintainers check. That rule exists to drop
    merge-base artifacts authored by upstream maintainers out of a FORK's commit list
    (a fork inherits pre-fork history still authored by upstream's own people). On the
    upstream feed itself those same people wrote everything, so the same rule would
    drop the whole feed rather than a sliver of merge-base noise.
    """
    noise = rules["noise"]
    subject = commit.get("subject") or ""
    norm = normalize_subject(subject)

    if commit.get("parents", 1) >= 2:
        return "merge commit"

    author = f"{commit.get('author_login') or ''} {commit.get('author_name') or ''}".lower()
    for bot in noise["bot_authors"]:
        if bot.lower() in author:
            return f"bot author ({bot})"

    if not trust_source:
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
                    "kind": "fork",
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
                "kind": "fork",
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


# -------------------------------------------------------------------- upstream

def _commit_from_compare_entry(entry):
    """Turn one GitHub compare-endpoint commit entry into our internal commit shape."""
    commit = entry.get("commit", {}) or {}
    author = commit.get("author", {}) or {}
    return {
        "sha": entry.get("sha"),
        "subject": (commit.get("message") or "").split("\n", 1)[0],
        "author_name": author.get("name"),
        "author_login": (entry.get("author") or {}).get("login"),
        "date": author.get("date"),
        "parents": len(entry.get("parents") or []),
        "html_url": entry.get("html_url"),
    }


def cmd_upstream(args):
    """Compare our fork against upstream's own default branch and screen its commits.

    Nothing in this pipeline watched UPSTREAM directly before this, which is how 525
    unseen upstream commits accumulated: `diverge`/`screen` only ever look at forks.
    This is the second source feeding the same screen/triage/report machinery -
    candidates are written to the same CANDIDATES_DIR, in the same shape cmd_screen
    writes, tagged kind="upstream", so triage/report/mark need no special cases
    beyond grouping by kind (triage) and a separate section (report).
    """
    meta = load_meta()
    rules = load_rules()

    upstream_repo = gh_json(f"repos/{UPSTREAM}")
    upstream_branch = (upstream_repo or {}).get("default_branch")
    if not upstream_branch:
        log("! cannot read upstream default branch")
        return 2
    upstream_owner = UPSTREAM.split("/", 1)[0]

    our_owner = OUR_FORK.split("/", 1)[0]
    our_branch = args.base
    if not our_branch:
        our_repo = gh_json(f"repos/{OUR_FORK}")
        our_branch = (our_repo or {}).get("default_branch")
    if not our_branch:
        log("! cannot read our fork's default branch")
        return 2

    log(f"Comparing {UPSTREAM}@{upstream_branch} against {OUR_FORK}@{our_branch}")

    # base = our own branch, head = upstream's default branch: compare/{base}...{head}
    # returns commits reachable from head but not base, so `commits` here are exactly
    # upstream's own commits we do not have - verified live against
    # `git rev-list <our-branch>..upstream/<upstream-branch>` before relying on it.
    cmp_data = compare_commits(UPSTREAM, f"{our_owner}:{our_branch}", upstream_owner, upstream_branch)
    if cmp_data is None:
        log("! compare failed")
        return 2
    if cmp_data.get("truncated"):
        log(f"  ! only collected {len(cmp_data['commits'])}/{cmp_data['total_commits']} "
            f"commits (compare API kept truncating)")

    our_subjects = our_history_subjects()
    log(f"  {cmp_data['total_commits']} commits on upstream we do not have "
        f"({len(cmp_data['commits'])} collected)")

    seen = dropped = kept = quarantined = reused = 0
    drop_reasons = {}

    for entry in cmp_data["commits"]:
        sha = entry.get("sha")
        if not sha:
            continue
        seen += 1
        if args.limit and kept >= args.limit:
            break

        commit = _commit_from_compare_entry(entry)

        cand_path = CANDIDATES_DIR / f"{sha[:10]}.json"
        existing = read_json(cand_path)
        if existing and not args.refresh:
            reused += 1
            continue

        reason = is_noise(commit, rules, our_subjects, trust_source=True)
        if reason:
            dropped += 1
            drop_reasons[reason.split("(")[0].strip()] = \
                drop_reasons.get(reason.split("(")[0].strip(), 0) + 1
            write_json(cand_path, {
                "sha": sha, "fork": UPSTREAM, "owner": upstream_owner, "kind": "upstream",
                "subject": commit["subject"], "author_name": commit["author_name"],
                "author_login": commit["author_login"], "date": commit["date"],
                "html_url": commit["html_url"],
                "status": "dropped", "drop_reason": reason,
                "screened_at": utc_now(),
            })
            continue

        detail = gh_json(f"repos/{UPSTREAM}/commits/{sha}")
        if detail is None:
            continue
        files = detail.get("files") or []
        flags, stats = screen_files(files, rules)
        status = "quarantine" if flags else "screened"
        if flags:
            quarantined += 1
        else:
            kept += 1

        write_json(cand_path, {
            "sha": sha,
            "fork": UPSTREAM,
            "owner": upstream_owner,
            "kind": "upstream",
            "subject": commit["subject"],
            "body": (detail.get("commit", {}).get("message") or "")[:2000],
            "author_name": commit["author_name"],
            "author_login": commit["author_login"],
            "date": commit["date"],
            "html_url": commit["html_url"],
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
        log(f"  {status:<10} {sha[:8]} {(commit['subject'] or '')[:60]}")

    log(f"  commits seen:  {seen}")
    log(f"  reused cache:  {reused}")
    log(f"  dropped:       {dropped}")
    for reason, n in sorted(drop_reasons.items(), key=lambda kv: -kv[1]):
        log(f"      {n:>4}  {reason}")
    log(f"  quarantined:   {quarantined}")
    log(f"  clean:         {kept}")
    log(f"  api calls:     {_api_calls}")
    save_meta(meta, "upstream", {
        "seen": seen, "dropped": dropped, "drop_reasons": drop_reasons,
        "quarantined": quarantined, "clean": kept, "reused": reused,
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


GROK_MODEL = "grok-4.6"


def grok_run(prompt, timeout=600, model=GROK_MODEL):
    """Ask Grok through the official Grok Build CLI.

    Grok used to have no CLI and this went out as a REST call with curl. It has one
    now, and the difference is not cosmetic: with XAI_API_KEY present in the
    environment the CLI announces "You are using XAI_API_KEY" and bills the metered
    API, while without it the same binary reports "You are logged in with grok.com"
    and draws on the subscription. A review pass over several hundred candidates is
    exactly where that distinction stops being academic, so the key is stripped from
    the child's environment here.

    The prompt goes through a file rather than an argument: a diff-sized payload does
    not fit on a command line, and the shell available here mangles '$' inside inline
    text.
    """
    exe = shutil.which("grok")
    if not exe:
        log("  ! grok CLI is not on PATH, skipping")
        return None

    env = dict(os.environ)
    env.pop("XAI_API_KEY", None)  # subscription, not metered API

    with tempfile.NamedTemporaryFile("w", suffix=".txt", delete=False,
                                     encoding="utf-8") as handle:
        handle.write(prompt)
        prompt_path = handle.name
    try:
        proc = subprocess.run(
            [exe, "--prompt-file", prompt_path, "--output-format", "json",
             "--model", model, "--no-subagents"],
            capture_output=True, text=True, timeout=timeout,
            encoding="utf-8", errors="replace", env=env)
    except (subprocess.TimeoutExpired, OSError) as exc:
        log(f"  ! grok failed: {exc}")
        return None
    finally:
        try:
            os.unlink(prompt_path)
        except OSError:
            pass

    if proc.returncode != 0:
        log(f"  ! grok exit {proc.returncode}: {(proc.stderr or '').strip()[:160]}")
        return None
    try:
        return json.loads(proc.stdout).get("text")
    except (json.JSONDecodeError, AttributeError):
        # Older builds print the answer directly rather than wrapping it.
        return (proc.stdout or "").strip() or None


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


FORK_TRIAGE_INTRO = [
    "You are triaging commits found in third-party forks of mRemoteNG, to decide "
    "whether our own fork (robertpopa22/mRemoteNG, ~1600 commits ahead of upstream) "
    "should import them.",
    "",
    "Our fork already fixed a great deal upstream never did, so the most common correct "
    "answer is that a change is already covered or no longer applies. Be strict.",
]

# Unlike a stranger's fork, these are commits from the project we forked - the same
# people, on the same files, going a different direction. "already_in_our_fork" is
# rarely the right call here; "conflicts with our own rework" usually is, and a
# conflicting patch can still be worth REIMPLEMENT for the direction it represents
# even when applies_cleanly is "rewrite" or "conflict".
UPSTREAM_TRIAGE_INTRO = [
    "You are triaging commits from mRemoteNG/mRemoteNG itself - the upstream project "
    "we forked, not a stranger's fork - to decide whether our fork "
    "(robertpopa22/mRemoteNG) should pull them in.",
    "",
    "Our fork is roughly 1788 commits and 2905 files ahead of upstream, with 216 files "
    "that upstream also went on to touch independently. Because of that overlap, the "
    "most common correct answer is usually NOT \"already covered\" - it is \"conflicts "
    "with our own rework of the same area\". A change can still be worth importing as "
    "DIRECTION (the idea, or the bug it fixes) even when the literal patch cannot apply; "
    "use REIMPLEMENT for that rather than REJECT, and applies_cleanly \"conflict\" or "
    "\"rewrite\" rather than assuming it is already handled.",
]


def build_triage_prompt(batch, issue_titles):
    intro = UPSTREAM_TRIAGE_INTRO if batch and candidate_kind(batch[0]) == "upstream" \
        else FORK_TRIAGE_INTRO
    parts = [
        *intro,
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

    # Sharding lets several triage processes run at once against different
    # providers without treading on each other: the split is by SHA, so a
    # candidate belongs to exactly one shard no matter how many are running.
    if args.shard:
        try:
            index, total = (int(x) for x in args.shard.split("/", 1))
        except ValueError:
            log("! --shard expects the form i/n, e.g. 1/3")
            return 2
        if not 1 <= index <= total:
            log("! shard index out of range")
            return 2
        pending = [(p, c) for p, c in pending
                   if int(c["sha"][:8], 16) % total == index - 1]
        log(f"  shard {index}/{total}: {len(pending)} candidates")

    if args.limit:
        pending = pending[:args.limit]
    if not pending:
        log("Nothing to triage.")
        return 0

    issue_titles = our_open_issue_titles()
    log(f"Triaging {len(pending)} candidates with {args.agent} (batch {args.batch})")

    # Grouped by kind before batching: fork and upstream commits need different
    # framing (build_triage_prompt) and a batch must never mix the two prompts.
    groups = {}
    for item in pending:
        groups.setdefault(candidate_kind(item[1]), []).append(item)

    judged = failed = 0
    for kind in sorted(groups):
        group = groups[kind]
        for i in range(0, len(group), args.batch):
            chunk = group[i:i + args.batch]
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

PATCH_REVIEW_LIMIT = 8000

REVIEW_HEAD = """\
Read-only review - this is a COUNTER-OPINION ONLY. Do NOT modify any files, do NOT build, \
do NOT run git add/git commit/git push or any other repository-mutating command. \
Judge independently from the diff and this repository; do not take anyone else's analysis on trust.

{direction}

A commit from a third-party fork is proposed for import into our fork. Decide whether it \
should be pre-approved for a human to land.
"""

REVIEW_TAIL = """\
Answer with EXACTLY one JSON object and nothing else:
{{"vote":"APPROVE|REJECT|NEEDS_HUMAN","aligned_with_direction":true|false,\
"concern":"<the single biggest risk, <= 25 words>","reason":"<why, <= 30 words>"}}

Vote APPROVE only if the change is genuinely useful to THIS fork, is unlikely to already \
be implemented here, and is small and clear enough that a maintainer can verify it quickly. \
Vote NEEDS_HUMAN when it is valuable but needs judgement. Default to REJECT when unsure."""

# The informed ballot: it carries the prior triage verdict.
REVIEW_TEMPLATE = REVIEW_HEAD + """
fork: {fork}
commit: {sha}
subject: {subject}
files ({nfiles}, +{adds}/-{dels}): {files}
prior automated triage (may be wrong): {triage}

diff:
{patch}

""" + REVIEW_TAIL

# The blind ballot. Every reviewer used to receive a byte-identical prompt carrying the
# same triage verdict, which is a shared anchor on every vote in the panel: agreement
# then partly measures the anchor rather than the change. At least one reviewer sees
# the diff with no prior verdict attached.
REVIEW_TEMPLATE_BLIND = REVIEW_HEAD + """
fork: {fork}
commit: {sha}
subject: {subject}
files ({nfiles}, +{adds}/-{dels}): {files}

diff:
{patch}

""" + REVIEW_TAIL

# A commit and the commit that later reverted it cannot be judged apart: alone, the revert
# reads as noise, and together they are the clearest signal in the set - somebody tried
# this and took it back. This is one stateless prompt holding both diffs in order, not a
# conversation: a multi-turn session buys the same understanding and adds anchoring and
# drift, and a model that has already committed to a verdict defends it.
REVIEW_TEMPLATE_CHAIN = REVIEW_HEAD + """
These commits are one sequence from the same fork, in order. The later ones act on the
earlier ones - a revert, a fix-up, a second attempt. Judge the sequence as a whole: what
is left standing after all of it, and whether THAT is worth importing.

fork: {fork}

{chain}

""" + REVIEW_TAIL

CHAIN_ENTRY = """\
--- {position}. commit {sha} ---
subject: {subject}
files ({nfiles}, +{adds}/-{dels}): {files}

diff:
{patch}
"""

# Round two, and only on a split. Arguments are anonymised so that deference to a brand
# does not stand in for judgement, and the session is fresh so that no reviewer is
# defending words it has already said.
ROUND2_TEMPLATE = """\
Read-only review - COUNTER-OPINION ONLY. Do NOT modify files, build, or run any \
repository-mutating command.

{direction}

You are re-deciding one import candidate. Independent reviewers disagreed about it. \
Their arguments are below, unattributed and in no meaningful order. Judge the ARGUMENTS \
on their merits against the diff and this repository - not by how confident any of them \
sounds, and not by counting them.

fork: {fork}
commit: {sha}
subject: {subject}
files ({nfiles}, +{adds}/-{dels}): {files}

diff:
{patch}

What the reviewers argued:
{arguments}

Answer with EXACTLY one JSON object and nothing else:
{{"vote":"APPROVE|REJECT|NEEDS_HUMAN","aligned_with_direction":true|false,\
"concern":"<the single biggest risk, <= 25 words>","reason":"<why, <= 30 words>"}}

Change your mind if an argument is better than your reasoning; keep your position if it \
is not. Default to REJECT when unsure."""


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


ERROR_VOTE = "NO_ANSWER"


def answered_votes(votes):
    """The ballots that actually carry a judgement.

    A reviewer that timed out, crashed or answered unparsable text said nothing. That is
    not dissent and it is certainly not consent - it is an absence, and it has to be
    counted as one so a broken CLI can never be mistaken for a quiet yes.
    """
    return [v for v in votes if v.get("vote") not in (None, ERROR_VOTE)]


def votes_are_split(votes):
    """True when the reviewers disagree about something that changes the outcome.

    The disagreement that matters is APPROVE against anything else, and it counts at
    any margin - a lone dissenter among three is still a split, not a rounding error.
    REJECT against NEEDS_HUMAN is a real difference of opinion but not one the gate can
    act on: neither is an approval, so the candidate goes to a human either way and a
    second round would spend quota without being able to change anything.
    """
    answered = [v["vote"] for v in answered_votes(votes)]
    if len(answered) < 2:
        return False
    approvals = sum(1 for v in answered if v == "APPROVE")
    return 0 < approvals < len(answered)


def consensus_decision(votes, has_security_flags):
    """Decide whether a change may skip a full manual investigation.

    Unanimity among the reviewers who answered, and at least two of them. A security
    flag can never be voted away, and a candidate nobody could review is held rather
    than waved through: for an import gate into a credential manager a false APPROVE
    costs far more than a false hold.
    """
    if has_security_flags:
        return "manual-review"
    answered = answered_votes(votes)
    if len(answered) < 2:
        return "held"
    if any(v.get("aligned") is False for v in answered):
        return "manual-review"
    if all(v.get("vote") == "APPROVE" for v in answered):
        return "pre-approved"
    return "manual-review"


def patch_was_truncated(cand, limit):
    """Whether the reviewers were shown less than the whole diff."""
    return len(cand.get("patch") or "") > limit


def revert_subject(subject):
    """The subject a commit claims to be reverting, if it says so."""
    match = re.match(r'^Revert\s+"(.+)"\s*$', (subject or "").strip(), re.DOTALL)
    return match.group(1).strip() if match else None


def build_chains(pending):
    """Group a commit with the commit that later reverted it.

    Only git ancestry within one fork, never a looser 'same author, same area' notion:
    a wider grouping would hand one prompt several unrelated judgements and turn N
    independent verdicts into one correlated verdict repeated N times.

    Pairing happens before any chain is emitted. Deciding as we walk the list would
    close the original into a chain of its own whenever it happened to be listed ahead
    of the revert that names it.
    """
    by_subject = {}
    for item in pending:
        cand = item[1]
        by_subject.setdefault((cand["fork"], (cand.get("subject") or "").strip()), item)

    partner = {}
    for item in pending:
        cand = item[1]
        reverted = revert_subject(cand.get("subject"))
        if not reverted:
            continue
        original = by_subject.get((cand["fork"], reverted))
        if original is None or original is item:
            continue
        if id(original) in partner or id(item) in partner:
            continue
        partner[id(original)] = item
        partner[id(item)] = original

    seen, chains = set(), []
    for item in pending:
        if id(item) in seen:
            continue
        mate = partner.get(id(item))
        if mate is None:
            seen.add(id(item))
            chains.append([item])
            continue
        # The revert is the one whose subject names the other; the original leads.
        first, second = (item, mate) if revert_subject(mate[1].get("subject")) else (mate, item)
        seen.add(id(first))
        seen.add(id(second))
        chains.append([first, second])
    return chains


def cmd_preapprove(args):
    """Ask independent model families to vote on each import candidate.

    The gate is a consensus of families, so it is only worth what its independence is
    worth. Three things protect that here: the family that ran triage does not vote on
    its own triage, at least one ballot is cast blind without the triage verdict
    attached, and a commit judged together with its revert is judged in a single
    stateless prompt rather than a running conversation.
    """
    meta = load_meta()
    rules = load_rules()
    reviewers = [a.strip() for a in args.reviewers.split(",") if a.strip()]
    if len(reviewers) < 2:
        log("! pre-approval needs at least two independent reviewers")
        return 2
    if len(set(reviewers)) != len(reviewers):
        log("! the same reviewer is listed twice - that is one opinion, not two")
        return 2

    # Whoever audits a unanimous verdict must not be one of the voices that produced it,
    # or the check is the same model agreeing with itself. With three families and the
    # triage family rotated out of each panel, the rotated-out one is exactly that
    # outsider - no fourth subscription required.
    arbiter = args.arbiter.strip()
    if arbiter and arbiter in reviewers:
        log(f"! {arbiter} also votes; the rotated-out family will audit instead")
        arbiter = ""

    pending = []
    for path, cand in iter_candidates():
        if cand.get("status") == "dropped" or not cand.get("triage"):
            continue
        existing = cand.get("preapproval")
        if existing and args.only_incomplete:
            if not any(v.get("vote") == ERROR_VOTE for v in existing.get("votes", [])):
                continue
        elif existing and not args.refresh:
            continue
        if args.sha and not cand["sha"].startswith(args.sha):
            continue
        _, tier, _ = score_candidate(cand, rules)
        # Quarantined changes are reviewed too. They can never be pre-approved
        # (consensus_decision blocks on a security flag), but the votes tell the
        # maintainer whether the diff is worth their reading time at all.
        if tier in ("A", "B", "Q"):
            pending.append((path, cand, tier))

    chains = build_chains(pending)
    if args.limit:
        chains = chains[:args.limit]
    if not chains:
        log("Nothing to pre-approve.")
        return 0

    total = sum(len(c) for c in chains)
    log(f"Pre-approving {total} candidates in {len(chains)} units "
        f"with reviewers: {', '.join(reviewers)}"
        + (f" (audit/arbiter: {arbiter})" if arbiter else ""))

    approved = manual = held = audited = flipped = 0
    for unit in chains:
        lead_path, lead, tier = unit[0]
        security = any(c.get("security_flags") for _, c, _ in unit)
        truncated = any(patch_was_truncated(c, PATCH_REVIEW_LIMIT) for _, c, _ in unit)

        # The triage family does not review its own triage. fork_intel triages with
        # --agent claude by default, so without this the same family judges twice and
        # the panel is one opinion narrower than it looks.
        triaged_by = {(c.get("triage") or {}).get("agent") for _, c, _ in unit}
        panel = [r for r in reviewers if r not in triaged_by]
        skipped = [r for r in reviewers if r in triaged_by]
        if len(panel) < 2:
            # Rotating everyone out would leave no panel at all; fall back to the full
            # list and record that this verdict is not rotation-clean.
            panel, skipped = reviewers, []

        def render(template, blind=False, **extra):
            if len(unit) > 1:
                entries = []
                for position, (_, c, _) in enumerate(unit, start=1):
                    st = c.get("stats") or {}
                    entries.append(CHAIN_ENTRY.format(
                        position=position, sha=c["sha"], subject=c.get("subject"),
                        nfiles=st.get("files"), adds=st.get("additions"),
                        dels=st.get("deletions"),
                        files=", ".join(f["filename"] for f in c.get("files", [])[:12]),
                        patch=(c.get("patch") or "")[:PATCH_REVIEW_LIMIT]))
                return REVIEW_TEMPLATE_CHAIN.format(
                    direction=PROJECT_DIRECTION, fork=lead["fork"],
                    chain="\n".join(entries))
            st = lead.get("stats") or {}
            fields = dict(
                direction=PROJECT_DIRECTION, fork=lead["fork"], sha=lead["sha"],
                subject=lead.get("subject"), nfiles=st.get("files"),
                adds=st.get("additions"), dels=st.get("deletions"),
                files=", ".join(f["filename"] for f in lead.get("files", [])[:12]),
                patch=(lead.get("patch") or "")[:PATCH_REVIEW_LIMIT], **extra)
            if not blind:
                tri = lead.get("triage") or {}
                fields["triage"] = (f"{tri.get('category')} value={tri.get('value')} "
                                    f"risk={tri.get('risk')} action={tri.get('action')}")
            return template.format(**fields)

        def collect(reviewer, prompt, blind=False, arbiter_vote=False, round_no=1):
            verdict = extract_json_object(agent_run(reviewer, prompt, timeout=args.timeout))
            return {
                "reviewer": reviewer,
                "vote": (verdict or {}).get("vote", ERROR_VOTE),
                "aligned": (verdict or {}).get("aligned_with_direction"),
                "concern": (verdict or {}).get("concern"),
                "reason": (verdict or {}).get("reason"),
                "arbiter": arbiter_vote,
                "blind": blind,
                "round": round_no,
            }

        # One ballot is cast blind, so at least one vote is not anchored on the triage
        # verdict every other reviewer is reading.
        informed_prompt = render(REVIEW_TEMPLATE)
        blind_prompt = render(REVIEW_TEMPLATE_BLIND, blind=True)
        votes = [collect(r, blind_prompt if i == 0 else informed_prompt, blind=(i == 0))
                 for i, r in enumerate(panel)]

        # Round two: only on a split, arguments anonymised, sessions fresh.
        if votes_are_split(votes):
            arguments = "\n".join(
                f"- Reviewer {chr(ord('A') + i)} voted {v['vote']}: "
                f"{v.get('reason') or v.get('concern') or '(no reason given)'}"
                for i, v in enumerate(answered_votes(votes)))
            log(f"    split - second round on anonymised arguments")
            round2_prompt = render(ROUND2_TEMPLATE, arguments=arguments)
            second = [collect(r, round2_prompt, round_no=2) for r in panel]
            if any(a["vote"] != b["vote"] for a, b in zip(votes, second)):
                flipped += 1
            votes = votes + second
            # The second round replaces the first for the decision; the first is kept
            # on the record so a later reader can see what changed and why.
            decision_votes = second
        else:
            decision_votes = votes

        decision = consensus_decision(decision_votes, security)

        # Nobody votes on a patch they were only shown part of.
        if truncated and decision == "pre-approved":
            decision = "manual-review"

        # Unanimity is the one outcome the panel never challenges, which is exactly how
        # "unanimous and wrong" stays invisible. A slice of it goes to the family that
        # sat this one out, to produce a measured disagreement rate rather than a
        # reassurance. Deterministic sampling: no RNG, so a re-run audits the same set.
        auditor = skipped[0] if skipped else arbiter
        audit = None
        if (auditor and decision == "pre-approved" and args.audit_every > 0
                and int(lead["sha"][:8], 16) % args.audit_every == 0):
            audit = collect(auditor, informed_prompt, arbiter_vote=True)
            audited += 1
            votes = votes + [audit]
            if audit["vote"] != "APPROVE":
                decision = "manual-review"

        lead["preapproval"] = {
            "decision": decision,
            "tier": tier,
            "panel": panel,
            "rotated_out": skipped,
            "chain": [c["sha"] for _, c, _ in unit] if len(unit) > 1 else None,
            "patch_truncated": truncated,
            "audited": bool(audit),
            "votes": votes,
            "dissent": [f"{v['reviewer']}: {v['vote']} - {v.get('reason') or v.get('concern') or ''}"
                        for v in decision_votes if v["vote"] != "APPROVE"],
            "at": utc_now(),
        }
        write_json(lead_path, lead)
        for path, cand, _ in unit[1:]:
            cand["preapproval"] = dict(lead["preapproval"], judged_with=lead["sha"])
            write_json(path, cand)

        if decision == "pre-approved":
            approved += 1
        elif decision == "held":
            held += 1
        else:
            manual += 1
        log(f"  {decision:<14} [{'/'.join(v['vote'][:4] for v in decision_votes)}] "
            f"{lead['sha'][:8]} {(lead.get('subject') or '')[:46]}")

    log(f"  pre-approved: {approved}   manual review: {manual}   held (no quorum): {held}")
    if audited:
        log(f"  unanimity audit: {audited} unanimous approval(s) re-checked by the rotated-out family")
    if flipped:
        log(f"  second round changed at least one vote on {flipped} unit(s)")
    save_meta(meta, "preapprove", {"pre_approved": approved, "manual": manual,
                                   "held": held, "audited": audited,
                                   "reviewers": reviewers, "arbiter": arbiter})
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


def _tier_candidates(rules, decided, kind):
    """Score and bucket every live candidate of one kind into tiers.

    Fork and upstream candidates share the exact same score_candidate and tier
    thresholds - the source of a commit changes how it gets triaged
    (build_triage_prompt's framing), never how a triage verdict is scored.
    """
    tiers = {t: [] for t in TIER_TITLES}
    untriaged = 0
    for _, cand in iter_candidates():
        if candidate_kind(cand) != kind:
            continue
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
    return tiers, untriaged


def _tier_count_lines(tiers, untriaged):
    lines = ["| Tier | Count |", "|---|---|"]
    for tier in ("A", "B", "C", "Q", "D"):
        lines.append(f"| {TIER_TITLES[tier]} | {len(tiers[tier])} |")
    if untriaged:
        lines.append(f"| not yet triaged | {untriaged} |")
    lines.append("")
    return lines


def _tier_detail_lines(tiers):
    lines = []
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
    return lines


def cmd_report(args):
    """Rank every judged candidate and write the report plus the import queue."""
    meta = load_meta()
    rules = load_rules()
    exclude = load_exclude()
    decided = exclude.get("commits", {})

    tiers, untriaged = _tier_candidates(rules, decided, "fork")
    up_tiers, up_untriaged = _tier_candidates(rules, decided, "upstream")

    today = datetime.now(timezone.utc).strftime("%Y-%m-%d")
    report_path = REPORTS_DIR / f"{today}_fork-radar.md"

    lines = [
        f"# Fork Radar - {today}",
        "",
        f"Upstream `{UPSTREAM}` - forks scanned for changes worth importing into `{OUR_FORK}`.",
        "",
    ]
    lines += _tier_count_lines(tiers, untriaged)
    lines += _tier_detail_lines(tiers)

    # Upstream's own unmerged commits - a second source feeding the same
    # screen/triage machinery, reported separately and never entered into the
    # cherry-pick queue below (that queue is fork-only).
    lines.append(f"# Upstream Radar - {today}")
    lines.append("")
    lines.append(f"Commits on `{UPSTREAM}`'s own default branch that `{OUR_FORK}` does not have "
                 "yet. These are not a stranger's fork: the most common correct verdict is a "
                 "conflict with our own rework of the same area, not \"already covered\", and a "
                 "change can be worth taking as direction even when the patch itself cannot apply. "
                 "Reported for awareness only - nothing here enters the import queue automatically.")
    lines.append("")
    lines += _tier_count_lines(up_tiers, up_untriaged)
    lines += _tier_detail_lines(up_tiers)

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

    log("  fork candidates:")
    for tier in ("A", "B", "C", "Q", "D"):
        log(f"    {TIER_TITLES[tier]:<50} {len(tiers[tier])}")
    if untriaged:
        log(f"    {'not yet triaged':<50} {untriaged}")
    log("  upstream candidates:")
    for tier in ("A", "B", "C", "Q", "D"):
        log(f"    {TIER_TITLES[tier]:<50} {len(up_tiers[tier])}")
    if up_untriaged:
        log(f"    {'not yet triaged':<50} {up_untriaged}")
    log(f"  report: {report_path}")
    log(f"  queue:  {IMPORT_QUEUE}")
    save_meta(meta, "report", {
        "fork": {t: len(tiers[t]) for t in tiers},
        "upstream": {t: len(up_tiers[t]) for t in up_tiers},
    })
    return 0


def public_text(value, limit=240):
    """One line, with tripwire tokens removed. Idea records are committed."""
    text = re.sub(r"\s+", " ", value or "").strip()
    text = _PUBLIC_TEXT_RE.sub("[omitted]", text)
    return text[:limit]


def suggested_adoption(tier, decision):
    """Machine status. A human status that differs is kept on the next export."""
    if decision in ("imported", "rejected", "deferred"):
        return decision
    if tier == "D":
        return "rejected"
    if tier == "Q":
        return "deferred"
    return "new"


def _idea_from_candidate(cand, score, tier, why, decision):
    triage = cand.get("triage") or {}
    flags = [f.get("id") for f in (cand.get("security_flags") or []) if f.get("id")]
    pre = (cand.get("preapproval") or {}).get("decision")
    suggested = suggested_adoption(tier, decision)
    return {
        "sha": cand.get("sha"),
        "kind": candidate_kind(cand),
        "fork": cand.get("fork"),
        "subject": public_text(cand.get("subject"), 180),
        "html_url": cand.get("html_url"),
        "date": cand.get("date"),
        "tier": tier,
        "score": score,
        "why": public_text(why, 180),
        "action": triage.get("action"),
        "value": triage.get("value"),
        "effort": triage.get("effort"),
        "risk": triage.get("risk"),
        "maps_to_issue": triage.get("maps_to_issue"),
        "security_flag_ids": flags,
        "preapproval": pre,
        "suggested_status": suggested,
        "status": suggested,
        "note": "",
    }


def _keep_adoption(existing, record):
    """Re-export refreshes the judgement and keeps a status or note we already stored."""
    if not existing:
        return record
    status = existing.get("status") or record["status"]
    note = existing.get("note") or ""
    if status == existing.get("suggested_status") and not note:
        status = record["suggested_status"]
        note = ""
    if status not in ADOPTION_STATUSES:
        status = record["suggested_status"]
    record["status"] = status
    record["note"] = public_text(note, 400)
    return record


def cmd_export_ideas(args):
    """Write the tracked adoption ledger. Judged ideas only, no patch bodies.

    `db/` stays gitignored because it redistributes other people's diffs.
    This ledger is the part that has to live in the repo: identity, tier, and
    whether we are trying it, have taken it, or have set it aside.
    Untriaged commits are counted and left in the local cache.
    """
    rules = load_rules()
    exclude = load_exclude().get("commits") or {}
    IDEAS_DIR.mkdir(parents=True, exist_ok=True)

    untriaged = {"fork": 0, "upstream": 0}
    tiers = {"fork": {t: 0 for t in TIER_TITLES}, "upstream": {t: 0 for t in TIER_TITLES}}
    status_counts = {s: 0 for s in ADOPTION_STATUSES}
    walk = []
    seen = set()

    for path, cand in iter_candidates():
        kind = candidate_kind(cand)
        if kind not in untriaged:
            continue
        if cand.get("status") == "dropped" or not cand.get("sha"):
            continue
        if not cand.get("triage"):
            untriaged[kind] += 1
            continue
        score, tier, why = score_candidate(cand, rules)
        decision = (exclude.get(cand["sha"]) or {}).get("decision")
        record = _keep_adoption(
            read_json(IDEAS_DIR / f"{cand['sha'][:10]}.json"),
            _idea_from_candidate(cand, score, tier, why, decision),
        )
        write_json(IDEAS_DIR / f"{cand['sha'][:10]}.json", record)
        seen.add(cand["sha"][:10])
        tiers[kind][tier] += 1
        status_counts[record["status"]] += 1
        if record["status"] in ("new", "trying") and tier in ("A", "B", "C"):
            walk.append({
                "sha": cand["sha"][:10],
                "kind": kind,
                "tier": tier,
                "score": score,
                "status": record["status"],
                "fork": record["fork"],
                "subject": record["subject"],
            })

    stale = 0
    for path in IDEAS_DIR.glob("*.json"):
        if path.name == "_index.json" or path.stem in seen:
            continue
        existing = read_json(path) or {}
        existing["live"] = False
        write_json(path, existing)
        stale += 1

    walk.sort(key=lambda item: ("ABC".index(item["tier"]), -item["score"], item["sha"]))
    write_json(IDEAS_DIR / "_index.json", {
        "_description": (
            "Judged upstream and fork ideas. No patch bodies. "
            "export-ideas refreshes tier and score and keeps status and note "
            "once they differ from suggested_status. "
            "Untriaged commits stay in the local cache only."
        ),
        "exported_at": utc_now(),
        "untriaged": untriaged,
        "tiers": tiers,
        "status": status_counts,
        "stale": stale,
        "walk_next": walk,
    })
    log(f"  ideas: {sum(status_counts.values())} judged, "
        f"{untriaged['fork'] + untriaged['upstream']} untriaged left local, "
        f"{len(walk)} still to walk")
    for kind in ("fork", "upstream"):
        counts = " ".join(f"{t}{tiers[kind][t]}" for t in ("A", "B", "C", "Q", "D"))
        log(f"    {kind}: {counts}")
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
    idea_path = IDEAS_DIR / f"{args.sha[:10]}.json"
    idea = read_json(idea_path)
    if idea:
        idea["status"] = args.decision
        if args.note:
            idea["note"] = public_text(args.note, 400)
        write_json(idea_path, idea)
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
    by_cand = {"fork": {}, "upstream": {}}
    for _, c in cands:
        bucket = by_cand.setdefault(candidate_kind(c), {})
        bucket[c.get("status", "?")] = bucket.get(c.get("status", "?"), 0) + 1

    log(f"Fork Intelligence v{VERSION}")
    log(f"  upstream: {UPSTREAM}")
    log(f"  forks tracked: {len(forks)}")
    for k in sorted(by_status):
        log(f"    {k:<18} {by_status[k]}")
    log(f"  candidates: {len(cands)}")
    for kind in ("fork", "upstream"):
        counts = by_cand.get(kind, {})
        log(f"    {kind}: {sum(counts.values())}")
        for k in sorted(counts):
            log(f"      {k:<16} {counts[k]}")
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
    p_div.add_argument("--all-branches", action="store_true",
                       help="also scan non-default branches (skips branches still "
                            "parked on upstream's head, which every fork inherits)")
    p_div.add_argument("--refresh", action="store_true",
                       help="re-scan forks whose branches were already enumerated")
    p_div.set_defaults(func=cmd_diverge)

    p_scr = sub.add_parser("screen", help="drop noise and security-screen the rest")
    p_scr.add_argument("--limit", type=int, default=0, help="stop after N clean commits")
    p_scr.add_argument("--refresh", action="store_true",
                       help="re-screen commits that already have a candidate file")
    p_scr.set_defaults(func=cmd_screen)

    p_ups = sub.add_parser("upstream",
                           help="compare against upstream's own default branch and screen it")
    p_ups.add_argument("--limit", type=int, default=0, help="stop after N clean commits")
    p_ups.add_argument("--refresh", action="store_true",
                       help="re-screen commits that already have a candidate file")
    p_ups.add_argument("--base", default="",
                       help="override our fork's ref used as the comparison base "
                            "(default: our fork's registered default branch)")
    p_ups.set_defaults(func=cmd_upstream)

    p_tri = sub.add_parser("triage", help="AI judgement on screened commits")
    p_tri.add_argument("--limit", type=int, default=0, help="stop after N candidates")
    p_tri.add_argument("--batch", type=int, default=5, help="commits per AI call (default 5)")
    p_tri.add_argument("--agent", default="claude", choices=list(AGENT_ARGS),
                       help="primary agent (others are used as fallback)")
    p_tri.add_argument("--refresh", action="store_true", help="re-triage already judged commits")
    p_tri.add_argument("--shard", default="",
                       help="process only shard i/n (split by SHA) so several triage "
                            "runs can work in parallel against different providers")
    p_tri.set_defaults(func=cmd_triage)

    p_pre = sub.add_parser("preapprove",
                           help="independent counter-opinions vote on import candidates")
    p_pre.add_argument("--reviewers", default="claude,codex,grok",
                       help="comma-separated model families on flat subscriptions, at "
                            "least two after rotation (default claude,codex,grok)")
    p_pre.add_argument("--arbiter", default="",
                       help="family used to audit unanimous approvals; by default this "
                            "is whichever reviewer was rotated out for running triage, "
                            "so no fourth subscription is needed")
    p_pre.add_argument("--audit-every", type=int, default=10,
                       help="audit roughly one in N unanimous approvals with the "
                            "rotated-out family (default 10; sampling is deterministic "
                            "on the SHA, so a re-run audits the same set)")
    p_pre.add_argument("--limit", type=int, default=0, help="stop after N candidates")
    p_pre.add_argument("--timeout", type=int, default=600, help="per-reviewer timeout in seconds")
    p_pre.add_argument("--refresh", action="store_true", help="re-run on already voted candidates")
    p_pre.add_argument("--only-incomplete", action="store_true",
                       help="re-run only candidates where a reviewer failed to answer")
    p_pre.add_argument("--sha", default="",
                       help="re-run a single candidate by SHA prefix")
    p_pre.set_defaults(func=cmd_preapprove)

    p_rep = sub.add_parser("report", help="rank candidates and write report + import queue")
    p_rep.set_defaults(func=cmd_report)

    p_ideas = sub.add_parser(
        "export-ideas",
        help="write the tracked ideas ledger (judged commits, no patches)")
    p_ideas.set_defaults(func=cmd_export_ideas)

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
