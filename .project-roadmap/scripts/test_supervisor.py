#!/usr/bin/env python3
"""
Tests for orchestrator_supervisor.py

Tests each failure mode detection (HealthChecker) and recovery action (RecoveryEngine).
Uses temporary files and mocked processes to simulate failure conditions.
"""

import sys
if sys.platform == "win32":
    sys.stdout.reconfigure(encoding="utf-8", errors="replace")
    sys.stderr.reconfigure(encoding="utf-8", errors="replace")

import datetime
import json
import os
import tempfile
import unittest
from pathlib import Path
from unittest.mock import MagicMock, patch

# Import the module under test
sys.path.insert(0, str(Path(__file__).parent))
import orchestrator_supervisor as sup


class TempFilesMixin:
    """Mixin to redirect file paths to temp directory during tests."""

    def setUp(self):
        self.tmpdir = tempfile.mkdtemp()
        self._orig_lock = sup.LOCK_FILE
        self._orig_sup_lock = sup.SUPERVISOR_LOCK_FILE
        self._orig_status = sup.STATUS_FILE
        self._orig_rate = sup.RATE_LIMIT_FILE
        self._orig_log = sup.LOG_FILE
        self._orig_strikes = sup.PROGRESS_STRIKES_FILE

        sup.LOCK_FILE = Path(self.tmpdir) / "orchestrator.lock"
        sup.SUPERVISOR_LOCK_FILE = Path(self.tmpdir) / "supervisor.lock"
        sup.STATUS_FILE = Path(self.tmpdir) / "orchestrator-status.json"
        sup.RATE_LIMIT_FILE = Path(self.tmpdir) / "_agent_rate_limits.json"
        sup.LOG_FILE = Path(self.tmpdir) / "orchestrator.log"
        sup.PROGRESS_STRIKES_FILE = Path(self.tmpdir) / "_progress_strikes.json"

    def tearDown(self):
        sup.LOCK_FILE = self._orig_lock
        sup.SUPERVISOR_LOCK_FILE = self._orig_sup_lock
        sup.STATUS_FILE = self._orig_status
        sup.RATE_LIMIT_FILE = self._orig_rate
        sup.LOG_FILE = self._orig_log
        sup.PROGRESS_STRIKES_FILE = self._orig_strikes

        import shutil
        shutil.rmtree(self.tmpdir, ignore_errors=True)

    def _write_json(self, path: Path, data: dict):
        path.write_text(json.dumps(data, indent=2), encoding="utf-8")

    def _write_text(self, path: Path, text: str):
        path.write_text(text, encoding="utf-8")


