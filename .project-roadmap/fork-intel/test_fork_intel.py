#!/usr/bin/env python3
"""Tests for the fork intelligence pipeline.

Fixtures are shaped like real GitHub API payloads and are calibrated against
what the first live run actually produced, so a regression in the filters shows
up as a failing test rather than as noise in a report:

    BuloZB       66 activity-farming commits  -> all dropped
    suleyman-shb 40 bot/merge commits         -> all dropped
    Nizhal       upstream-maintainer commits  -> dropped (merge-base artifact)
    vindict6     OS dark-mode support         -> survives screening
    synthetic    workflow edit + committed dll -> quarantined, never queued

Run:
    python .project-roadmap/fork-intel/test_fork_intel.py
"""

import sys
import unittest
from pathlib import Path

sys.path.insert(0, str(Path(__file__).parent))

import fork_intel as fi  # noqa: E402


RULES = fi.load_rules()


def commit(subject, author_name="Jane Dev", author_login="janedev", parents=1):
    return {"sha": "0" * 40, "subject": subject, "author_name": author_name,
            "author_login": author_login, "parents": parents, "date": "2026-07-01T00:00:00Z"}


def changed_file(filename, patch="+ int x = 1;", status="modified", additions=1, deletions=0):
    return {"filename": filename, "patch": patch, "status": status,
            "additions": additions, "deletions": deletions}


class NormalizeSubjectTests(unittest.TestCase):
    def test_strips_conventional_prefix_and_issue_ref(self):
        self.assertEqual(fi.normalize_subject("fix(#143): Restore search focus"),
                         "restore search focus")

    def test_same_change_worded_with_and_without_prefix_matches(self):
        self.assertEqual(fi.normalize_subject("feat: add SFTP support"),
                         fi.normalize_subject("Add SFTP support"))

    def test_drops_sha_like_tokens(self):
        self.assertEqual(fi.normalize_subject("Revert a6508fc62 broken save"),
                         "revert broken save")


class NoiseFilterTests(unittest.TestCase):
    def setUp(self):
        self.ours = {fi.normalize_subject("Fix RDP focus after reconnect")}

    def drop_reason(self, c):
        return fi.is_noise(c, RULES, self.ours)

    def test_activity_farming_is_dropped(self):
        # BuloZB pushed 66 of these; none carry a code change.
        self.assertIn("noise subject pattern",
                      self.drop_reason(commit("chore: activity sync [2026-07-19]")))

    def test_merge_commit_is_dropped(self):
        self.assertEqual("merge commit",
                         self.drop_reason(commit("Merge pull request #8 from x/y", parents=2)))

    def test_bot_author_is_dropped(self):
        reason = self.drop_reason(commit("Add retry to connect",
                                         author_name="google-labs-jules[bot]",
                                         author_login="google-labs-jules[bot]"))
        self.assertIn("bot author", reason)

    def test_upstream_maintainer_commit_is_dropped_as_merge_base_artifact(self):
        # Nizhal/PeggyPro branched off an older upstream branch: the "extra"
        # commits are really upstream's own work, not fork work.
        reason = self.drop_reason(commit("Update sql_configuration.rst",
                                         author_name="Dimitrij", author_login="dimitrij"))
        self.assertIn("upstream maintainer", reason)

    def test_change_we_already_carry_is_dropped(self):
        self.assertIn("already in our history",
                      self.drop_reason(commit("fix: Fix RDP focus after reconnect")))

    def test_genuine_fork_work_survives(self):
        # vindict6's dark-mode commit is the calibration example of real value.
        self.assertIsNone(self.drop_reason(commit(
            "Dark mode: follow the OS, honor the theming setting, dark title bars")))

    def test_empty_subject_is_dropped(self):
        self.assertIsNotNone(self.drop_reason(commit("done")))


