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
        self._orig_status = sup.STATUS_FILE
        self._orig_rate = sup.RATE_LIMIT_FILE
        self._orig_log = sup.LOG_FILE

        sup.LOCK_FILE = Path(self.tmpdir) / "orchestrator.lock"
        sup.STATUS_FILE = Path(self.tmpdir) / "orchestrator-status.json"
        sup.RATE_LIMIT_FILE = Path(self.tmpdir) / "_agent_rate_limits.json"
        sup.LOG_FILE = Path(self.tmpdir) / "orchestrator.log"

    def tearDown(self):
        sup.LOCK_FILE = self._orig_lock
        sup.STATUS_FILE = self._orig_status
        sup.RATE_LIMIT_FILE = self._orig_rate
        sup.LOG_FILE = self._orig_log

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

    @patch("orchestrator_supervisor.HealthChecker._is_actively_testing")
    @patch("orchestrator_supervisor._count_processes")
    def test_testhost_without_active_test_detected(self, mock_count, mock_testing):
        mock_count.return_value = 3
        mock_testing.return_value = False
        checker = sup.HealthChecker()
        status = sup.HealthStatus(healthy=True)
        checker._check_phantom_tests(status)
        self.assertFalse(status.healthy)
        self.assertEqual(status.failures[0]["mode"], "phantom_test_processes")

    @patch("orchestrator_supervisor.HealthChecker._is_actively_testing")
    @patch("orchestrator_supervisor._count_processes")
    def test_testhost_during_active_test_is_ok(self, mock_count, mock_testing):
        mock_count.return_value = 3
        mock_testing.return_value = True
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
               - datetime.timedelta(minutes=20)).isoformat()
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

    @patch("orchestrator_supervisor._count_processes")
    def test_stale_notepad_detected(self, mock_count):
        def side_effect(name):
            return 2 if name == "notepad.exe" else 0
        mock_count.side_effect = side_effect
        checker = sup.HealthChecker()
        status = sup.HealthStatus(healthy=True)
        checker._check_stale_processes(status)
        self.assertFalse(status.healthy)
        self.assertEqual(status.failures[0]["mode"], "stale_editor_processes")


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


# ── SUPERVISOR ──────────────────────────────────────────────────────────────
class TestSupervisor(TempFilesMixin, unittest.TestCase):

    @patch("orchestrator_supervisor._count_processes", return_value=0)
    @patch("orchestrator_supervisor._find_orchestrator_pids", return_value=[])
    def test_one_shot_check_healthy(self, mock_pids, mock_count):
        supervisor = sup.Supervisor()
        health = supervisor.one_shot_check()
        self.assertTrue(health.healthy)

    def test_one_shot_check_with_stale_lock(self):
        self._write_json(sup.LOCK_FILE, {"pid": 99999999, "started": "2026-01-01"})
        supervisor = sup.Supervisor()
        # one_shot_check auto-recovers
        health = supervisor.one_shot_check()
        # After recovery, lock should be gone
        self.assertFalse(sup.LOCK_FILE.exists())


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