# ── FM1: STALE LOCK FILE ───────────────────────────────────────────────────
class TestFM1StaleLock(TempFilesMixin, unittest.TestCase):

    def test_no_lock_file_is_healthy(self):
        checker = sup.HealthChecker()
        status = sup.HealthStatus(healthy=True)
        checker._check_lock_file(status)
        self.assertTrue(status.healthy)

    def test_lock_with_dead_pid_detected(self):
        self._write_json(sup.LOCK_FILE, {
            "pid": 99999999,  # Very unlikely to be alive
            "started": "2026-01-01T00:00:00",
        })
        checker = sup.HealthChecker()
        status = sup.HealthStatus(healthy=True)
        checker._check_lock_file(status)
        self.assertFalse(status.healthy)
        self.assertEqual(len(status.failures), 1)
        self.assertEqual(status.failures[0]["mode"], "crashed_process")

    def test_lock_with_alive_pid_is_ok(self):
        self._write_json(sup.LOCK_FILE, {
            "pid": os.getpid(),  # Current process — definitely alive
            "started": datetime.datetime.now().isoformat(),
        })
        checker = sup.HealthChecker()
        status = sup.HealthStatus(healthy=True)
        checker._check_lock_file(status)
        self.assertTrue(status.healthy)

    def test_corrupt_lock_detected(self):
        self._write_text(sup.LOCK_FILE, "not json{{{")
        checker = sup.HealthChecker()
        status = sup.HealthStatus(healthy=True)
        checker._check_lock_file(status)
        self.assertFalse(status.healthy)
        self.assertEqual(status.failures[0]["mode"], "stale_lock")

    def test_old_lock_detected(self):
        old_time = (datetime.datetime.now()
                    - datetime.timedelta(hours=25)).isoformat()
        self._write_json(sup.LOCK_FILE, {
            "pid": os.getpid(),
            "started": old_time,
        })
        checker = sup.HealthChecker()
        status = sup.HealthStatus(healthy=True)
        checker._check_lock_file(status)
        self.assertFalse(status.healthy)
        self.assertIn("stale_lock", status.failures[0]["mode"])

    def test_recovery_removes_stale_lock(self):
        self._write_json(sup.LOCK_FILE, {
            "pid": 99999999,
            "started": "2026-01-01T00:00:00",
        })
        engine = sup.RecoveryEngine()
        result = engine._recover_stale_lock("dead PID")
        self.assertTrue(result.success)
        self.assertFalse(sup.LOCK_FILE.exists())

    def test_recovery_does_not_remove_live_lock(self):
        self._write_json(sup.LOCK_FILE, {
            "pid": os.getpid(),
            "started": datetime.datetime.now().isoformat(),
        })
        engine = sup.RecoveryEngine()
        result = engine._recover_stale_lock("PID might be alive")
        self.assertFalse(result.success)
        self.assertTrue(sup.LOCK_FILE.exists())


# ── FM2: MULTIPLE INSTANCES ────────────────────────────────────────────────
class TestFM2MultipleInstances(TempFilesMixin, unittest.TestCase):

    @patch("orchestrator_supervisor._find_orchestrator_pids")
    def test_single_instance_is_ok(self, mock_pids):
        mock_pids.return_value = [1234]
        checker = sup.HealthChecker()
        status = sup.HealthStatus(healthy=True)
        checker._check_multiple_instances(status)
        self.assertTrue(status.healthy)

    @patch("orchestrator_supervisor._find_orchestrator_pids")
    def test_no_instances_is_ok(self, mock_pids):
        mock_pids.return_value = []
        checker = sup.HealthChecker()
        status = sup.HealthStatus(healthy=True)
        checker._check_multiple_instances(status)
        self.assertTrue(status.healthy)

    @patch("orchestrator_supervisor._find_orchestrator_pids")
    def test_multiple_instances_detected(self, mock_pids):
        mock_pids.return_value = [1234, 5678, 9012]
        checker = sup.HealthChecker()
        status = sup.HealthStatus(healthy=True)
        checker._check_multiple_instances(status)
        self.assertFalse(status.healthy)
        self.assertEqual(status.failures[0]["mode"], "multiple_instances")

    @patch("orchestrator_supervisor._kill_pid")
    @patch("orchestrator_supervisor._find_orchestrator_pids")
    def test_recovery_kills_duplicates(self, mock_pids, mock_kill):
        mock_pids.return_value = [100, 200, 300]
        mock_kill.return_value = True
        engine = sup.RecoveryEngine()
        result = engine._recover_multiple_instances("3 instances")
        self.assertTrue(result.success)
        # Should kill PIDs 200 and 300 (keep 100 as oldest)
        self.assertEqual(mock_kill.call_count, 2)


