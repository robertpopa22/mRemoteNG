#!/usr/bin/env python3
"""
Orchestrator Supervisor — Self-Healing Wrapper for iis_orchestrator.py

Monitors the orchestrator process, detects failure modes, auto-recovers,
and restarts with exponential backoff. Designed to run continuously.

Usage:
    python orchestrator_supervisor.py                    # run supervisor loop
    python orchestrator_supervisor.py --check            # one-shot health check
    python orchestrator_supervisor.py --max-restarts 5   # limit restarts
    python orchestrator_supervisor.py --orchestrator-args "issues --max-issues 10"

Failure modes detected (from /insights analysis + code audit):
    FM1: Stale lock file (PID dead but lock exists)
    FM2: Multiple orchestrator instances running
    FM3: Phantom test processes (testhost.exe lingering)
    FM4: Rate-limit file corruption or stale entries
    FM5: Status file corruption (partial JSON write)
    FM6: Orchestrator process hung (no status update for N minutes)
    FM7: Orchestrator crashed (process dead, lock still present)
    FM8: Stale editor/tool processes (notepad.exe, mstsc.exe)
    FM9: Git state dirty (merge conflicts, uncommitted changes, detached HEAD)
    FM10: All agents rate-limited (no available agent to process issues)
    FM11: No progress (stuck on issue >30min or 0 commits after 60min)
    FM12: Build infrastructure failure (persistent build failures)
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
import signal
import subprocess
import time
from dataclasses import dataclass, field
from enum import Enum
from pathlib import Path
from typing import Optional

# ── CONFIG ──────────────────────────────────────────────────────────────────
REPO_ROOT = Path(__file__).resolve().parents[2]
GIT_DIR = REPO_ROOT / ".git"
SCRIPTS_DIR = REPO_ROOT / ".project-roadmap" / "scripts"
CHAIN_CONTEXT_DIR = SCRIPTS_DIR / "chain-context"
LOCK_FILE = SCRIPTS_DIR / "orchestrator.lock"
SUPERVISOR_LOCK_FILE = SCRIPTS_DIR / "supervisor.lock"
STATUS_FILE = SCRIPTS_DIR / "orchestrator-status.json"
LOG_FILE = SCRIPTS_DIR / "orchestrator.log"
RATE_LIMIT_FILE = SCRIPTS_DIR / "_agent_rate_limits.json"
SUPERVISOR_LOG = SCRIPTS_DIR / "supervisor.log"
ORCHESTRATOR_SCRIPT = SCRIPTS_DIR / "iis_orchestrator.py"
PROGRESS_STRIKES_FILE = SCRIPTS_DIR / "_progress_strikes.json"
BUILD_SCRIPT = REPO_ROOT / "build.ps1"

# Source directories to restore on git cleanup (exclude orchestrator metadata)
SOURCE_DIRS = ["mRemoteNG/", "mRemoteNGTests/", "mRemoteNGSpecs/"]

# Thresholds
HUNG_TIMEOUT_MINUTES = 30          # no status update = hung (Codex xhigh needs 15-20min per issue)
STALE_LOCK_HOURS = 24              # lock older than this = definitely stale
HEALTH_CHECK_INTERVAL = 30         # seconds between checks in supervisor loop
REPORT_INTERVAL_CYCLES = 10        # report every N health checks (~5 min at 30s interval)

# FM11: No progress thresholds
ISSUE_STUCK_MINUTES = 30           # single issue with no progress
SESSION_STALL_MINUTES = 60         # session running but 0 commits
PROGRESS_MAX_STRIKES = 3           # strikes before hard kill
STRIKE_DECAY_MINUTES = 60          # P4: reset strikes after this much idle time

# FM12: Build infrastructure
BUILD_FAIL_CONSECUTIVE_THRESHOLD = 3  # consecutive build failures = infra problem
BUILD_INFRA_PATTERNS = [
    "CS2012",           # DLL locked
    "MSB3027",          # file copy failed
    "NETSDK1094",       # publish failed
    "Access is denied",
    "being used by another process",
]

# Stale processes to monitor
STALE_PROCESSES = ["notepad.exe", "testhost.exe", "mstsc.exe", "dotnet.exe"]

# Processes to kill during pre-run cleanup
CLEANUP_PROCESSES = [
    "testhost.exe", "MSBuild.exe", "VBCSCompiler.exe",
    "dotnet.exe", "notepad.exe",
]

# ── ADAPTIVE BACKOFF CONFIG (per failure mode) ──────────────────────────────
# Each mode has its own initial/max backoff (seconds)
BACKOFF_CONFIG = {
    "stale_lock":              {"initial": 2,    "max": 30},
    "multiple_instances":      {"initial": 10,   "max": 60},
    "phantom_test_processes":  {"initial": 5,    "max": 30},
    "rate_limit_corruption":   {"initial": 5,    "max": 30},
    "status_corruption":       {"initial": 5,    "max": 30},
    "hung_process":            {"initial": 30,   "max": 300},
    "crashed_process":         {"initial": 10,   "max": 60},
    "stale_editor_processes":  {"initial": 5,    "max": 30},
    "git_state_dirty":         {"initial": 10,   "max": 60},
    "all_agents_blocked":      {"initial": 3600, "max": 86400},  # 1h initial, 24h max
    "no_progress":             {"initial": 60,   "max": 600},
    "build_infra_failure":     {"initial": 60,   "max": 600},
}
# Fallback for unknown modes
BACKOFF_DEFAULT = {"initial": 10, "max": 600}


# ── LOGGING ─────────────────────────────────────────────────────────────────
def setup_logging():
    fmt = "%(asctime)s [%(levelname)s] %(message)s"
    logging.basicConfig(
        level=logging.INFO,
        format=fmt,
        handlers=[
            logging.FileHandler(str(SUPERVISOR_LOG), encoding="utf-8"),
            logging.StreamHandler(sys.stdout),
        ],
    )
    return logging.getLogger("supervisor")


log = logging.getLogger("supervisor")


# ── DATA CLASSES ────────────────────────────────────────────────────────────
class FailureMode(Enum):
    FM1_STALE_LOCK = "stale_lock"
    FM2_MULTIPLE_INSTANCES = "multiple_instances"
    FM3_PHANTOM_TESTS = "phantom_test_processes"
    FM4_RATE_LIMIT_CORRUPTION = "rate_limit_corruption"
    FM5_STATUS_CORRUPTION = "status_corruption"
    FM6_HUNG_PROCESS = "hung_process"
    FM7_CRASHED_PROCESS = "crashed_process"
    FM8_STALE_PROCESSES = "stale_editor_processes"
    FM9_GIT_STATE_DIRTY = "git_state_dirty"
    FM10_ALL_AGENTS_BLOCKED = "all_agents_blocked"
    FM11_NO_PROGRESS = "no_progress"
    FM12_BUILD_INFRA_FAILURE = "build_infra_failure"


@dataclass
class HealthStatus:
    """Result of a health check."""
    healthy: bool
    failures: list = field(default_factory=list)
    details: dict = field(default_factory=dict)
    timestamp: str = ""

    def __post_init__(self):
        if not self.timestamp:
            self.timestamp = datetime.datetime.now().isoformat()

    def add_failure(self, mode: FailureMode, detail: str):
        self.failures.append({"mode": mode.value, "detail": detail})
        self.healthy = False


@dataclass
class RecoveryResult:
    """Result of a recovery action."""
    mode: FailureMode
    success: bool
    action_taken: str
    verified: bool = False
    detail: str = ""


# ── HEALTH CHECKER ──────────────────────────────────────────────────────────
class HealthChecker:
    """Detects all known failure modes by inspecting system state."""

    def __init__(self):
        self._orchestrator_running = False  # set by caller before check_all

    def check_all(self, orchestrator_running: bool = False) -> HealthStatus:
        status = HealthStatus(healthy=True)
        self._orchestrator_running = orchestrator_running
        # Run all checks — order matters (some depend on others)
        self._check_lock_file(status)
        self._check_multiple_instances(status)
        self._check_hung_process(status)
        self._check_phantom_tests(status)
        self._check_stale_processes(status)
        self._check_rate_limit_file(status)
        self._check_status_file(status)
        # New checks (FM9-FM12)
        self._check_git_state(status)
        self._check_agent_availability(status)
        self._check_progress(status)
        self._check_build_health(status)
        return status

    def _check_lock_file(self, status: HealthStatus):
        """FM1: Stale lock file — PID dead but lock exists."""
        if not LOCK_FILE.exists():
            status.details["lock"] = "no_lock"
            return

        try:
            lock_data = json.loads(LOCK_FILE.read_text(encoding="utf-8"))
            pid = lock_data.get("pid")
            started = lock_data.get("started", "")

            if pid is None:
                status.add_failure(FailureMode.FM1_STALE_LOCK,
                                   "Lock file exists but has no PID")
                return

            # Check if PID is alive
            if not _is_pid_alive(pid):
                status.add_failure(FailureMode.FM7_CRASHED_PROCESS,
                                   f"Lock file PID {pid} is dead (started: {started})")
                return

            # Check age — even if PID alive, >24h is suspicious
            if started:
                try:
                    start_dt = datetime.datetime.fromisoformat(started)
                    age_hours = (datetime.datetime.now() - start_dt).total_seconds() / 3600
                    if age_hours > STALE_LOCK_HOURS:
                        status.add_failure(FailureMode.FM1_STALE_LOCK,
                                           f"Lock file is {age_hours:.1f}h old (PID {pid})")
                except (ValueError, TypeError):
                    pass

            status.details["lock"] = {"pid": pid, "started": started, "alive": True}

        except json.JSONDecodeError:
            status.add_failure(FailureMode.FM1_STALE_LOCK,
                               "Lock file is corrupt JSON")
        except OSError as e:
            status.add_failure(FailureMode.FM1_STALE_LOCK,
                               f"Cannot read lock file: {e}")

    def _check_multiple_instances(self, status: HealthStatus):
        """FM2: Multiple orchestrator Python processes running."""
        pids = _find_orchestrator_pids()
        if len(pids) > 1:
            status.add_failure(FailureMode.FM2_MULTIPLE_INSTANCES,
                               f"Found {len(pids)} orchestrator processes: {pids}")
        status.details["orchestrator_pids"] = pids

    def _check_hung_process(self, status: HealthStatus):
        """FM6: Orchestrator alive but status not updated for too long."""
        if not STATUS_FILE.exists():
            return

        try:
            data = _safe_read_status()
            if data is None:
                return
            last_updated = data.get("last_updated")
            running = data.get("running", False)

            if not running:
                return  # Not running — not hung

            if last_updated:
                last_dt = datetime.datetime.fromisoformat(last_updated)
                age_min = (datetime.datetime.now() - last_dt).total_seconds() / 60
                if age_min > HUNG_TIMEOUT_MINUTES:
                    task = data.get("current_task", "unknown")
                    status.add_failure(FailureMode.FM6_HUNG_PROCESS,
                                       f"No status update for {age_min:.0f}min "
                                       f"(last task: {task})")
                status.details["status_age_min"] = round(age_min, 1)
        except (ValueError, TypeError, OSError):
            pass  # Handled by _check_status_file

    def _check_phantom_tests(self, status: HealthStatus):
        """FM3: testhost.exe processes lingering after test run."""
        count = _count_processes("testhost.exe")
        if count > 0:
            # P5/P10: Skip if orchestrator is actively running tests OR building
            if not self._is_active_task("test") and not self._is_active_task("build"):
                status.add_failure(FailureMode.FM3_PHANTOM_TESTS,
                                   f"{count} testhost.exe processes lingering")
        status.details["testhost_count"] = count

    def _check_stale_processes(self, status: HealthStatus):
        """FM8: Stale editor/tool processes left by agents."""
        stale = {}
        for proc in STALE_PROCESSES:
            if proc == "testhost.exe":
                continue  # Handled by FM3
            # P6: Skip dotnet.exe — it's shared with VS/SDK background processes
            # that can't (and shouldn't) be killed. Only testhost.exe is agent-specific.
            if proc == "dotnet.exe":
                continue
            count = _count_processes(proc)
            if count > 0:
                stale[proc] = count
        if stale:
            status.add_failure(FailureMode.FM8_STALE_PROCESSES,
                               f"Stale processes: {stale}")
        status.details["stale_processes"] = stale

    def _check_rate_limit_file(self, status: HealthStatus):
        """FM4: Rate-limit file corruption or expired entries not cleaned."""
        if not RATE_LIMIT_FILE.exists():
            return

        try:
            data = json.loads(RATE_LIMIT_FILE.read_text(encoding="utf-8"))
            expired = []
            corrupt = []
            now = datetime.datetime.now()

            for agent, entry in data.items():
                avail = entry.get("available_after")
                if not avail:
                    corrupt.append(agent)
                    continue
                try:
                    avail_dt = datetime.datetime.fromisoformat(avail)
                    if now >= avail_dt:
                        expired.append(agent)
                except (ValueError, TypeError):
                    corrupt.append(agent)

            if expired or corrupt:
                detail = []
                if expired:
                    detail.append(f"expired: {expired}")
                if corrupt:
                    detail.append(f"corrupt: {corrupt}")
                status.add_failure(FailureMode.FM4_RATE_LIMIT_CORRUPTION,
                                   "; ".join(detail))
            status.details["rate_limits"] = {
                "expired": expired, "corrupt": corrupt,
                "active": [a for a in data if a not in expired and a not in corrupt],
            }
        except json.JSONDecodeError:
            status.add_failure(FailureMode.FM4_RATE_LIMIT_CORRUPTION,
                               "Rate-limit file is corrupt JSON")
        except OSError as e:
            status.add_failure(FailureMode.FM4_RATE_LIMIT_CORRUPTION,
                               f"Cannot read rate-limit file: {e}")

    def _check_status_file(self, status: HealthStatus):
        """FM5: Status file corruption (partial JSON from crash)."""
        if not STATUS_FILE.exists():
            return

        try:
            data = json.loads(STATUS_FILE.read_text(encoding="utf-8"))
            # Validate required fields
            if not isinstance(data, dict):
                raise ValueError("status must be a JSON object")
            required = ["started_at", "running", "last_updated"]
            missing = [f for f in required if f not in data]
            if missing:
                status.add_failure(FailureMode.FM5_STATUS_CORRUPTION,
                                   f"Missing fields: {missing}")
            else:
                if not isinstance(data["running"], bool):
                    raise ValueError("running must be a boolean")
                for field in ("started_at", "last_updated"):
                    datetime.datetime.fromisoformat(data[field])
        except (ValueError, TypeError) as e:
            status.add_failure(FailureMode.FM5_STATUS_CORRUPTION,
                               f"Corrupt status: {e}")
        except OSError as e:
            status.add_failure(FailureMode.FM5_STATUS_CORRUPTION,
                               f"Cannot read: {e}")

    def _check_git_state(self, status: HealthStatus):
        """FM9: Git state dirty — only checked when orchestrator is NOT running.

        Detects: merge in progress, rebase in progress, detached HEAD,
        uncommitted source changes (excluding orchestrator metadata).
        """
        if self._orchestrator_running:
            return  # Avoid false positives on intentional changes

        problems = []

        # Check for merge in progress
        if (GIT_DIR / "MERGE_HEAD").exists():
            problems.append("merge in progress (MERGE_HEAD exists)")

        # Check for rebase in progress
        if (GIT_DIR / "rebase-merge").exists() or (GIT_DIR / "rebase-apply").exists():
            problems.append("rebase in progress")

        # Check for detached HEAD
        rc, head_ref = _run_git(["symbolic-ref", "--short", "HEAD"])
        if rc != 0:
            problems.append("detached HEAD")

        # Check for uncommitted source changes (exclude orchestrator metadata)
        rc, porcelain = _run_git(["status", "--porcelain", "--"] + SOURCE_DIRS)
        if rc == 0 and porcelain.strip():
            changed_count = len([l for l in porcelain.strip().splitlines() if l.strip()])
            if changed_count > 0:
                problems.append(f"{changed_count} uncommitted source file(s)")

        if problems:
            status.add_failure(FailureMode.FM9_GIT_STATE_DIRTY,
                               "; ".join(problems))
        status.details["git_state"] = problems if problems else "clean"

    def _check_agent_availability(self, status: HealthStatus):
        """FM10: All agents rate-limited — no agent available to process issues."""
        if not RATE_LIMIT_FILE.exists():
            return  # No rate limits = all available

        try:
            data = json.loads(RATE_LIMIT_FILE.read_text(encoding="utf-8"))
        except (json.JSONDecodeError, OSError):
            return  # Handled by FM4

        if not data:
            return  # Empty = all available

        now = datetime.datetime.now()
        blocked_agents = []
        earliest_available = None

        # Check all agents including model-specific keys (e.g. "gemini:gemini-3-pro-preview")
        for agent_key, entry in data.items():
            avail = entry.get("available_after")
            if not avail:
                continue
            try:
                avail_dt = datetime.datetime.fromisoformat(avail)
                if now < avail_dt:
                    blocked_agents.append(agent_key)
                    if earliest_available is None or avail_dt < earliest_available:
                        earliest_available = avail_dt
            except (ValueError, TypeError):
                continue  # Handled by FM4

        # Determine base agent names (strip model suffixes)
        base_agents = {"codex", "claude"}
        blocked_base = set()
        for key in blocked_agents:
            base = key.split(":")[0]  # "gemini:gemini-3-pro" → "gemini"
            blocked_base.add(base)

        if base_agents <= blocked_base:
            # All base agents are blocked
            wait_seconds = int((earliest_available - now).total_seconds()) if earliest_available else 3600
            status.add_failure(
                FailureMode.FM10_ALL_AGENTS_BLOCKED,
                f"All agents blocked: {sorted(blocked_agents)}. "
                f"Earliest available: {earliest_available.isoformat() if earliest_available else 'unknown'} "
                f"(~{wait_seconds}s)")
            status.details["agent_availability"] = {
                "blocked": sorted(blocked_agents),
                "earliest_available": earliest_available.isoformat() if earliest_available else None,
                "wait_seconds": wait_seconds,
            }
        else:
            available = base_agents - blocked_base
            status.details["agent_availability"] = {
                "available": sorted(available),
                "blocked": sorted(blocked_agents),
            }

    def _check_progress(self, status: HealthStatus):
        """FM11: No progress — stuck on issue or session with 0 commits."""
        data = _safe_read_status()
        if data is None or not data.get("running"):
            return

        now = datetime.datetime.now()
        problems = []

        # Check if stuck on a single issue
        task = data.get("current_task")
        if task and isinstance(task, dict):
            started_at = task.get("started_at")
            if started_at:
                try:
                    task_dt = datetime.datetime.fromisoformat(started_at)
                    task_age_min = (now - task_dt).total_seconds() / 60
                    if task_age_min > ISSUE_STUCK_MINUTES:
                        issue = task.get("issue", "?")
                        step = task.get("step", "?")
                        problems.append(
                            f"stuck on issue #{issue} ({step}) for {task_age_min:.0f}min")
                except (ValueError, TypeError):
                    pass

        # Check if session running >60min with 0 commits
        started = data.get("started_at")
        commits = data.get("commits", [])
        if started and len(commits) == 0:
            try:
                start_dt = datetime.datetime.fromisoformat(started)
                session_age_min = (now - start_dt).total_seconds() / 60
                if session_age_min > SESSION_STALL_MINUTES:
                    problems.append(
                        f"session running {session_age_min:.0f}min with 0 commits")
            except (ValueError, TypeError):
                pass

        if problems:
            status.add_failure(FailureMode.FM11_NO_PROGRESS,
                               "; ".join(problems))
        status.details["progress"] = problems if problems else "ok"

    def _check_build_health(self, status: HealthStatus):
        """FM12: Build infrastructure failure — persistent build failures in log."""
        if not LOG_FILE.exists():
            return

        try:
            # Read last 200 lines of orchestrator.log
            lines = _tail_file(LOG_FILE, 200)
        except OSError:
            return

        # Count consecutive build failures (scan from end)
        consecutive_fails = 0
        for line in reversed(lines):
            if "[BUILD] FAILED" in line:
                consecutive_fails += 1
            elif "[BUILD] OK" in line:
                break  # Found a success — stop counting

        if consecutive_fails < BUILD_FAIL_CONSECUTIVE_THRESHOLD:
            return

        # Check for infrastructure-specific patterns
        infra_hits = []
        for line in lines[-50:]:  # Check recent lines only
            for pattern in BUILD_INFRA_PATTERNS:
                if pattern in line:
                    infra_hits.append(pattern)
                    break

        detail = f"{consecutive_fails} consecutive build failures"
        if infra_hits:
            unique_hits = sorted(set(infra_hits))
            detail += f" (infra patterns: {unique_hits})"

        status.add_failure(FailureMode.FM12_BUILD_INFRA_FAILURE, detail)
        status.details["build_health"] = {
            "consecutive_fails": consecutive_fails,
            "infra_patterns": sorted(set(infra_hits)),
        }

    def _is_active_task(self, keyword: str) -> bool:
        """Check if orchestrator is currently in a task matching keyword.

        Unified check for P5/P6/P10: replaces separate _is_actively_testing()
        and _is_actively_building() with a single keyword-based check.
        """
        data = _safe_read_status()
        if data is None:
            return False
        task = data.get("current_task", "")
        phase = data.get("current_phase", "")
        return keyword in str(task).lower() or keyword in str(phase).lower()

    # Keep backward-compatible alias
    def _is_actively_testing(self) -> bool:
        return self._is_active_task("test")

    def _is_actively_building(self) -> bool:
        return self._is_active_task("build")


# ── RECOVERY ENGINE ─────────────────────────────────────────────────────────
class RecoveryEngine:
    """Executes recovery actions for each failure mode, then verifies."""

    def recover(self, failure: dict) -> RecoveryResult:
        mode = FailureMode(failure["mode"])
        detail = failure["detail"]

        handlers = {
            FailureMode.FM1_STALE_LOCK: self._recover_stale_lock,
            FailureMode.FM2_MULTIPLE_INSTANCES: self._recover_multiple_instances,
            FailureMode.FM3_PHANTOM_TESTS: self._recover_phantom_tests,
            FailureMode.FM4_RATE_LIMIT_CORRUPTION: self._recover_rate_limits,
            FailureMode.FM5_STATUS_CORRUPTION: self._recover_status_file,
            FailureMode.FM6_HUNG_PROCESS: self._recover_hung_process,
            FailureMode.FM7_CRASHED_PROCESS: self._recover_crashed_process,
            FailureMode.FM8_STALE_PROCESSES: self._recover_stale_processes,
            FailureMode.FM9_GIT_STATE_DIRTY: self._recover_git_state,
            FailureMode.FM10_ALL_AGENTS_BLOCKED: self._recover_all_blocked,
            FailureMode.FM11_NO_PROGRESS: self._recover_no_progress,
            FailureMode.FM12_BUILD_INFRA_FAILURE: self._recover_build_infra,
        }

        handler = handlers.get(mode)
        if not handler:
            return RecoveryResult(mode=mode, success=False,
                                  action_taken="no handler", detail=detail)

        result = handler(detail)
        # Verify recovery (skip for FM10/FM11 — they have special semantics)
        if mode not in (FailureMode.FM10_ALL_AGENTS_BLOCKED,
                        FailureMode.FM11_NO_PROGRESS):
            result.verified = self._verify_recovery(mode)
        else:
            result.verified = result.success
        return result

    def _recover_stale_lock(self, detail: str) -> RecoveryResult:
        """FM1: Remove stale lock file after verifying PID is dead."""
        log.info("[RECOVERY] FM1: Removing stale lock file")
        try:
            # Double-check PID before removing
            if LOCK_FILE.exists():
                try:
                    lock_data = json.loads(LOCK_FILE.read_text(encoding="utf-8"))
                    pid = lock_data.get("pid")
                    if pid and _is_pid_alive(pid):
                        # PID came back alive — don't remove
                        return RecoveryResult(
                            mode=FailureMode.FM1_STALE_LOCK, success=False,
                            action_taken="PID is alive — not removing lock",
                            detail=f"PID {pid} still running")
                except (json.JSONDecodeError, OSError):
                    pass  # Corrupt — safe to remove

                LOCK_FILE.unlink(missing_ok=True)
                return RecoveryResult(
                    mode=FailureMode.FM1_STALE_LOCK, success=True,
                    action_taken="Removed stale lock file")
        except OSError as e:
            return RecoveryResult(
                mode=FailureMode.FM1_STALE_LOCK, success=False,
                action_taken=f"Failed to remove lock: {e}")

        return RecoveryResult(
            mode=FailureMode.FM1_STALE_LOCK, success=True,
            action_taken="Lock file already gone")

    def _recover_multiple_instances(self, detail: str) -> RecoveryResult:
        """FM2: Kill all but the oldest orchestrator instance."""
        pids = _find_orchestrator_pids()
        if len(pids) <= 1:
            return RecoveryResult(
                mode=FailureMode.FM2_MULTIPLE_INSTANCES, success=True,
                action_taken="No duplicates found")

        # Keep oldest (first), kill rest
        to_kill = pids[1:]
        killed = []
        for pid in to_kill:
            if _kill_pid(pid):
                killed.append(pid)

        return RecoveryResult(
            mode=FailureMode.FM2_MULTIPLE_INSTANCES, success=len(killed) > 0,
            action_taken=f"Killed {len(killed)} duplicate PIDs: {killed}")

    def _recover_phantom_tests(self, detail: str) -> RecoveryResult:
        """FM3: Kill lingering testhost.exe processes."""
        return self._kill_process_by_name("testhost.exe", FailureMode.FM3_PHANTOM_TESTS)

    def _recover_rate_limits(self, detail: str) -> RecoveryResult:
        """FM4: Clean expired/corrupt entries from rate-limit file."""
        log.info("[RECOVERY] FM4: Cleaning rate-limit file")
        try:
            if not RATE_LIMIT_FILE.exists():
                return RecoveryResult(
                    mode=FailureMode.FM4_RATE_LIMIT_CORRUPTION, success=True,
                    action_taken="File does not exist")

            try:
                data = json.loads(RATE_LIMIT_FILE.read_text(encoding="utf-8"))
            except json.JSONDecodeError:
                # Corrupt — recreate empty
                RATE_LIMIT_FILE.write_text("{}", encoding="utf-8")
                return RecoveryResult(
                    mode=FailureMode.FM4_RATE_LIMIT_CORRUPTION, success=True,
                    action_taken="Recreated corrupt rate-limit file as empty")

            now = datetime.datetime.now()
            cleaned = {}
            removed = []

            for agent, entry in data.items():
                avail = entry.get("available_after")
                if not avail:
                    removed.append(f"{agent} (no date)")
                    continue
                try:
                    avail_dt = datetime.datetime.fromisoformat(avail)
                    if now >= avail_dt:
                        removed.append(f"{agent} (expired)")
                    else:
                        cleaned[agent] = entry  # Keep active limits
                except (ValueError, TypeError):
                    removed.append(f"{agent} (corrupt date)")

            RATE_LIMIT_FILE.write_text(
                json.dumps(cleaned, indent=2, ensure_ascii=False),
                encoding="utf-8")

            return RecoveryResult(
                mode=FailureMode.FM4_RATE_LIMIT_CORRUPTION, success=True,
                action_taken=f"Removed {len(removed)} entries: {removed}")

        except OSError as e:
            return RecoveryResult(
                mode=FailureMode.FM4_RATE_LIMIT_CORRUPTION, success=False,
                action_taken=f"Failed: {e}")

    def _recover_status_file(self, detail: str) -> RecoveryResult:
        """FM5: Backup corrupt status file and create clean one."""
        log.info("[RECOVERY] FM5: Recovering status file")
        try:
            if STATUS_FILE.exists():
                # Backup corrupt file
                backup = STATUS_FILE.with_suffix(".json.bak")
                try:
                    backup.write_bytes(STATUS_FILE.read_bytes())
                except OSError:
                    pass

                # Try to salvage what we can
                try:
                    data = json.loads(STATUS_FILE.read_text(encoding="utf-8"))
                except json.JSONDecodeError:
                    data = {}

                # Write clean status
                clean = {
                    "started_at": data.get("started_at",
                                           datetime.datetime.now().isoformat()),
                    "running": False,
                    "current_phase": None,
                    "current_task": None,
                    "last_updated": datetime.datetime.now().isoformat(),
                    "issues": data.get("issues", {}),
                    "warnings": data.get("warnings", {}),
                    "commits": data.get("commits", []),
                    "errors": data.get("errors", []),
                    "supervisor_note": "Recovered by supervisor after corruption",
                }
                STATUS_FILE.write_text(
                    json.dumps(clean, indent=2, ensure_ascii=False),
                    encoding="utf-8")

                return RecoveryResult(
                    mode=FailureMode.FM5_STATUS_CORRUPTION, success=True,
                    action_taken="Backed up corrupt file, wrote clean status")

        except OSError as e:
            return RecoveryResult(
                mode=FailureMode.FM5_STATUS_CORRUPTION, success=False,
                action_taken=f"Failed: {e}")

        return RecoveryResult(
            mode=FailureMode.FM5_STATUS_CORRUPTION, success=True,
            action_taken="Status file does not exist")

    def _recover_hung_process(self, detail: str) -> RecoveryResult:
        """FM6: Kill hung orchestrator process."""
        log.info("[RECOVERY] FM6: Killing hung orchestrator")
        pids = _find_orchestrator_pids()
        killed = []
        for pid in pids:
            if _kill_pid(pid):
                killed.append(pid)

        # Clean lock after killing
        LOCK_FILE.unlink(missing_ok=True)

        return RecoveryResult(
            mode=FailureMode.FM6_HUNG_PROCESS,
            success=len(killed) > 0 or len(pids) == 0,
            action_taken=f"Killed {len(killed)} hung processes, cleaned lock")

    def _recover_crashed_process(self, detail: str) -> RecoveryResult:
        """FM7: Clean up after crashed process (lock + stale processes)."""
        log.info("[RECOVERY] FM7: Cleaning up after crash")
        LOCK_FILE.unlink(missing_ok=True)

        # Also kill any stale test processes
        _kill_by_name("testhost.exe")
        _kill_by_name("notepad.exe")

        return RecoveryResult(
            mode=FailureMode.FM7_CRASHED_PROCESS, success=True,
            action_taken="Removed lock, killed stale processes")

    def _recover_stale_processes(self, detail: str) -> RecoveryResult:
        """FM8: Kill stale editor/tool processes.
        P5: Skip MSBuild.exe, VBCSCompiler.exe, dotnet.exe if orchestrator is active."""
        checker = HealthChecker()
        is_active = checker._is_active_task("build") or checker._is_active_task("test")

        killed = []
        for proc in STALE_PROCESSES:
            if proc == "testhost.exe":
                continue  # Handled by FM3
            # P5/P6: Don't kill build/test processes while orchestrator is active
            if is_active and proc in ("dotnet.exe", "MSBuild.exe", "VBCSCompiler.exe"):
                log.info("[RECOVERY] FM8: Skipping %s — orchestrator is active", proc)
                continue
            count = _count_processes(proc)
            if count > 0:
                if _kill_by_name(proc):
                    killed.append(proc)

        return RecoveryResult(
            mode=FailureMode.FM8_STALE_PROCESSES,
            success=len(killed) > 0,
            action_taken=f"Killed: {killed}" if killed else "No processes to kill")

    def _recover_git_state(self, detail: str) -> RecoveryResult:
        """FM9: Recover git state — stash first (P3), then abort merge/rebase,
        checkout main, restore sources."""
        log.info("[RECOVERY] FM9: Recovering git state — %s", detail)
        actions = []

        # P3: Stash uncommitted changes BEFORE any destructive operation
        stash_ref = _git_stash_if_dirty()
        if stash_ref:
            actions.append(f"stashed: {stash_ref}")

        # Abort merge if in progress
        if (GIT_DIR / "MERGE_HEAD").exists():
            rc, _ = _run_git(["merge", "--abort"])
            actions.append(f"merge --abort (rc={rc})")

        # Abort rebase if in progress
        if (GIT_DIR / "rebase-merge").exists() or (GIT_DIR / "rebase-apply").exists():
            rc, _ = _run_git(["rebase", "--abort"])
            actions.append(f"rebase --abort (rc={rc})")

        # Checkout main if detached
        rc, head = _run_git(["symbolic-ref", "--short", "HEAD"])
        if rc != 0:
            rc, _ = _run_git(["checkout", "main"])
            actions.append(f"checkout main (rc={rc})")

        # Restore source directories
        rc, _ = _run_git(["checkout", "--"] + SOURCE_DIRS)
        if rc == 0:
            actions.append("restored source dirs")
        else:
            actions.append("source dir restore failed (may be clean)")

        # Clean untracked files in source dirs
        rc, _ = _run_git(["clean", "-fd", "--"] + SOURCE_DIRS)
        if rc == 0:
            actions.append("cleaned untracked in source dirs")

        return RecoveryResult(
            mode=FailureMode.FM9_GIT_STATE_DIRTY, success=True,
            action_taken="; ".join(actions))

    def _recover_all_blocked(self, detail: str) -> RecoveryResult:
        """FM10: All agents blocked — nothing to do, supervisor will sleep."""
        log.info("[RECOVERY] FM10: All agents blocked — supervisor will enter dormancy")
        # No active recovery needed — the supervisor run() loop handles dormancy
        return RecoveryResult(
            mode=FailureMode.FM10_ALL_AGENTS_BLOCKED, success=True,
            action_taken="Acknowledged — supervisor will sleep until agents available")

    def _recover_no_progress(self, detail: str) -> RecoveryResult:
        """FM11: No progress — progressive strikes: warn → kill → hard kill.
        P4: Time-based decay — strikes reset if last_strike > STRIKE_DECAY_MINUTES."""
        log.info("[RECOVERY] FM11: No progress detected — %s", detail)

        # Load/update strikes
        strikes = _load_progress_strikes()

        # P4: Time-based decay — if last strike was >60min ago, reset
        last_strike_str = strikes.get("last_strike")
        if last_strike_str:
            try:
                last_dt = datetime.datetime.fromisoformat(last_strike_str)
                minutes_since = (datetime.datetime.now() - last_dt).total_seconds() / 60
                if minutes_since > STRIKE_DECAY_MINUTES:
                    log.info("[RECOVERY] FM11: Last strike was %.0fmin ago (>%dmin) — resetting",
                             minutes_since, STRIKE_DECAY_MINUTES)
                    strikes = {"count": 0, "last_strike": None}
            except (ValueError, TypeError):
                pass

        strikes["count"] = strikes.get("count", 0) + 1
        strikes["last_strike"] = datetime.datetime.now().isoformat()
        _save_progress_strikes(strikes)

        strike_count = strikes["count"]
        log.info("[RECOVERY] FM11: Strike %d/%d", strike_count, PROGRESS_MAX_STRIKES)

        if strike_count < 2:
            # Strike 1: Warning only, continue monitoring
            return RecoveryResult(
                mode=FailureMode.FM11_NO_PROGRESS, success=True,
                action_taken=f"Warning (strike {strike_count}/{PROGRESS_MAX_STRIKES})")

        if strike_count < PROGRESS_MAX_STRIKES:
            # Strike 2: Kill orchestrator, let supervisor restart
            pids = _find_orchestrator_pids()
            killed = []
            for pid in pids:
                if _kill_pid(pid):
                    killed.append(pid)
            LOCK_FILE.unlink(missing_ok=True)
            return RecoveryResult(
                mode=FailureMode.FM11_NO_PROGRESS, success=True,
                action_taken=f"Strike {strike_count}: killed orchestrator PIDs {killed}")

        # Strike 3+: Hard kill + full cleanup
        pids = _find_orchestrator_pids()
        for pid in pids:
            _kill_pid(pid)
        LOCK_FILE.unlink(missing_ok=True)
        for proc in CLEANUP_PROCESSES:
            _kill_by_name(proc)
        # Reset strikes after hard kill
        strikes["count"] = 0
        _save_progress_strikes(strikes)
        return RecoveryResult(
            mode=FailureMode.FM11_NO_PROGRESS, success=True,
            action_taken=f"Strike {strike_count}: hard kill + full cleanup, strikes reset")

    def _recover_build_infra(self, detail: str) -> RecoveryResult:
        """FM12: Build infrastructure failure — kill build processes, test build.
        P5: Check if orchestrator is actively building before killing."""
        log.info("[RECOVERY] FM12: Build infrastructure failure — %s", detail)
        actions = []

        # P5: Don't kill build processes if orchestrator is actively building
        checker = HealthChecker()
        if checker._is_active_task("build") or checker._is_active_task("test"):
            log.info("[RECOVERY] FM12: Orchestrator is active — deferring recovery")
            return RecoveryResult(
                mode=FailureMode.FM12_BUILD_INFRA_FAILURE, success=True,
                action_taken="Deferred — orchestrator is actively building/testing")

        # Kill build-related processes
        for proc in ["MSBuild.exe", "VBCSCompiler.exe", "dotnet.exe"]:
            if _count_processes(proc) > 0:
                _kill_by_name(proc)
                actions.append(f"killed {proc}")

        # Wait for file handles to release
        time.sleep(5)

        # Test build
        log.info("[RECOVERY] FM12: Running test build...")
        try:
            result = subprocess.run(
                ["pwsh", "-NoProfile", "-ExecutionPolicy", "Bypass",
                 "-File", str(BUILD_SCRIPT), "-NoRestore"],
                capture_output=True, text=True, timeout=300,
                cwd=str(REPO_ROOT),
            )
            if result.returncode == 0:
                actions.append("test build PASSED — safe to restart")
                return RecoveryResult(
                    mode=FailureMode.FM12_BUILD_INFRA_FAILURE, success=True,
                    action_taken="; ".join(actions))
            else:
                # Extract last few error lines
                stderr_tail = result.stderr[-500:] if result.stderr else ""
                stdout_tail = result.stdout[-500:] if result.stdout else ""
                actions.append(f"test build FAILED (rc={result.returncode})")
                log.error("[RECOVERY] FM12: Test build failed:\n%s\n%s",
                          stdout_tail, stderr_tail)
                return RecoveryResult(
                    mode=FailureMode.FM12_BUILD_INFRA_FAILURE, success=False,
                    action_taken="; ".join(actions),
                    detail="Build still broken — do NOT restart orchestrator")
        except subprocess.TimeoutExpired:
            actions.append("test build TIMED OUT (300s)")
            return RecoveryResult(
                mode=FailureMode.FM12_BUILD_INFRA_FAILURE, success=False,
                action_taken="; ".join(actions))
        except OSError as e:
            actions.append(f"test build error: {e}")
            return RecoveryResult(
                mode=FailureMode.FM12_BUILD_INFRA_FAILURE, success=False,
                action_taken="; ".join(actions))

    def _kill_process_by_name(self, name: str, mode: FailureMode) -> RecoveryResult:
        """Helper: kill all processes with given name."""
        log.info("[RECOVERY] Killing all %s processes", name)
        success = _kill_by_name(name)
        return RecoveryResult(
            mode=mode, success=success,
            action_taken=f"taskkill /F /IM {name}")

    def _verify_recovery(self, mode: FailureMode) -> bool:
        """Run targeted re-check to confirm recovery worked."""
        checker = HealthChecker()
        status = HealthStatus(healthy=True)

        verify_map = {
            FailureMode.FM1_STALE_LOCK: checker._check_lock_file,
            FailureMode.FM2_MULTIPLE_INSTANCES: checker._check_multiple_instances,
            FailureMode.FM3_PHANTOM_TESTS: checker._check_phantom_tests,
            FailureMode.FM4_RATE_LIMIT_CORRUPTION: checker._check_rate_limit_file,
            FailureMode.FM5_STATUS_CORRUPTION: checker._check_status_file,
            FailureMode.FM6_HUNG_PROCESS: checker._check_hung_process,
            FailureMode.FM7_CRASHED_PROCESS: checker._check_lock_file,
            FailureMode.FM8_STALE_PROCESSES: checker._check_stale_processes,
            FailureMode.FM9_GIT_STATE_DIRTY: checker._check_git_state,
            FailureMode.FM12_BUILD_INFRA_FAILURE: checker._check_build_health,
        }

        check_fn = verify_map.get(mode)
        if check_fn:
            check_fn(status)
            # Check if this specific failure mode reappeared
            for f in status.failures:
                if f["mode"] == mode.value:
                    return False
        return True


# ── SUPERVISOR ──────────────────────────────────────────────────────────────
class Supervisor:
    """Continuously monitors orchestrator, recovers from failures, restarts."""

    def __init__(self, orchestrator_args: str = "", max_restarts: int = 0):
        self.orchestrator_args = orchestrator_args
        self.max_restarts = max_restarts  # 0 = unlimited
        self.restart_count = 0
        self.checker = HealthChecker()
        self.recovery = RecoveryEngine()
        self._running = True
        self._orchestrator_proc: Optional[subprocess.Popen] = None
        self._cycle_count = 0
        self._last_report_triaged = 0
        self._last_report_implemented = 0

        # Adaptive backoff: per-mode current values
        self._mode_backoff: dict[str, float] = {}

        signal.signal(signal.SIGINT, self._handle_signal)
        signal.signal(signal.SIGTERM, self._handle_signal)

    def _handle_signal(self, signum, frame):
        log.info("[SUPERVISOR] Received signal %s — shutting down", signum)
        self._running = False
        # P1: Release supervisor lock on signal
        _release_supervisor_lock()
        if self._orchestrator_proc and self._orchestrator_proc.poll() is None:
            log.info("[SUPERVISOR] Stopping orchestrator (PID %d)",
                     self._orchestrator_proc.pid)
            self._orchestrator_proc.terminate()
            try:
                self._orchestrator_proc.wait(timeout=30)
            except subprocess.TimeoutExpired:
                self._orchestrator_proc.kill()

    def _get_backoff(self, mode_value: str) -> float:
        """Get current backoff for a failure mode."""
        if mode_value not in self._mode_backoff:
            cfg = BACKOFF_CONFIG.get(mode_value, BACKOFF_DEFAULT)
            self._mode_backoff[mode_value] = cfg["initial"]
        return self._mode_backoff[mode_value]

    def _increase_backoff(self, mode_value: str):
        """Double backoff for a failure mode, capped at max."""
        cfg = BACKOFF_CONFIG.get(mode_value, BACKOFF_DEFAULT)
        current = self._mode_backoff.get(mode_value, cfg["initial"])
        self._mode_backoff[mode_value] = min(current * 2, cfg["max"])

    def _reset_backoff(self, mode_value: str):
        """Reset backoff to initial for a failure mode."""
        cfg = BACKOFF_CONFIG.get(mode_value, BACKOFF_DEFAULT)
        self._mode_backoff[mode_value] = cfg["initial"]

    def _pre_run_cleanup(self):
        """Pre-run cleanup before starting orchestrator.

        Kills stale processes, aborts stuck git operations, restores source
        dirs, removes stale locks, and cleans old chain-context files.
        P3: Stashes uncommitted changes before destructive git operations.
        P4: Does NOT reset progress strikes (they persist across restarts).
        """
        log.info("[CLEANUP] Running pre-start cleanup...")

        # 1. Kill stale processes
        for proc in CLEANUP_PROCESSES:
            count = _count_processes(proc)
            if count > 0:
                _kill_by_name(proc)
                log.info("[CLEANUP] Killed %d × %s", count, proc)

        # 2. Abort git merge/rebase if stuck
        if (GIT_DIR / "MERGE_HEAD").exists():
            rc, _ = _run_git(["merge", "--abort"])
            log.info("[CLEANUP] Aborted merge (rc=%d)", rc)
        if (GIT_DIR / "rebase-merge").exists() or (GIT_DIR / "rebase-apply").exists():
            rc, _ = _run_git(["rebase", "--abort"])
            log.info("[CLEANUP] Aborted rebase (rc=%d)", rc)

        # 3. P3: Stash uncommitted source changes BEFORE restoring
        rc, porcelain = _run_git(["status", "--porcelain", "--"] + SOURCE_DIRS)
        if rc == 0 and porcelain.strip():
            stash_ref = _git_stash_if_dirty()
            if stash_ref:
                log.info("[CLEANUP] Stashed uncommitted changes: %s", stash_ref)
            else:
                # Stash failed — still restore to ensure clean state
                _run_git(["checkout", "--"] + SOURCE_DIRS)
                _run_git(["clean", "-fd", "--"] + SOURCE_DIRS)
                log.info("[CLEANUP] Restored source dirs to HEAD (stash failed)")
        else:
            log.info("[CLEANUP] Source dirs clean — no stash needed")

        # 4. Remove stale lock if PID dead
        if LOCK_FILE.exists():
            try:
                lock_data = json.loads(LOCK_FILE.read_text(encoding="utf-8"))
                pid = lock_data.get("pid")
                if pid and not _is_pid_alive(pid):
                    LOCK_FILE.unlink(missing_ok=True)
                    log.info("[CLEANUP] Removed stale lock (PID %d dead)", pid)
            except (json.JSONDecodeError, OSError):
                LOCK_FILE.unlink(missing_ok=True)
                log.info("[CLEANUP] Removed corrupt lock file")

        # 5. Clean chain-context files older than 7 days
        if CHAIN_CONTEXT_DIR.exists():
            cutoff = datetime.datetime.now().timestamp() - (7 * 86400)
            cleaned = 0
            for f in CHAIN_CONTEXT_DIR.iterdir():
                if f.is_file() and f.suffix == ".json" and not f.name.startswith("_"):
                    try:
                        if f.stat().st_mtime < cutoff:
                            f.unlink()
                            cleaned += 1
                    except OSError:
                        pass
            if cleaned:
                log.info("[CLEANUP] Removed %d old chain-context files", cleaned)

        # 6. P3: Clean stale git stashes older than 7 days
        _cleanup_old_stashes(max_age_days=7)

        # P4: Do NOT reset progress strikes — they persist across restarts
        # (was: _save_progress_strikes({"count": 0, "last_strike": None}))

        log.info("[CLEANUP] Pre-start cleanup complete")

    def run(self):
        """Main supervisor loop."""
        # P1: Acquire supervisor lock
        if not _acquire_supervisor_lock():
            log.error("[SUPERVISOR] Another supervisor is running — exiting")
            sys.exit(1)

        log.info("=" * 60)
        log.info("[SUPERVISOR] Starting — monitoring orchestrator")
        log.info("[SUPERVISOR] Args: %s", self.orchestrator_args or "(default)")
        log.info("[SUPERVISOR] Max restarts: %s",
                 self.max_restarts or "unlimited")
        log.info("=" * 60)

        try:
            while self._running:
                orch_running = self._is_orchestrator_running()

                # Phase 1: Health check + recovery
                health = self.checker.check_all(orchestrator_running=orch_running)

                if not health.healthy:
                    log.warning("[SUPERVISOR] Health check FAILED — %d issues",
                                len(health.failures))
                    for f in health.failures:
                        log.warning("  - %s: %s", f["mode"], f["detail"])

                    # Check for FM10 (all agents blocked) — special dormancy handling
                    fm10_failure = None
                    for f in health.failures:
                        if f["mode"] == FailureMode.FM10_ALL_AGENTS_BLOCKED.value:
                            fm10_failure = f
                            break

                    if fm10_failure:
                        # Enter dormancy — sleep until earliest agent available
                        wait_secs = health.details.get("agent_availability", {}).get(
                            "wait_seconds", 3600)
                        # Cap at adaptive backoff max
                        cfg = BACKOFF_CONFIG["all_agents_blocked"]
                        wait_secs = min(wait_secs, cfg["max"])
                        log.info("[SUPERVISOR] FM10: All agents blocked — "
                                 "dormancy for %ds (~%.1fh)",
                                 wait_secs, wait_secs / 3600)
                        # Sleep in 1s chunks for SIGINT responsiveness
                        for _ in range(wait_secs):
                            if not self._running:
                                break
                            time.sleep(1)
                        continue

                    # Recover each failure
                    all_recovered = True
                    worst_backoff = 0
                    for failure in health.failures:
                        result = self.recovery.recover(failure)
                        mode_val = failure["mode"]
                        if result.success and result.verified:
                            log.info("[RECOVERY] %s: OK — %s (verified)",
                                     result.mode.value, result.action_taken)
                            self._reset_backoff(mode_val)
                        elif result.success:
                            log.warning("[RECOVERY] %s: action taken but not verified — %s",
                                        result.mode.value, result.action_taken)
                        else:
                            log.error("[RECOVERY] %s: FAILED — %s",
                                      result.mode.value, result.action_taken)
                            all_recovered = False
                            backoff = self._get_backoff(mode_val)
                            worst_backoff = max(worst_backoff, backoff)
                            self._increase_backoff(mode_val)

                    if not all_recovered:
                        log.error("[SUPERVISOR] Some recoveries failed — "
                                  "waiting %ds before retry", worst_backoff)
                        _interruptible_sleep(worst_backoff, lambda: self._running)
                        continue

                # Phase 2: Check if orchestrator is running
                if not self._is_orchestrator_running():
                    if self.max_restarts > 0 and self.restart_count >= self.max_restarts:
                        log.info("[SUPERVISOR] Max restarts (%d) reached — exiting",
                                 self.max_restarts)
                        break

                    log.info("[SUPERVISOR] Orchestrator not running — "
                             "starting (attempt %d)",
                             self.restart_count + 1)

                    # Pre-run cleanup before starting
                    self._pre_run_cleanup()

                    self._start_orchestrator()
                    self.restart_count += 1
                    # Give it time to initialize
                    time.sleep(5)
                    if not self._is_orchestrator_running():
                        log.error("[SUPERVISOR] Orchestrator failed to start")

                # Phase 3: Periodic progress report
                self._cycle_count += 1
                if self._cycle_count % REPORT_INTERVAL_CYCLES == 0:
                    self._periodic_report()

                # Phase 4: Wait before next check
                for _ in range(HEALTH_CHECK_INTERVAL):
                    if not self._running:
                        break
                    time.sleep(1)

        finally:
            # P1: Always release supervisor lock
            _release_supervisor_lock()

        log.info("[SUPERVISOR] Shutting down — %d restarts total",
                 self.restart_count)

    def _periodic_report(self):
        """Log a concise progress summary from orchestrator-status.json."""
        data = _safe_read_status()
        if data is None or not data.get("running"):
            return
        try:
            issues = data.get("issues", {})
            tokens = data.get("token_usage", {})
            task = data.get("current_task", {})
            commits = data.get("commits", [])
            errors = data.get("errors", [])
            started = data.get("started_at", "")

            triaged = issues.get("triaged", 0)
            implemented = issues.get("implemented", 0)
            failed = issues.get("failed", 0)
            total = issues.get("total_synced", 0)
            wontfix = issues.get("skipped_wontfix", 0)
            needs_info = issues.get("skipped_needs_info", 0)
            duplicate = issues.get("skipped_duplicate", 0)
            processed = triaged + implemented + failed + wontfix + needs_info + duplicate

            # Delta since last report
            d_triaged = triaged - self._last_report_triaged
            d_impl = implemented - self._last_report_implemented
            self._last_report_triaged = triaged
            self._last_report_implemented = implemented

            # Duration
            duration = ""
            if started:
                try:
                    dt = datetime.datetime.fromisoformat(started)
                    elapsed = datetime.datetime.now() - dt
                    h, rem = divmod(int(elapsed.total_seconds()), 3600)
                    m, s = divmod(rem, 60)
                    duration = f"{h:02d}:{m:02d}:{s:02d}"
                except (ValueError, TypeError):
                    pass

            # Current task
            task_desc = ""
            if task:
                t_type = task.get("type", "?")
                t_issue = task.get("issue", "")
                t_step = task.get("step", "")
                task_desc = f"{t_type} #{t_issue} ({t_step})" if t_issue else t_type

            cost = tokens.get("cost_usd", 0)
            cost_per = cost / processed if processed > 0 else 0

            log.info("=" * 60)
            log.info("[REPORT] Progress: %d/%d (%.1f%%) | +%d triaged, +%d impl since last",
                     processed, total, (processed / total * 100) if total else 0,
                     d_triaged, d_impl)
            log.info("[REPORT] Triaged: %d | Implemented: %d | Failed: %d | "
                     "Wontfix: %d | NeedsInfo: %d",
                     triaged, implemented, failed, wontfix, needs_info)
            log.info("[REPORT] Commits: %d | Errors: %d | Cost: $%.2f ($%.2f/issue)",
                     len(commits), len(errors), cost, cost_per)
            if task_desc:
                log.info("[REPORT] Current: %s", task_desc)
            if duration:
                log.info("[REPORT] Duration: %s", duration)
            log.info("=" * 60)

        except (json.JSONDecodeError, OSError, KeyError) as e:
            log.debug("[REPORT] Could not read status: %s", e)

    def _is_orchestrator_running(self) -> bool:
        """Check if we have a live orchestrator process."""
        if self._orchestrator_proc and self._orchestrator_proc.poll() is None:
            return True
        # Also check for externally-started orchestrators
        return len(_find_orchestrator_pids()) > 0

    def _start_orchestrator(self):
        """Start the orchestrator as a subprocess."""
        cmd = [sys.executable, str(ORCHESTRATOR_SCRIPT), "--force"]
        if self.orchestrator_args:
            cmd.extend(self.orchestrator_args.split())

        log.info("[SUPERVISOR] Starting: %s", " ".join(cmd))

        try:
            self._orchestrator_proc = subprocess.Popen(
                cmd,
                stdout=subprocess.PIPE,
                stderr=subprocess.STDOUT,
                cwd=str(SCRIPTS_DIR),
                creationflags=(subprocess.CREATE_NEW_PROCESS_GROUP
                               if sys.platform == "win32" else 0),
            )
            log.info("[SUPERVISOR] Orchestrator started — PID %d",
                     self._orchestrator_proc.pid)

            # Start log reader thread
            import threading
            t = threading.Thread(target=self._read_orchestrator_output, daemon=True)
            t.start()

        except Exception as e:
            log.error("[SUPERVISOR] Failed to start orchestrator: %s", e)
            self._orchestrator_proc = None

    def _read_orchestrator_output(self):
        """Read orchestrator stdout in background thread."""
        if not self._orchestrator_proc or not self._orchestrator_proc.stdout:
            return
        try:
            for line in self._orchestrator_proc.stdout:
                if isinstance(line, bytes):
                    line = line.decode("utf-8", errors="replace")
                line = line.rstrip()
                if line:
                    log.info("[ORCH] %s", line)
        except Exception:
            pass



    @staticmethod
    def one_shot_check() -> HealthStatus:
        return HealthChecker().check_all(orchestrator_running=bool(_find_orchestrator_pids()))


# ── UTILITIES ───────────────────────────────────────────────────────────────
def _is_pid_alive(pid: int) -> bool:
    """Check if a process with given PID exists."""
    if type(pid) is not int or not 0 < pid <= 0xFFFFFFFF:
        return False
    if sys.platform == "win32":
        # On Windows os.kill(pid, 0) is not a read-only existence probe.
        import ctypes
        from ctypes import wintypes

        kernel = ctypes.WinDLL("kernel32", use_last_error=True)
        kernel.OpenProcess.argtypes = (wintypes.DWORD, wintypes.BOOL, wintypes.DWORD)
        kernel.OpenProcess.restype = wintypes.HANDLE
        kernel.WaitForSingleObject.argtypes = (wintypes.HANDLE, wintypes.DWORD)
        kernel.WaitForSingleObject.restype = wintypes.DWORD
        kernel.CloseHandle.argtypes = (wintypes.HANDLE,)
        handle = kernel.OpenProcess(0x00100000, False, pid)  # SYNCHRONIZE
        if not handle:
            return ctypes.get_last_error() == 5  # Access denied is not proof of exit.
        try:
            return kernel.WaitForSingleObject(handle, 0) != 0
        finally:
            kernel.CloseHandle(handle)
    try:
        os.kill(pid, 0)
        return True
    except (OSError, ProcessLookupError):
        return False


def _find_orchestrator_pids() -> list:
    """Find all Python processes running iis_orchestrator.py."""
    try:
        query = (
            "Get-CimInstance Win32_Process -Filter \"Name = 'python.exe' OR Name = 'python3.exe' "
            "OR Name = 'pythonw.exe'\" | "
            "Where-Object { $_.CommandLine -match 'iis_orchestrator\\.py' } | "
            "Select-Object -ExpandProperty ProcessId"
        )
        result = subprocess.run(["powershell.exe", "-NoProfile", "-NonInteractive", "-Command", query],
                                capture_output=True, text=True, timeout=10, check=True)
        return sorted(int(line) for line in result.stdout.splitlines() if line.strip().isdigit())
    except (OSError, subprocess.SubprocessError):
        log.warning("Cannot read the IIS process inventory")
        return []


def _count_processes(name: str) -> int:
    """Count running processes by name."""
    try:
        result = subprocess.run(
            ["tasklist", "/FI", f"IMAGENAME eq {name}", "/NH"],
            capture_output=True, text=True, timeout=10,
        )
        count = 0
        for line in result.stdout.splitlines():
            if name.lower() in line.lower():
                count += 1
        return count
    except Exception:
        return 0


def _kill_pid(pid: int) -> bool:
    """Kill a process by PID."""
    try:
        subprocess.run(
            ["taskkill", "/F", "/T", "/PID", str(pid)],
            capture_output=True, timeout=15,
        )
        log.info("[KILL] Killed PID %d", pid)
        return True
    except Exception as e:
        log.warning("[KILL] Failed to kill PID %d: %s", pid, e)
        return False


def _kill_by_name(name: str) -> bool:
    """Kill all processes with given image name."""
    try:
        subprocess.run(
            ["taskkill", "/F", "/IM", name],
            capture_output=True, timeout=10,
        )
        return True
    except Exception:
        return False


def _run_git(args: list) -> tuple:
    """Run a git command in the repo root. Returns (returncode, stdout)."""
    try:
        result = subprocess.run(
            ["git", "--no-optional-locks"] + args,
            capture_output=True, text=True, timeout=30,
            cwd=str(REPO_ROOT),
        )
        return result.returncode, result.stdout.strip()
    except Exception as e:
        return -1, str(e)


def _safe_read_status() -> Optional[dict]:
    """Read status file with retries (handles atomic write races)."""
    for attempt in range(3):
        try:
            if not STATUS_FILE.exists():
                return None
            data = json.loads(STATUS_FILE.read_text(encoding="utf-8"))
            return data if isinstance(data, dict) else None
        except (json.JSONDecodeError, OSError):
            if attempt < 2:
                time.sleep(0.2)
    return None


def _tail_file(path: Path, n: int) -> list:
    """Read the last N lines of a file."""
    try:
        text = path.read_text(encoding="utf-8", errors="replace")
        lines = text.splitlines()
        return lines[-n:]
    except OSError:
        return []


def _load_progress_strikes() -> dict:
    """Load progress strike state from persistent file."""
    try:
        if PROGRESS_STRIKES_FILE.exists():
            return json.loads(PROGRESS_STRIKES_FILE.read_text(encoding="utf-8"))
    except (json.JSONDecodeError, OSError):
        pass
    return {"count": 0, "last_strike": None}


def _save_progress_strikes(data: dict):
    """Save progress strike state to persistent file."""
    try:
        PROGRESS_STRIKES_FILE.write_text(
            json.dumps(data, indent=2, ensure_ascii=False),
            encoding="utf-8")
    except OSError as e:
        log.warning("[PROGRESS] Could not save strikes: %s", e)


def _interruptible_sleep(seconds: float, check_running):
    """Sleep for `seconds` in 1s chunks, breaking early if check_running() is False."""
    for _ in range(int(seconds)):
        if not check_running():
            break
        time.sleep(1)


# ── P1: SUPERVISOR LOCK ────────────────────────────────────────────────────
def _acquire_supervisor_lock() -> bool:
    """Acquire supervisor lock file to prevent concurrent supervisors.

    Returns True if lock acquired, False if another supervisor is running.
    Removes stale locks (dead PID) automatically.
    """
    if SUPERVISOR_LOCK_FILE.exists():
        try:
            lock_data = json.loads(SUPERVISOR_LOCK_FILE.read_text(encoding="utf-8"))
            pid = lock_data.get("pid")
            if pid and _is_pid_alive(pid):
                log.error("[LOCK] Supervisor already running (PID %d, started %s)",
                          pid, lock_data.get("started", "unknown"))
                return False
            else:
                log.warning("[LOCK] Stale supervisor lock (PID %d dead) — removing", pid)
                SUPERVISOR_LOCK_FILE.unlink(missing_ok=True)
        except (json.JSONDecodeError, OSError):
            log.warning("[LOCK] Corrupt supervisor lock — removing")
            SUPERVISOR_LOCK_FILE.unlink(missing_ok=True)

    try:
        SUPERVISOR_LOCK_FILE.write_text(
            json.dumps({
                "pid": os.getpid(),
                "started": datetime.datetime.now().isoformat(),
            }, indent=2),
            encoding="utf-8",
        )
        log.info("[LOCK] Supervisor lock acquired (PID %d)", os.getpid())
        return True
    except OSError as e:
        log.error("[LOCK] Failed to create supervisor lock: %s", e)
        return False


def _release_supervisor_lock():
    """Release supervisor lock file."""
    try:
        SUPERVISOR_LOCK_FILE.unlink(missing_ok=True)
    except OSError:
        pass


# ── P3: GIT STASH SAFETY NET ───────────────────────────────────────────────
def _git_stash_if_dirty() -> Optional[str]:
    """Stash uncommitted source changes before destructive git operations.

    Returns the stash message (ref) if stash was created, or None.
    Only stashes changes in SOURCE_DIRS to avoid touching orchestrator metadata.
    """
    rc, porcelain = _run_git(["status", "--porcelain", "--"] + SOURCE_DIRS)
    if rc != 0 or not porcelain.strip():
        return None  # Clean or error

    timestamp = datetime.datetime.now().strftime("%Y%m%d-%H%M%S")
    msg = f"supervisor-recovery-{timestamp}"

    rc, output = _run_git(["stash", "push", "-m", msg, "--"] + SOURCE_DIRS)
    if rc == 0:
        log.info("[STASH] Saved uncommitted changes: %s", msg)
        return msg
    else:
        log.warning("[STASH] Failed to stash: %s", output)
        return None


def _cleanup_old_stashes(max_age_days: int = 7):
    """Remove supervisor-created stashes older than max_age_days."""
    rc, stash_list = _run_git(["stash", "list"])
    if rc != 0 or not stash_list.strip():
        return

    now = datetime.datetime.now()
    # Parse stash entries and drop old supervisor-created ones (from bottom up)
    indices_to_drop = []
    for line in stash_list.splitlines():
        if "supervisor-recovery-" not in line:
            continue
        # Extract timestamp from stash message
        try:
            # Format: stash@{N}: ... supervisor-recovery-YYYYMMDD-HHMMSS
            import re
            idx_match = re.search(r"stash@\{(\d+)\}", line)
            ts_match = re.search(r"supervisor-recovery-(\d{8}-\d{6})", line)
            if idx_match and ts_match:
                idx = int(idx_match.group(1))
                ts = datetime.datetime.strptime(ts_match.group(1), "%Y%m%d-%H%M%S")
                age_days = (now - ts).total_seconds() / 86400
                if age_days > max_age_days:
                    indices_to_drop.append(idx)
        except (ValueError, TypeError):
            continue

    # Drop from highest index to lowest to avoid index shifting
    for idx in sorted(indices_to_drop, reverse=True):
        rc, _ = _run_git(["stash", "drop", f"stash@{{{idx}}}"])
        if rc == 0:
            log.info("[STASH] Dropped old stash@{%d}", idx)


# ── MAIN ────────────────────────────────────────────────────────────────────
def main():
    parser = argparse.ArgumentParser(
        description="Health inspection for iis_orchestrator.py")
    parser.add_argument("--check", action="store_true",
                        help="One-shot read-only health check")
    parser.add_argument("--max-restarts", type=int, default=0,
                        help="Max restarts before exit (0 = unlimited)")
    parser.add_argument("--orchestrator-args", type=str, default="",
                        help="Arguments to pass to orchestrator")
    args = parser.parse_args()

    if not args.check:
        parser.error("legacy automatic recovery is disabled; use --check for inspection")
    health = Supervisor.one_shot_check()
    if health.healthy:
        print("IIS health check: no findings")
    else:
        for failure in health.failures:
            print(f"{failure['mode']}: {failure['detail']}")
    return int(not health.healthy)


if __name__ == "__main__":
    sys.exit(main())
