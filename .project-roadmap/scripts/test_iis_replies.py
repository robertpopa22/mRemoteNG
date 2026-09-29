"""Reply publication regressions. No GitHub writes or product UI."""

import contextlib
import io
import json
import tempfile
import unittest
from pathlib import Path
from unittest.mock import patch

import iis_orchestrator as iis


class ReplyTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        self.meta = self.root / "_meta.json"
        self.meta.write_text(json.dumps({
            "repos": {"fork": "example/fork", "upstream": "example/upstream"},
            "comment_templates": {"testing": "{{VERIFICATION}}"},
        }), encoding="utf-8")
        (self.root / "fork").mkdir()
        self.issue = self.root / "fork" / "0198.json"
        self.issue.write_text(json.dumps({
            "our_status": "in-progress", "github_updated_at": "2026-09-29T08:10:22Z",
        }), encoding="utf-8")
        self.before = self.issue.read_bytes()
        for name, value in (("META_PATH", self.meta), ("ISSUES_DB_ROOT", self.root)):
            p = patch.object(iis, name, value)
            p.start()
            self.addCleanup(p.stop)
        self.send = self.enterContext(patch.object(iis, "gh_post_comment", return_value=True))
        self.fetch = self.enterContext(patch.object(iis, "gh_run_json", return_value={
            "updatedAt": "2026-09-29T08:10:22Z",
        }))
        self.enterContext(contextlib.redirect_stdout(io.StringIO()))

    def update(self, **kwargs):
        return iis.iis_update(198, "testing", repo="fork", **kwargs)

    def reply(self, text="Thanks. Dialog improved; checkbox clipping remains open."):
        path = self.root / "reply.md"
        path.write_text(text, encoding="utf-8")
        return path

    def assert_not_written(self):
        self.send.assert_not_called()
        self.assertEqual(self.before, self.issue.read_bytes())

    def test_bare_post_flag_does_not_send_or_change_status(self):
        self.assertFalse(self.update(post_comment=True))
        self.assert_not_written()
        self.fetch.assert_not_called()

    def test_empty_missing_and_unfinished_reply_are_rejected(self):
        for path in (self.root / "missing.md", self.reply("")):
            self.assertFalse(self.update(post_comment=True, comment_file=path))
            self.assert_not_written()
        self.assertFalse(self.update(post_comment=True, comment_file=self.reply("{{VERIFICATION}}")))
        self.assert_not_written()

    def test_new_feedback_prevents_stale_reply_and_local_transition(self):
        self.fetch.return_value = {"updatedAt": "2026-09-29T09:00:00Z"}
        self.assertFalse(self.update(post_comment=True, comment_file=self.reply()))
        self.assert_not_written()

    def test_unavailable_or_missing_timestamp_fails_closed(self):
        for response in (None, {}, {"number": 198}):
            self.fetch.return_value = response
            self.assertFalse(self.update(post_comment=True, comment_file=self.reply()))
            self.assert_not_written()

    def test_complete_file_is_posted_to_exact_repo_and_recorded(self):
        path = self.reply()
        self.assertTrue(self.update(post_comment=True, comment_file=path))
        self.send.assert_called_once_with("example/fork", 198, path.read_text(encoding="utf-8"))
        self.assertTrue(json.loads(self.issue.read_text())["iterations"][-1]["comment_posted"])

    def test_preview_and_local_status_change_do_not_send(self):
        self.assertTrue(self.update(comment_file=self.reply()))
        self.send.assert_not_called()
        self.fetch.assert_not_called()
        self.assertFalse(json.loads(self.issue.read_text())["iterations"][-1]["comment_posted"])

    def test_failed_send_does_not_report_success_or_change_local_status(self):
        self.send.return_value = False
        self.assertFalse(self.update(post_comment=True, comment_file=self.reply()))
        self.send.assert_called_once()
        self.assertEqual(self.before, self.issue.read_bytes())

    def test_automatic_fix_drafts_and_preserves_reviewed_text(self):
        with patch.object(iis, "_run") as run:
            self.assertFalse(iis.post_github_comment(200, "abcd1234", "Candidate change"))
            draft = self.root / "reply-drafts" / "upstream-200-abcd1234.md"
            self.assertIn("{{VERIFICATION}}", draft.read_text(encoding="utf-8"))
            draft.write_text("Reviewed text", encoding="utf-8")
            self.assertFalse(iis.post_github_comment(200, "abcd1234", "Candidate change"))
            self.assertEqual("Reviewed text", draft.read_text(encoding="utf-8"))
            run.assert_not_called()


class QueueScopeTests(unittest.TestCase):
    def test_maintainer_promise_survives_empty_inbound_queue(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            (root / "maintainer-actions.json").write_text(json.dumps({"actions": [
                {"id": "fork-192", "repo": "fork", "status": "pending", "next_action": "Vendor submission",
                 "owner": "maintainer", "reviewed_at": "2026-09-29"},
                {"id": "upstream-1", "repo": "upstream", "status": "pending", "next_action": "Hidden"},
                {"id": "fork-done", "repo": "fork", "status": "done", "next_action": "Finished"},
            ]}), encoding="utf-8")
            output = io.StringIO()
            with patch.object(iis, "ISSUES_DB_ROOT", root), \
                    patch.object(iis, "iis_read_json", wraps=iis.iis_read_json) as read, \
                    patch.object(iis, "META_PATH", root / "meta.json"), \
                    patch.object(iis, "iis_load_all_issues", return_value=[]), \
                    contextlib.redirect_stdout(output):
                (root / "meta.json").write_text('{"last_sync":"2026-09-29"}', encoding="utf-8")
                iis.iis_analyze(waiting_only=True, repos="fork")
            self.assertIn("fork-192", output.getvalue())
            self.assertNotIn("upstream-1", output.getvalue())
            self.assertNotIn("fork-done", output.getvalue())

    def test_lowercase_fork_bugs_are_not_classified_as_enhancements(self):
        self.assertEqual("P2-bug", iis._auto_classify({"labels": ["bug"]})["priority"])
        self.assertEqual("P1-security", iis._auto_classify({"labels": ["Security"]})["priority"])
        self.assertEqual("P0-critical", iis._auto_classify({
            "labels": ["bug"], "priority": "P0-critical",
        })["priority"])

    def test_closed_new_feedback_remains_actionable_and_old_records_stay_visible(self):
        records = [
            {"number": 14, "state": "closed", "waiting_for_us": True},
            {"number": 165, "state": "closed", "waiting_for_us": True,
             "unread_comments": 1, "needs_action": True},
            {"number": 200, "state": "open", "waiting_for_us": True},
        ]
        out = io.StringIO()
        with patch.object(iis, "iis_read_json", return_value={"last_sync": "today"}), \
                patch.object(iis, "iis_load_all_issues", return_value=records), \
                contextlib.redirect_stdout(out):
            iis.iis_analyze(repos="fork", waiting_only=True)
        self.assertIn("review separately): 1", out.getvalue())
        self.assertIn("#14", out.getvalue())
        self.assertIn("#165", out.getvalue())
        self.assertIn("Awaiting response: 2", out.getvalue())

    def test_fork_intake_does_not_count_upstream_backlog(self):
        with patch.object(iis, "iis_read_json", return_value={"last_sync": "today"}), \
                patch.object(iis, "iis_load_all_issues", return_value=[]) as load, \
                contextlib.redirect_stdout(io.StringIO()):
            iis.iis_analyze(repos="fork", waiting_only=True)
        load.assert_called_once_with(("fork",))


if __name__ == "__main__":
    unittest.main(verbosity=2)