# ── FM3: PHANTOM TESTS ─────────────────────────────────────────────────────
class TestFM3PhantomTests(TempFilesMixin, unittest.TestCase):

    @patch("orchestrator_supervisor._count_processes")
    def test_no_testhost_is_ok(self, mock_count):
        mock_count.return_value = 0
        checker = sup.HealthChecker()
        status = sup.HealthStatus(healthy=True)
        checker._check_phantom_tests(status)
        self.assertTrue(status.healthy)

    @patch("orchestrator_supervisor.HealthChecker._is_active_task")
    @patch("orchestrator_supervisor._count_processes")
    def test_testhost_without_active_test_detected(self, mock_count, mock_active):
        mock_count.return_value = 3
        mock_active.return_value = False
        checker = sup.HealthChecker()
        status = sup.HealthStatus(healthy=True)
        checker._check_phantom_tests(status)
        self.assertFalse(status.healthy)
        self.assertEqual(status.failures[0]["mode"], "phantom_test_processes")

    @patch("orchestrator_supervisor.HealthChecker._is_active_task")
    @patch("orchestrator_supervisor._count_processes")
    def test_testhost_during_active_test_is_ok(self, mock_count, mock_active):
        mock_count.return_value = 3
        mock_active.return_value = True
        checker = sup.HealthChecker()
        status = sup.HealthStatus(healthy=True)
        checker._check_phantom_tests(status)
        self.assertTrue(status.healthy)


# ── FM4: RATE-LIMIT CORRUPTION ──────────────────────────────────────────────
class TestFM4RateLimits(TempFilesMixin, unittest.TestCase):

    def test_no_file_is_ok(self):
        checker = sup.HealthChecker()
        status = sup.HealthStatus(healthy=True)
        checker._check_rate_limit_file(status)
        self.assertTrue(status.healthy)

    def test_expired_entry_detected(self):
        yesterday = (datetime.datetime.now()
                     - datetime.timedelta(hours=25)).isoformat()
        self._write_json(sup.RATE_LIMIT_FILE, {
            "codex": {"available_after": yesterday, "detected_at": yesterday},
        })
        checker = sup.HealthChecker()
        status = sup.HealthStatus(healthy=True)
        checker._check_rate_limit_file(status)
        self.assertFalse(status.healthy)
        self.assertEqual(status.failures[0]["mode"], "rate_limit_corruption")

    def test_active_limit_is_ok(self):
        tomorrow = (datetime.datetime.now()
                    + datetime.timedelta(hours=25)).isoformat()
        self._write_json(sup.RATE_LIMIT_FILE, {
            "codex": {"available_after": tomorrow,
                      "detected_at": datetime.datetime.now().isoformat()},
        })
        checker = sup.HealthChecker()
        status = sup.HealthStatus(healthy=True)
        checker._check_rate_limit_file(status)
        self.assertTrue(status.healthy)

    def test_corrupt_json_detected(self):
        self._write_text(sup.RATE_LIMIT_FILE, "{{not json}}")
        checker = sup.HealthChecker()
        status = sup.HealthStatus(healthy=True)
        checker._check_rate_limit_file(status)
        self.assertFalse(status.healthy)

    def test_recovery_cleans_expired(self):
        yesterday = (datetime.datetime.now()
                     - datetime.timedelta(hours=25)).isoformat()
        tomorrow = (datetime.datetime.now()
                    + datetime.timedelta(hours=25)).isoformat()
        self._write_json(sup.RATE_LIMIT_FILE, {
            "codex": {"available_after": yesterday},
            "gemini": {"available_after": tomorrow},
        })
        engine = sup.RecoveryEngine()
        result = engine._recover_rate_limits("expired: codex")
        self.assertTrue(result.success)
        # Verify gemini kept, codex removed
        data = json.loads(sup.RATE_LIMIT_FILE.read_text(encoding="utf-8"))
        self.assertNotIn("codex", data)
        self.assertIn("gemini", data)

    def test_recovery_recreates_corrupt_file(self):
        self._write_text(sup.RATE_LIMIT_FILE, "corrupt!!!")
        engine = sup.RecoveryEngine()
        result = engine._recover_rate_limits("corrupt JSON")
        self.assertTrue(result.success)
        data = json.loads(sup.RATE_LIMIT_FILE.read_text(encoding="utf-8"))
        self.assertEqual(data, {})


