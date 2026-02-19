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
SCRIPTS_DIR = Path(r"D:\github\mRemoteNG\.project-roadmap\scripts")
LOCK_FILE = SCRIPTS_DIR / "orchestrator.lock"
STATUS_FILE = SCRIPTS_DIR / "orchestrator-status.json"
LOG_FILE = SCRIPTS_DIR / "orchestrator.log"
RATE_LIMIT_FILE = SCRIPTS_DIR / "_agent_rate_limits.json"
SUPERVISOR_LOG = SCRIPTS_DIR / "supervisor.log"
ORCHESTRATOR_SCRIPT = SCRIPTS_DIR / "iis_orchestrator.py"

# Thresholds
HUNG_TIMEOUT_MINUTES = 15          # no status update = hung
STALE_LOCK_HOURS = 24              # lock older than this = definitely stale
MAX_RESTART_BACKOFF_SECONDS = 600  # cap backoff at 10 min
INITIAL_BACKOFF_SECONDS = 10       # first restart delay
HEALTH_CHECK_INTERVAL = 30        # seconds between checks in supervisor loop

# Stale processes to monitor
STALE_PROCESSES = ["notepad.exe", "testhost.exe", "mstsc.exe", "dotnet.exe"]


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


log = setup_logging()


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

    def check_all(self) -> HealthStatus:
        status = HealthStatus(healthy=True)
        # Run all checks — order matters (some depend on others)
        self._check_lock_file(status)
        self._check_multiple_instances(status)
        self._check_hung_process(status)
        self._check_phantom_tests(status)
        self._check_stale_processes(status)
        self._check_rate_limit_file(status)
        self._check_status_file(status)
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
            data = json.loads(STATUS_FILE.read_text(encoding="utf-8"))
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
        except (json.JSONDecodeError, OSError):
            pass  # Handled by _check_status_file

    def _check_phantom_tests(self, status: HealthStatus):
        """FM3: testhost.exe processes lingering after test run."""
        count = _count_processes("testhost.exe")
        if count > 0:
            # Check if orchestrator is actively running tests
            if not self._is_actively_testing():
                status.add_failure(FailureMode.FM3_PHANTOM_TESTS,
                                   f"{count} testhost.exe processes lingering")
        status.details["testhost_count"] = count

    def _check_stale_processes(self, status: HealthStatus):
        """FM8: Stale editor/tool processes left by agents."""
        stale = {}
        for proc in STALE_PROCESSES:
            if proc == "testhost.exe":
                continue  # Handled by FM3
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
            required = ["started_at", "running", "last_updated"]
            missing = [f for f in required if f not in data]
            if missing:
                status.add_failure(FailureMode.FM5_STATUS_CORRUPTION,
                                   f"Missing fields: {missing}")
        except json.JSONDecodeError as e:
            status.add_failure(FailureMode.FM5_STATUS_CORRUPTION,
                               f"Corrupt JSON: {e}")
        except OSError as e:
            status.add_failure(FailureMode.FM5_STATUS_CORRUPTION,
                               f"Cannot read: {e}")

    def _is_actively_testing(self) -> bool:
        """Check if orchestrator is currently in a test phase."""
        if not STATUS_FILE.exists():
            return False
        try:
            data = json.loads(STATUS_FILE.read_text(encoding="utf-8"))
            task = data.get("current_task", "")
            return "test" in str(task).lower()
        except Exception:
            return False


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
        }

        handler = handlers.get(mode)
        if not handler:
            return RecoveryResult(mode=mode, success=False,
                                  action_taken="no handler", detail=detail)

        result = handler(detail)
        # Verify recovery
        result.verified = self._verify_recovery(mode)
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
        """FM8: Kill stale editor/tool processes."""
        killed = []
        for proc in STALE_PROCESSES:
            if proc == "testhost.exe":
                continue  # Handled by FM3
            count = _count_processes(proc)
            if count > 0:
                if _kill_by_name(proc):
                    killed.append(proc)

        return RecoveryResult(
            mode=FailureMode.FM8_STALE_PROCESSES,
            success=len(killed) > 0,
            action_taken=f"Killed: {killed}" if killed else "No processes to kill")

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
        self.backoff = INITIAL_BACKOFF_SECONDS
        self.checker = HealthChecker()
        self.recovery = RecoveryEngine()
        self._running = True
        self._orchestrator_proc: Optional[subprocess.Popen] = None

        signal.signal(signal.SIGINT, self._handle_signal)
        signal.signal(signal.SIGTERM, self._handle_signal)

    def _handle_signal(self, signum, frame):
        log.info("[SUPERVISOR] Received signal %s — shutting down", signum)
        self._running = False
        if self._orchestrator_proc and self._orchestrator_proc.poll() is None:
            log.info("[SUPERVISOR] Stopping orchestrator (PID %d)",
                     self._orchestrator_proc.pid)
            self._orchestrator_proc.terminate()
            try:
                self._orchestrator_proc.wait(timeout=30)
            except subprocess.TimeoutExpired:
                self._orchestrator_proc.kill()

    def run(self):
        """Main supervisor loop."""
        log.info("=" * 60)
        log.info("[SUPERVISOR] Starting — monitoring orchestrator")
        log.info("[SUPERVISOR] Args: %s", self.orchestrator_args or "(default)")
        log.info("[SUPERVISOR] Max restarts: %s",
                 self.max_restarts or "unlimited")
        log.info("=" * 60)

        while self._running:
            # Phase 1: Health check + recovery
            health = self.checker.check_all()

            if not health.healthy:
                log.warning("[SUPERVISOR] Health check FAILED — %d issues",
                            len(health.failures))
                for f in health.failures:
                    log.warning("  - %s: %s", f["mode"], f["detail"])

                # Recover each failure
                all_recovered = True
                for failure in health.failures:
                    result = self.recovery.recover(failure)
                    if result.success and result.verified:
                        log.info("[RECOVERY] %s: OK — %s (verified)",
                                 result.mode.value, result.action_taken)
                    elif result.success:
                        log.warning("[RECOVERY] %s: action taken but not verified — %s",
                                    result.mode.value, result.action_taken)
                    else:
                        log.error("[RECOVERY] %s: FAILED — %s",
                                  result.mode.value, result.action_taken)
                        all_recovered = False

                if not all_recovered:
                    log.error("[SUPERVISOR] Some recoveries failed — "
                              "waiting before retry")
                    time.sleep(self.backoff)
                    self.backoff = min(self.backoff * 2, MAX_RESTART_BACKOFF_SECONDS)
                    continue

            # Phase 2: Check if orchestrator is running
            if not self._is_orchestrator_running():
                if self.max_restarts > 0 and self.restart_count >= self.max_restarts:
                    log.info("[SUPERVISOR] Max restarts (%d) reached — exiting",
                             self.max_restarts)
                    break

                log.info("[SUPERVISOR] Orchestrator not running — "
                         "starting (attempt %d, backoff %ds)",
                         self.restart_count + 1, self.backoff)

                self._start_orchestrator()
                self.restart_count += 1
                # Reset backoff on successful start
                time.sleep(5)  # Give it time to initialize
                if self._is_orchestrator_running():
                    self.backoff = INITIAL_BACKOFF_SECONDS
                else:
                    self.backoff = min(self.backoff * 2, MAX_RESTART_BACKOFF_SECONDS)
                    log.error("[SUPERVISOR] Orchestrator failed to start — "
                              "next backoff: %ds", self.backoff)

            # Phase 3: Wait before next check
            for _ in range(HEALTH_CHECK_INTERVAL):
                if not self._running:
                    break
                time.sleep(1)

        log.info("[SUPERVISOR] Shutting down — %d restarts total",
                 self.restart_count)

    def _is_orchestrator_running(self) -> bool:
        """Check if we have a live orchestrator process."""
        if self._orchestrator_proc and self._orchestrator_proc.poll() is None:
            return True
        # Also check for externally-started orchestrators
        return len(_find_orchestrator_pids()) > 0

    def _start_orchestrator(self):
        """Start the orchestrator as a subprocess."""
        cmd = [sys.executable, str(ORCHESTRATOR_SCRIPT)]
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

    def one_shot_check(self) -> HealthStatus:
        """Run a single health check and return results."""
        health = self.checker.check_all()

        if health.healthy:
            log.info("[CHECK] All healthy")
        else:
            log.warning("[CHECK] %d issues found:", len(health.failures))
            for f in health.failures:
                log.warning("  - %s: %s", f["mode"], f["detail"])

            # Auto-recover
            for failure in health.failures:
                result = self.recovery.recover(failure)
                status = "OK" if result.success else "FAILED"
                verified = " (verified)" if result.verified else ""
                log.info("[RECOVERY] %s: %s — %s%s",
                         result.mode.value, status,
                         result.action_taken, verified)

        return health


