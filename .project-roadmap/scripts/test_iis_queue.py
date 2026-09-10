"""The work-queue rule, pinned.

This rule has had four holes, and every one was found the same way: an issue sat
unanswered until somebody noticed by accident. Each case below is one of those holes,
named after the issue that fell through it, so a future edit that reopens one fails here
instead of in silence weeks later.

    python .project-roadmap/scripts/test_iis_queue.py
"""

import sys
import unittest
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))

from iis_orchestrator import iis_is_waiting_for_us  # noqa: E402

US = "robertpopa22"


def waiting(comments=None, author=US, labels=None, our_status="new"):
    return iis_is_waiting_for_us(comments or [], author, labels or [], US, our_status)


def comment(is_ours):
    return {"is_ours": is_ours, "author": US if is_ours else "someone"}


class WaitingForUsTests(unittest.TestCase):
    def test_the_last_word_decides_when_there_are_comments(self):
        self.assertTrue(waiting(comments=[comment(True), comment(False)]),
                        "a reporter had the last word and is waiting for an answer")
        self.assertFalse(waiting(comments=[comment(False), comment(True)]),
                         "we answered last; the ball is with the reporter")

    def test_a_fresh_report_from_outside_with_no_comments_is_work(self):
        # The first hole: gating the queue on unread comments hid brand-new reports,
        # which have no comments at all.
        self.assertTrue(waiting(author="someone", comments=[]))

    def test_an_auto_submitted_crash_report_is_work(self):
        # The second hole (#149, invisible for a month): the app files crash reports
        # under the maintainer's own account, so the author test alone never sees them.
        self.assertTrue(waiting(author=US, labels=["bug", "crash-report", "auto-submitted"]))

    def test_a_bug_we_filed_on_ourselves_is_work(self):
        # The fourth hole (#177, RDP splitter redraw): our own issue, no crash label, no
        # comments -- invisible to the queue for as long as it stayed open.
        self.assertTrue(waiting(author=US, labels=[], our_status="new"))
        self.assertTrue(waiting(author=US, labels=["bug"], our_status="triaged"))

    def test_an_issue_we_have_already_dispositioned_is_not_work(self):
        # This is what keeps the fix above from resurfacing everything we have settled --
        # e.g. the pinned "how this fork is maintained" explainer, which is wontfix.
        for status in ("wontfix", "duplicate", "released", "testing", "in-progress", "roadmap"):
            with self.subTest(status=status):
                self.assertFalse(waiting(author=US, our_status=status))

    def test_a_dispositioned_issue_still_counts_when_someone_else_opened_it(self):
        # An outside report with no comments has never been answered, whatever we
        # scribbled in our own lifecycle field.
        self.assertTrue(waiting(author="someone", our_status="released"))

    def test_a_missing_status_is_treated_as_untouched(self):
        self.assertTrue(waiting(author=US, our_status=None))
        self.assertTrue(waiting(author=US, our_status=""))

    def test_labels_are_matched_case_insensitively_and_survive_nulls(self):
        self.assertTrue(waiting(author=US, labels=["Crash-Report"], our_status="wontfix"))
        self.assertFalse(waiting(author=US, labels=[None], our_status="wontfix"))


if __name__ == "__main__":
    unittest.main(verbosity=2)