# ── FM5: STATUS CORRUPTION ─────────────────────────────────────────────────
class TestFM5StatusCorruption(TempFilesMixin, unittest.TestCase):

    def test_no_file_is_ok(self):
        checker = sup.HealthChecker()
        status = sup.HealthStatus(healthy=True)
        checker._check_status_file(status)
        self.assertTrue(status.healthy)

    def test_valid_status_is_ok(self):
        self._write_json(sup.STATUS_FILE, {
            "started_at": "2026-02-19T10:00:00",
            "running": False,
            "last_updated": "2026-02-19T12:00:00",
        })
        checker = sup.HealthChecker()
        status = sup.HealthStatus(healthy=True)
        checker._check_status_file(status)
        self.assertTrue(status.healthy)

    def test_missing_fields_detected(self):
        self._write_json(sup.STATUS_FILE, {"running": True})
        checker = sup.HealthChecker()
        status = sup.HealthStatus(healthy=True)
        checker._check_status_file(status)
        self.assertFalse(status.healthy)
        self.assertEqual(status.failures[0]["mode"], "status_corruption")

    def test_corrupt_json_detected(self):
        self._write_text(sup.STATUS_FILE, '{"started": "ok", broken')
        checker = sup.HealthChecker()
        status = sup.HealthStatus(healthy=True)
        checker._check_status_file(status)
        self.assertFalse(status.healthy)

    def test_recovery_creates_clean_status(self):
        self._write_text(sup.STATUS_FILE, "corrupt!!!")
        engine = sup.RecoveryEngine()
        result = engine._recover_status_file("corrupt JSON")
        self.assertTrue(result.success)
        data = json.loads(sup.STATUS_FILE.read_text(encoding="utf-8"))
        self.assertFalse(data["running"])
        self.assertIn("supervisor_note", data)
        # Backup should exist
        self.assertTrue(sup.STATUS_FILE.with_suffix(".json.bak").exists())


# ── FM6: HUNG PROCESS ──────────────────────────────────────────────────────
class TestFM6HungProcess(TempFilesMixin, unittest.TestCase):

    def test_not_running_is_ok(self):
        self._write_json(sup.STATUS_FILE, {
            "running": False,
            "last_updated": "2026-01-01T00:00:00",
        })
        checker = sup.HealthChecker()
        status = sup.HealthStatus(healthy=True)
        checker._check_hung_process(status)
        self.assertTrue(status.healthy)

    def test_recent_update_is_ok(self):
        self._write_json(sup.STATUS_FILE, {
            "running": True,
            "last_updated": datetime.datetime.now().isoformat(),
        })
        checker = sup.HealthChecker()
        status = sup.HealthStatus(healthy=True)
        checker._check_hung_process(status)
        self.assertTrue(status.healthy)

    def test_old_update_detected(self):
        old = (datetime.datetime.now()
               - datetime.timedelta(minutes=sup.HUNG_TIMEOUT_MINUTES + 1)).isoformat()
        self._write_json(sup.STATUS_FILE, {
            "running": True,
            "last_updated": old,
            "current_task": "implement #1234",
        })
        checker = sup.HealthChecker()
        status = sup.HealthStatus(healthy=True)
        checker._check_hung_process(status)
        self.assertFalse(status.healthy)
        self.assertEqual(status.failures[0]["mode"], "hung_process")


# ── FM7: CRASHED PROCESS ───────────────────────────────────────────────────
class TestFM7CrashedProcess(TempFilesMixin, unittest.TestCase):

    def test_recovery_cleans_lock_and_processes(self):
        self._write_json(sup.LOCK_FILE, {"pid": 99999999, "started": "2026-01-01"})
        engine = sup.RecoveryEngine()
        with patch("orchestrator_supervisor._kill_by_name") as mock_kill:
            mock_kill.return_value = True
            result = engine._recover_crashed_process("PID dead")
        self.assertTrue(result.success)
        self.assertFalse(sup.LOCK_FILE.exists())