class SecurityScreenTests(unittest.TestCase):
    def flags_for(self, files):
        flags, _ = fi.screen_files(files, RULES)
        return {f["id"] for f in flags}

    def test_plain_source_change_is_clean(self):
        self.assertEqual(set(), self.flags_for([
            changed_file("mRemoteNG/UI/Forms/frmMain.cs", "+    label.Text = \"hi\";")]))

    def test_workflow_edit_is_flagged(self):
        self.assertIn("ci-workflow", self.flags_for([
            changed_file(".github/workflows/nightly.yml", "+  run: echo hi")]))

    def test_committed_binary_is_flagged(self):
        self.assertIn("binary-artifact", self.flags_for([
            changed_file("Tools/helper.dll", None, status="added")]))

    def test_added_file_without_text_diff_is_flagged(self):
        self.assertIn("opaque-file", self.flags_for([
            changed_file("assets/blob.xyz", None, status="added")]))

    def test_remote_download_in_added_lines_is_flagged(self):
        self.assertIn("network-download", self.flags_for([
            changed_file("mRemoteNG/App/Update.cs",
                         "+ var s = new WebClient().DownloadString(url);")]))

    def test_process_exec_is_flagged(self):
        self.assertIn("process-exec", self.flags_for([
            changed_file("mRemoteNG/Tools/Run.cs", "+ Process.Start(\"cmd.exe\");")]))

    def test_secret_access_is_flagged(self):
        self.assertIn("env-secret-access", self.flags_for([
            changed_file("scripts/ship.ps1", "+ $t = $env:GITHUB_TOKEN")]))

    def test_dependency_manifest_change_is_flagged(self):
        self.assertIn("dependency-manifest", self.flags_for([
            changed_file("Directory.Packages.props", "+ <PackageVersion Include=\"Evil\" />")]))

    def test_crypto_path_is_flagged(self):
        self.assertIn("security-code", self.flags_for([
            changed_file("mRemoteNG/Security/CryptoProvider.cs", "+ // tweak")]))

    def test_only_added_lines_are_inspected(self):
        # Removing a dangerous call must not look like introducing one.
        self.assertEqual(set(), self.flags_for([
            changed_file("mRemoteNG/Tools/Run.cs", "- Process.Start(\"cmd.exe\");")]))

    def test_stats_are_accumulated(self):
        _, stats = fi.screen_files([
            changed_file("a.cs", "+x", additions=3, deletions=1),
            changed_file("b.cs", "+y", additions=2, deletions=4)], RULES)
        self.assertEqual({"files": 2, "additions": 5, "deletions": 5, "binary_files": 0}, stats)


class ScoringTests(unittest.TestCase):
    def cand(self, **triage):
        base = {"value": 4, "effort": 2, "risk": 1, "already_in_our_fork": False,
                "action": "IMPORT", "applies_cleanly": "likely"}
        base.update(triage)
        return {"sha": "a" * 40, "triage": base, "security_flags": [],
                "stats": {"files": 4, "additions": 60, "deletions": 5}}

    def test_valuable_clean_change_reaches_tier_a(self):
        _, tier, _ = fi.score_candidate(self.cand(), RULES)
        self.assertEqual("A", tier)

    def test_security_flag_forces_quarantine_even_when_valuable(self):
        cand = self.cand(value=5)
        cand["security_flags"] = [{"id": "ci-workflow", "severity": "critical",
                                   "file": ".github/workflows/x.yml", "reason": "r"}]
        _, tier, why = fi.score_candidate(cand, RULES)
        self.assertEqual("Q", tier)
        self.assertIn("security review", why)

    def test_change_we_already_have_is_rejected(self):
        _, tier, _ = fi.score_candidate(self.cand(already_in_our_fork=True), RULES)
        self.assertEqual("D", tier)

    def test_rewrite_verdict_lands_in_tier_b(self):
        _, tier, _ = fi.score_candidate(self.cand(applies_cleanly="rewrite"), RULES)
        self.assertEqual("B", tier)

    def test_huge_diff_is_not_auto_cherry_picked(self):
        cand = self.cand(value=5)
        cand["stats"] = {"files": 400, "additions": 90000, "deletions": 5000}
        _, tier, _ = fi.score_candidate(cand, RULES)
        self.assertNotEqual("A", tier)

    def test_low_value_high_risk_is_rejected(self):
        _, tier, _ = fi.score_candidate(
            self.cand(value=1, effort=4, risk=5, action="WATCH"), RULES)
        self.assertEqual("D", tier)