# ── UTILITIES ───────────────────────────────────────────────────────────────
def _is_pid_alive(pid: int) -> bool:
    """Check if a process with given PID exists."""
    try:
        os.kill(pid, 0)
        return True
    except (OSError, ProcessLookupError):
        return False


def _find_orchestrator_pids() -> list:
    """Find all Python processes running iis_orchestrator.py."""
    try:
        result = subprocess.run(
            ["wmic", "process", "where",
             "name='python.exe' or name='python3.exe'",
             "get", "processid,commandline"],
            capture_output=True, text=True, timeout=10,
        )
        pids = []
        for line in result.stdout.splitlines():
            if "iis_orchestrator" in line and "supervisor" not in line.lower():
                # Extract PID (last number on line)
                parts = line.strip().split()
                if parts:
                    try:
                        pid = int(parts[-1])
                        pids.append(pid)
                    except ValueError:
                        pass
        return sorted(pids)
    except Exception:
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


# ── MAIN ────────────────────────────────────────────────────────────────────
def main():
    parser = argparse.ArgumentParser(
        description="Self-healing supervisor for iis_orchestrator.py")
    parser.add_argument("--check", action="store_true",
                        help="One-shot health check + recovery (no loop)")
    parser.add_argument("--max-restarts", type=int, default=0,
                        help="Max restarts before exit (0 = unlimited)")
    parser.add_argument("--orchestrator-args", type=str, default="",
                        help="Arguments to pass to orchestrator")
    args = parser.parse_args()

    if args.check:
        supervisor = Supervisor()
        health = supervisor.one_shot_check()
        sys.exit(0 if health.healthy else 1)
    else:
        supervisor = Supervisor(
            orchestrator_args=args.orchestrator_args,
            max_restarts=args.max_restarts,
        )
        supervisor.run()


if __name__ == "__main__":
    main()