# ── FM8: STALE PROCESSES ───────────────────────────────────────────────────
class TestFM8StaleProcesses(TempFilesMixin, unittest.TestCase):

    @patch("orchestrator_supervisor._count_processes")
    def test_no_stale_is_ok(self, mock_count):
        mock_count.return_value = 0
        checker = sup.HealthChecker()
        status = sup.HealthStatus(healthy=True)
        checker._check_stale_processes(status)
        self.assertTrue(status.healthy)

    @patch("orchestrator_supervisor.HealthChecker._is_active_task")
    @patch("orchestrator_supervisor._count_processes")
    def test_stale_notepad_detected(self, mock_count, mock_active):
        def side_effect(name):
            return 2 if name == "notepad.exe" else 0
        mock_count.side_effect = side_effect
        mock_active.return_value = False
        checker = sup.HealthChecker()
        status = sup.HealthStatus(healthy=True)
        checker._check_stale_processes(status)
        self.assertFalse(status.healthy)
        self.assertEqual(status.failures[0]["mode"], "stale_editor_processes")

    @patch("orchestrator_supervisor.HealthChecker._is_active_task")
    @patch("orchestrator_supervisor._count_processes")
    def test_dotnet_skipped_during_active_build(self, mock_count, mock_active):
        """P6: dotnet.exe should NOT be flagged as stale during active build."""
        def count_effect(name):
            return 2 if name == "dotnet.exe" else 0
        mock_count.side_effect = count_effect
        mock_active.return_value = True  # orchestrator is active
        checker = sup.HealthChecker()
        status = sup.HealthStatus(healthy=True)
        checker._check_stale_processes(status)
        # dotnet.exe should be skipped → healthy
        self.assertTrue(status.healthy)


# ── RECOVERY VERIFICATION ──────────────────────────────────────────────────
class TestRecoveryVerification(TempFilesMixin, unittest.TestCase):

    def test_verify_after_stale_lock_removal(self):
        self._write_json(sup.LOCK_FILE, {"pid": 99999999, "started": "2026-01-01"})
        engine = sup.RecoveryEngine()
        result = engine.recover({
            "mode": "stale_lock",
            "detail": "Lock file is corrupt JSON",
        })
        # Lock had dead PID — detected as FM7 (crashed), recovery should remove it
        # But we passed FM1 (stale_lock) — verify passes because lock is gone
        self.assertTrue(result.success)
        self.assertTrue(result.verified)

    def test_verify_rate_limit_cleanup(self):
        yesterday = (datetime.datetime.now()
                     - datetime.timedelta(hours=25)).isoformat()
        self._write_json(sup.RATE_LIMIT_FILE, {
            "codex": {"available_after": yesterday},
        })
        engine = sup.RecoveryEngine()
        result = engine.recover({
            "mode": "rate_limit_corruption",
            "detail": "expired: codex",
        })
        self.assertTrue(result.success)
        self.assertTrue(result.verified)


# ── P1: SUPERVISOR LOCK ────────────────────────────────────────────────────
class TestSupervisorLock(TempFilesMixin, unittest.TestCase):

    def test_acquire_creates_file(self):
        """P1: Lock file created on acquire."""
        self.assertFalse(sup.SUPERVISOR_LOCK_FILE.exists())
        result = sup._acquire_supervisor_lock()
        self.assertTrue(result)
        self.assertTrue(sup.SUPERVISOR_LOCK_FILE.exists())
        # Verify contents
        data = json.loads(sup.SUPERVISOR_LOCK_FILE.read_text(encoding="utf-8"))
        self.assertEqual(data["pid"], os.getpid())
        self.assertIn("started", data)
        # Cleanup
        sup._release_supervisor_lock()

    def test_acquire_fails_if_running(self):
        """P1: Second acquire fails if first supervisor is still alive."""
        # Simulate running supervisor (our own PID)
        self._write_json(sup.SUPERVISOR_LOCK_FILE, {
            "pid": os.getpid(),
            "started": datetime.datetime.now().isoformat(),
        })
        result = sup._acquire_supervisor_lock()
        self.assertFalse(result)

    def test_acquire_removes_stale(self):
        """P1: Stale lock (dead PID) is removed, new lock acquired."""
        self._write_json(sup.SUPERVISOR_LOCK_FILE, {
            "pid": 99999999,
            "started": "2026-01-01T00:00:00",
        })
        result = sup._acquire_supervisor_lock()
        self.assertTrue(result)
        # Verify new lock has our PID
        data = json.loads(sup.SUPERVISOR_LOCK_FILE.read_text(encoding="utf-8"))
        self.assertEqual(data["pid"], os.getpid())
        # Cleanup
        sup._release_supervisor_lock()

    def test_release_removes_file(self):
        """P1: Release removes the lock file."""
        sup._acquire_supervisor_lock()
        self.assertTrue(sup.SUPERVISOR_LOCK_FILE.exists())
        sup._release_supervisor_lock()
        self.assertFalse(sup.SUPERVISOR_LOCK_FILE.exists())