class PreApprovalConsensusTests(unittest.TestCase):
    """Pre-approval is a consensus gate: it only ever removes work from a human,
    it must never grant approval on a split or missing opinion."""

    @staticmethod
    def vote(reviewer, verdict, aligned=True):
        return {"reviewer": reviewer, "vote": verdict, "aligned": aligned}

    def test_unanimous_approval_on_a_clean_change_pre_approves(self):
        votes = [self.vote("codex", "APPROVE"), self.vote("gemini", "APPROVE")]
        self.assertEqual("pre-approved", fi.consensus_decision(votes, False))

    def test_one_dissent_forces_manual_review(self):
        votes = [self.vote("codex", "APPROVE"), self.vote("gemini", "NEEDS_HUMAN")]
        self.assertEqual("manual-review", fi.consensus_decision(votes, False))

    def test_rejection_forces_manual_review(self):
        votes = [self.vote("codex", "REJECT"), self.vote("gemini", "APPROVE")]
        self.assertEqual("manual-review", fi.consensus_decision(votes, False))

    def test_reviewer_that_did_not_answer_counts_as_dissent(self):
        votes = [self.vote("codex", "APPROVE"), self.vote("gemini", "NO_ANSWER")]
        self.assertEqual("manual-review", fi.consensus_decision(votes, False))

    def test_misalignment_with_our_direction_blocks_pre_approval(self):
        votes = [self.vote("codex", "APPROVE"), self.vote("gemini", "APPROVE", aligned=False)]
        self.assertEqual("manual-review", fi.consensus_decision(votes, False))

    def test_security_flag_blocks_pre_approval_even_when_unanimous(self):
        votes = [self.vote("codex", "APPROVE"), self.vote("gemini", "APPROVE")]
        self.assertEqual("manual-review", fi.consensus_decision(votes, True))

    def test_no_votes_is_not_approval(self):
        self.assertEqual("manual-review", fi.consensus_decision([], False))


class VerdictParsingTests(unittest.TestCase):
    def test_parses_fenced_json(self):
        text = 'Here you go:\n```json\n[{"sha":"abc","action":"IMPORT"}]\n```\nDone.'
        self.assertEqual([{"sha": "abc", "action": "IMPORT"}], fi.extract_json_array(text))

    def test_parses_bare_array_with_trailing_prose(self):
        self.assertEqual([{"sha": "x"}],
                         fi.extract_json_array('[{"sha":"x"}] and that is my answer'))

    def test_returns_none_when_there_is_no_array(self):
        self.assertIsNone(fi.extract_json_array("I could not analyse these commits."))

    def test_returns_none_on_malformed_json(self):
        self.assertIsNone(fi.extract_json_array('[{"sha": }]'))

    def test_parses_single_object_verdict(self):
        text = 'My verdict:\n```json\n{"vote":"APPROVE","aligned_with_direction":true}\n```'
        self.assertEqual({"vote": "APPROVE", "aligned_with_direction": True},
                         fi.extract_json_object(text))

    def test_object_parser_ignores_surrounding_prose(self):
        self.assertEqual({"vote": "REJECT"},
                         fi.extract_json_object('I think {"vote":"REJECT"} because of X'))

    def test_object_parser_returns_none_without_an_object(self):
        self.assertIsNone(fi.extract_json_object("REJECT - too risky"))


if __name__ == "__main__":
    unittest.main(verbosity=2)