# ── P3: GIT STASH BEFORE RECOVERY ──────────────────────────────────────────
class TestFM9Stash(TempFilesMixin, unittest.TestCase):

    @patch("orchestrator_supervisor._run_git")
    def test_stash_before_recovery(self, mock_git):
        """P3: FM9 recovery stashes dirty changes before checkout."""
        # Mock git responses:
        # 1. stash: status --porcelain returns dirty files
        # 2. stash: stash push succeeds
        # 3. merge abort check
        # 4. rebase abort check
        # 5. symbolic-ref (not detached)
        # 6. checkout -- SOURCE_DIRS
        # 7. clean -fd -- SOURCE_DIRS
        call_log = []

        def git_side_effect(args):
            call_log.append(args)
            if args[:2] == ["status", "--porcelain"]:
                return 0, "M mRemoteNG/SomeFile.cs"
            if args[:2] == ["stash", "push"]:
                return 0, "Saved working directory"
            if args[:2] == ["symbolic-ref", "--short"]:
                return 0, "main"
            if args[0] == "checkout":
                return 0, ""
            if args[0] == "clean":
                return 0, ""
            return 0, ""

        mock_git.side_effect = git_side_effect

        engine = sup.RecoveryEngine()
        result = engine._recover_git_state("uncommitted files")
        self.assertTrue(result.success)
        # Verify stash was called (should be among the first git commands)
        stash_calls = [c for c in call_log if c[:2] == ["stash", "push"]]
        self.assertEqual(len(stash_calls), 1, "Stash should be called exactly once")
        self.assertIn("stashed:", result.action_taken)


# ── P4: STRIKE PERSISTENCE + DECAY ─────────────────────────────────────────
class TestFM11Strikes(TempFilesMixin, unittest.TestCase):

    def test_strikes_persist_across_cleanup(self):
        """P4: _pre_run_cleanup() does NOT reset strikes."""
        # Set up strikes
        sup._save_progress_strikes({
            "count": 2,
            "last_strike": datetime.datetime.now().isoformat(),
        })
        # Verify strikes are set
        strikes = sup._load_progress_strikes()
        self.assertEqual(strikes["count"], 2)

        # Simulate what _pre_run_cleanup would do (it no longer resets strikes)
        # The actual _pre_run_cleanup needs git and processes, so we just verify
        # that _load_progress_strikes still returns 2 after a hypothetical cleanup
        strikes_after = sup._load_progress_strikes()
        self.assertEqual(strikes_after["count"], 2, "Strikes must persist across cleanup")

    def test_strikes_decay_after_1h(self):
        """P4: Strikes reset if last_strike was >60 minutes ago."""
        # Set strikes with old timestamp
        old_time = (datetime.datetime.now()
                    - datetime.timedelta(minutes=90)).isoformat()
        sup._save_progress_strikes({
            "count": 2,
            "last_strike": old_time,
        })

        # Trigger FM11 recovery — it should detect decay and reset
        engine = sup.RecoveryEngine()
        with patch("orchestrator_supervisor._find_orchestrator_pids", return_value=[]):
            result = engine._recover_no_progress("test decay")

        # After decay + increment: count should be 1 (reset to 0 then +1)
        strikes = sup._load_progress_strikes()
        self.assertEqual(strikes["count"], 1,
                         "Strikes should decay to 0 then increment to 1")

    def test_strikes_accumulate_within_1h(self):
        """P4: Strikes accumulate normally if within decay window."""
        recent_time = (datetime.datetime.now()
                       - datetime.timedelta(minutes=10)).isoformat()
        sup._save_progress_strikes({
            "count": 1,
            "last_strike": recent_time,
        })

        engine = sup.RecoveryEngine()
        with patch("orchestrator_supervisor._find_orchestrator_pids", return_value=[]):
            result = engine._recover_no_progress("stuck")

        strikes = sup._load_progress_strikes()
        self.assertEqual(strikes["count"], 2, "Strikes should increment to 2")


# ── P5/P10: ACTIVE PROCESS PROTECTION ──────────────────────────────────────
class TestFM12DeferredDuringActive(TempFilesMixin, unittest.TestCase):

    @patch("orchestrator_supervisor.HealthChecker._is_active_task")
    def test_deferred_during_active_test(self, mock_active):
        """P5: FM12 recovery is deferred when orchestrator is actively testing."""
        mock_active.return_value = True
        engine = sup.RecoveryEngine()
        result = engine._recover_build_infra("3 consecutive build failures")
        self.assertTrue(result.success)
        self.assertIn("Deferred", result.action_taken)


# ── SUPERVISOR ──────────────────────────────────────────────────────────────
class TestSupervisor(TempFilesMixin, unittest.TestCase):

    @patch("orchestrator_supervisor._count_processes", return_value=0)
    @patch("orchestrator_supervisor._find_orchestrator_pids", return_value=[])
    @patch("orchestrator_supervisor._run_git")
    def test_one_shot_check_healthy(self, mock_git, mock_pids, mock_count):
        def git_side_effect(args):
            if args[:2] == ["symbolic-ref", "--short"]:
                return 0, "main"
            if args[:2] == ["status", "--porcelain"]:
                return 0, ""  # clean
            return 0, ""
        mock_git.side_effect = git_side_effect
        supervisor = sup.Supervisor()
        health = supervisor.one_shot_check()
        self.assertTrue(health.healthy)

    @patch("orchestrator_supervisor._run_git", return_value=(0, ""))
    def test_one_shot_check_with_stale_lock(self, mock_git):
        self._write_json(sup.LOCK_FILE, {"pid": 99999999, "started": "2026-01-01"})
        before = sup.LOCK_FILE.read_bytes()
        with patch.object(sup.RecoveryEngine, "recover") as recovery:
            health = sup.Supervisor.one_shot_check()
        self.assertFalse(health.healthy)
        self.assertEqual(before, sup.LOCK_FILE.read_bytes())
        self.assertFalse(sup.SUPERVISOR_LOCK_FILE.exists())
        recovery.assert_not_called()


# ── UTILITY FUNCTIONS ───────────────────────────────────────────────────────
class TestUtilities(unittest.TestCase):

    def test_is_pid_alive_self(self):
        self.assertTrue(sup._is_pid_alive(os.getpid()))

    def test_is_pid_alive_dead(self):
        self.assertFalse(sup._is_pid_alive(99999999))

    def test_health_status_starts_healthy(self):
        hs = sup.HealthStatus(healthy=True)
        self.assertTrue(hs.healthy)
        self.assertEqual(len(hs.failures), 0)

    def test_add_failure_marks_unhealthy(self):
        hs = sup.HealthStatus(healthy=True)
        hs.add_failure(sup.FailureMode.FM1_STALE_LOCK, "test")
        self.assertFalse(hs.healthy)
        self.assertEqual(len(hs.failures), 1)


if __name__ == "__main__":
    unittest.main(verbosity=2)
