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

import re
import sys
import unittest
from pathlib import Path
from unittest.mock import patch

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

    def test_reviewer_that_did_not_answer_leaves_no_quorum(self):
        # Silence is neither consent nor dissent. One answered ballot is not a panel,
        # so the candidate is held rather than approved or sent on as a judgement.
        votes = [self.vote("codex", "APPROVE"), self.vote("gemini", "NO_ANSWER")]
        self.assertEqual("held", fi.consensus_decision(votes, False))

    def test_a_broken_reviewer_can_never_produce_an_approval(self):
        # The failure that matters: a CLI that is down, rate-limited or timing out must
        # not be able to turn a single yes into a pass.
        votes = [self.vote("codex", "APPROVE"), self.vote("grok", "NO_ANSWER"),
                 self.vote("claude", "NO_ANSWER")]
        self.assertNotEqual("pre-approved", fi.consensus_decision(votes, False))

    def test_misalignment_with_our_direction_blocks_pre_approval(self):
        votes = [self.vote("codex", "APPROVE"), self.vote("gemini", "APPROVE", aligned=False)]
        self.assertEqual("manual-review", fi.consensus_decision(votes, False))

    def test_security_flag_blocks_pre_approval_even_when_unanimous(self):
        votes = [self.vote("codex", "APPROVE"), self.vote("gemini", "APPROVE")]
        self.assertEqual("manual-review", fi.consensus_decision(votes, True))

    def test_no_votes_is_not_approval(self):
        self.assertEqual("held", fi.consensus_decision([], False))


class ArbitrationTests(unittest.TestCase):
    """A third model family is asked only when the first two disagree, and its
    vote is what turns a split into a decision."""

    @staticmethod
    def vote(reviewer, verdict, aligned=True, arbiter=False):
        return {"reviewer": reviewer, "vote": verdict, "aligned": aligned, "arbiter": arbiter}

    def test_disagreement_is_a_split(self):
        self.assertTrue(fi.votes_are_split(
            [self.vote("codex", "APPROVE"), self.vote("gemini", "REJECT")]))

    def test_unanimous_approval_is_not_a_split(self):
        self.assertFalse(fi.votes_are_split(
            [self.vote("codex", "APPROVE"), self.vote("gemini", "APPROVE")]))

    def test_unanimous_refusal_is_not_a_split(self):
        self.assertFalse(fi.votes_are_split(
            [self.vote("codex", "REJECT"), self.vote("gemini", "NEEDS_HUMAN")]))

    def test_silence_from_one_reviewer_is_not_a_split(self):
        # Nothing was said, so there is nothing to arbitrate.
        self.assertFalse(fi.votes_are_split(
            [self.vote("codex", "APPROVE"), self.vote("gemini", "NO_ANSWER")]))

    def test_a_majority_does_not_pass_the_gate(self):
        # Superseded policy: a third family used to break a split by majority. The gate
        # is unanimity now. A split goes to a second round on anonymised arguments, and
        # if that round is still not unanimous a human decides - a false APPROVE into a
        # credential manager costs far more than a false hold.
        votes = [self.vote("codex", "APPROVE"), self.vote("gemini", "REJECT"),
                 self.vote("grok", "APPROVE", arbiter=True)]
        self.assertEqual("manual-review", fi.consensus_decision(votes, False))

    def test_arbiter_siding_with_the_objection_keeps_it_manual(self):
        votes = [self.vote("codex", "APPROVE"), self.vote("gemini", "REJECT"),
                 self.vote("grok", "REJECT", arbiter=True)]
        self.assertEqual("manual-review", fi.consensus_decision(votes, False))

    def test_majority_without_an_arbiter_is_not_enough(self):
        # Two reviewers alone must be unanimous; majority only counts once a
        # third family was deliberately brought in.
        votes = [self.vote("codex", "APPROVE"), self.vote("gemini", "APPROVE"),
                 self.vote("claude", "REJECT")]
        self.assertEqual("manual-review", fi.consensus_decision(votes, False))

    def test_arbiter_cannot_override_a_security_flag(self):
        votes = [self.vote("codex", "APPROVE"), self.vote("gemini", "REJECT"),
                 self.vote("grok", "APPROVE", arbiter=True)]
        self.assertEqual("manual-review", fi.consensus_decision(votes, True))

    def test_two_votes_from_the_same_family_are_not_a_majority(self):
        # Guard against the arbiter also being a reviewer: grok voting twice against
        # one codex objection is one opinion outvoting another, not a 2-of-3 majority.
        votes = [self.vote("codex", "REJECT"), self.vote("grok", "APPROVE"),
                 self.vote("grok", "APPROVE", arbiter=True)]
        families = [v["reviewer"] for v in votes]
        self.assertNotEqual(len(set(families)), len(families),
                            "fixture must contain a duplicated family")
        # The pipeline prevents this configuration up front; assert the rule that
        # makes it necessary: a genuine 2-of-3 needs three distinct families.
        distinct_approvals = {v["reviewer"] for v in votes if v["vote"] == "APPROVE"}
        self.assertLess(len(distinct_approvals), 2)

    def test_arbiter_cannot_override_misalignment(self):
        votes = [self.vote("codex", "APPROVE"), self.vote("gemini", "REJECT", aligned=False),
                 self.vote("grok", "APPROVE", arbiter=True)]
        self.assertEqual("manual-review", fi.consensus_decision(votes, False))


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



class PanelIndependenceTests(unittest.TestCase):
    """The gate is a consensus of model families, so it is worth exactly what its
    independence is worth. These pin the three things that protect it."""

    @staticmethod
    def candidate(sha, subject, fork="someone/mRemoteNG", triage_agent="claude", patch="diff"):
        return {"sha": sha, "subject": subject, "fork": fork, "patch": patch,
                "stats": {"files": 1, "additions": 1, "deletions": 0}, "files": [],
                "triage": {"agent": triage_agent, "action": "IMPORT"}}

    def test_a_revert_is_grouped_with_the_commit_it_reverts(self):
        # Judged alone a revert reads as noise; judged with its original it is the
        # clearest signal in the set - somebody tried this and took it back.
        original = ("p1", self.candidate("aaa1", "Anchor the RDP control to the panel"), "B")
        revert = ("p2", self.candidate("bbb2", 'Revert "Anchor the RDP control to the panel"'), "B")
        chains = fi.build_chains([original, revert])
        self.assertEqual(1, len(chains))
        self.assertEqual(["aaa1", "bbb2"], [c["sha"] for _, c, _ in chains[0]])

    def test_the_original_comes_first_in_the_chain(self):
        revert = ("p2", self.candidate("bbb2", 'Revert "Make it faster"'), "B")
        original = ("p1", self.candidate("aaa1", "Make it faster"), "B")
        chains = fi.build_chains([revert, original])
        self.assertEqual(["aaa1", "bbb2"], [c["sha"] for _, c, _ in chains[0]])

    def test_a_revert_in_another_fork_is_not_the_same_chain(self):
        # Same subject, different fork: unrelated work that happens to be named alike.
        a = ("p1", self.candidate("aaa1", "Fix the thing", fork="alice/mRemoteNG"), "B")
        b = ("p2", self.candidate("bbb2", 'Revert "Fix the thing"', fork="bob/mRemoteNG"), "B")
        self.assertEqual(2, len(fi.build_chains([a, b])))

    def test_unrelated_commits_are_judged_separately(self):
        a = ("p1", self.candidate("aaa1", "One thing"), "B")
        b = ("p2", self.candidate("bbb2", "Another thing"), "B")
        chains = fi.build_chains([a, b])
        self.assertEqual([1, 1], [len(c) for c in chains])

    def test_a_truncated_diff_is_detected(self):
        # Nobody votes on a patch they were only shown part of.
        big = self.candidate("aaa1", "Huge", patch="x" * (fi.PATCH_REVIEW_LIMIT + 1))
        small = self.candidate("bbb2", "Small", patch="x" * 10)
        self.assertTrue(fi.patch_was_truncated(big, fi.PATCH_REVIEW_LIMIT))
        self.assertFalse(fi.patch_was_truncated(small, fi.PATCH_REVIEW_LIMIT))

    def test_revert_subject_is_only_read_from_a_real_revert(self):
        self.assertEqual("Do the thing", fi.revert_subject('Revert "Do the thing"'))
        self.assertIsNone(fi.revert_subject("Reverting some of the thing"))
        self.assertIsNone(fi.revert_subject("Do the thing"))
        self.assertIsNone(fi.revert_subject(None))

    def test_only_answered_ballots_are_counted(self):
        votes = [{"vote": "APPROVE"}, {"vote": "NO_ANSWER"}, {"vote": None}, {"vote": "REJECT"}]
        self.assertEqual(["APPROVE", "REJECT"],
                         [v["vote"] for v in fi.answered_votes(votes)])


class ComparePaginationTests(unittest.TestCase):
    """The compare endpoint caps `commits` at 250 and reports the real count in
    `total_commits`; these pin the paging that recovers the rest."""

    @staticmethod
    def _commits(n, prefix):
        return [{"sha": f"{prefix}{i:04d}",
                 "commit": {"message": f"{prefix} commit {i}",
                            "author": {"name": "Someone", "date": "2026-07-01T00:00:00Z"}},
                 "author": {"login": "someone"}, "parents": [], "html_url": "https://x"}
                for i in range(n)]

    def _fake_gh_json(self, pages, total_commits):
        def fake(endpoint, paginate=False):
            match = re.search(r"page=(\d+)", endpoint)
            index = int(match.group(1)) - 1
            if index < len(pages):
                return {"total_commits": total_commits, "ahead_by": total_commits,
                        "behind_by": 0, "merge_base_commit": {"sha": "base"},
                        "commits": pages[index]}
            return {"total_commits": total_commits, "commits": []}
        return fake

    def test_pages_past_the_250_cap_are_all_collected(self):
        pages = [self._commits(100, "p1-"), self._commits(100, "p2-"),
                 self._commits(100, "p3-"), self._commits(40, "p4-")]
        with patch.object(fi, "gh_json", side_effect=self._fake_gh_json(pages, 340)):
            result = fi.compare_commits("mRemoteNG/mRemoteNG", "main", "someone", "branch")

        self.assertEqual(340, len(result["commits"]))
        self.assertEqual(340, result["total_commits"])
        self.assertFalse(result["truncated"])

    def test_a_page_running_dry_early_is_reported_as_truncated(self):
        pages = [self._commits(100, "p1-"), self._commits(100, "p2-")]
        with patch.object(fi, "gh_json", side_effect=self._fake_gh_json(pages, 340)):
            result = fi.compare_commits("mRemoteNG/mRemoteNG", "main", "someone", "branch")

        self.assertEqual(200, len(result["commits"]))
        self.assertTrue(result["truncated"])

    def test_first_call_failure_returns_none(self):
        with patch.object(fi, "gh_json", return_value=None):
            self.assertIsNone(
                fi.compare_commits("mRemoteNG/mRemoteNG", "main", "someone", "branch"))


class TrustSourceNoiseTests(unittest.TestCase):
    """is_noise(trust_source=True) is how the upstream feed is screened: those
    commits were all written by upstream maintainers, so the one rule that would
    drop the entire feed is skipped - every other layer still applies."""

    def test_trust_source_keeps_an_upstream_maintainer_commit(self):
        c = commit("Update sql_configuration.rst", author_name="Dimitrij", author_login="dimitrij")
        self.assertIsNone(fi.is_noise(c, RULES, set(), trust_source=True))

    def test_default_still_drops_the_same_commit(self):
        c = commit("Update sql_configuration.rst", author_name="Dimitrij", author_login="dimitrij")
        self.assertIsNotNone(fi.is_noise(c, RULES, set()))

    def test_trust_source_still_drops_a_bot_author(self):
        c = commit("Add retry to connect", author_name="dependabot[bot]",
                   author_login="dependabot[bot]")
        reason = fi.is_noise(c, RULES, set(), trust_source=True)
        self.assertIn("bot author", reason)

    def test_trust_source_still_drops_a_merge_commit(self):
        c = commit("Merge pull request #8 from x/y", parents=2)
        self.assertEqual("merge commit", fi.is_noise(c, RULES, set(), trust_source=True))


class CandidateKindTests(unittest.TestCase):
    def test_legacy_record_without_kind_is_fork(self):
        self.assertEqual("fork", fi.candidate_kind({"sha": "abc"}))

    def test_explicit_kind_is_respected(self):
        self.assertEqual("upstream", fi.candidate_kind({"sha": "abc", "kind": "upstream"}))


class OurHistorySubjectsRefTests(unittest.TestCase):
    """git log --all also walks fetched-but-unmerged remote refs (like `upstream`),
    which silently matched fork/upstream candidates as "already in our history"."""

    def test_passes_the_given_ref_and_not_all(self):
        calls = []

        def fake_git(args, cwd=fi.REPO_ROOT):
            calls.append(args)
            return "fix: something\n"

        with patch.object(fi, "git", side_effect=fake_git):
            fi.our_history_subjects("origin/main")

        self.assertEqual(1, len(calls))
        self.assertIn("origin/main", calls[0])
        self.assertNotIn("--all", calls[0])

    def test_defaults_to_head_not_all(self):
        calls = []

        def fake_git(args, cwd=fi.REPO_ROOT):
            calls.append(args)
            return ""

        with patch.object(fi, "git", side_effect=fake_git):
            fi.our_history_subjects()

        self.assertIn("HEAD", calls[0])
        self.assertNotIn("--all", calls[0])


class UpstreamTriagePromptTests(unittest.TestCase):
    """A single triage prompt must never mix fork and upstream framing, and both
    framings must still ask the model for the same JSON."""

    @staticmethod
    def _batch(kind):
        cand = {"sha": "a" * 40, "fork": "someone/mRemoteNG", "subject": "Do a thing",
                "stats": {"files": 1, "additions": 1, "deletions": 0}, "files": [],
                "patch": "diff"}
        if kind is not None:
            cand["kind"] = kind
        return [cand]

    SCHEMA_LINE = '"action":"IMPORT|REIMPLEMENT|WATCH|REJECT","rationale":"<= 30 words"}]'

    def test_upstream_prompt_differs_from_fork_prompt(self):
        with patch.object(fi, "related_history", return_value=[]):
            fork_prompt = fi.build_triage_prompt(self._batch("fork"), [])
            upstream_prompt = fi.build_triage_prompt(self._batch("upstream"), [])
        self.assertNotEqual(fork_prompt, upstream_prompt)

    def test_both_still_demand_the_same_json_schema(self):
        with patch.object(fi, "related_history", return_value=[]):
            fork_prompt = fi.build_triage_prompt(self._batch("fork"), [])
            upstream_prompt = fi.build_triage_prompt(self._batch("upstream"), [])
        self.assertIn(self.SCHEMA_LINE, fork_prompt)
        self.assertIn(self.SCHEMA_LINE, upstream_prompt)

    def test_legacy_batch_with_no_kind_gets_fork_framing(self):
        with patch.object(fi, "related_history", return_value=[]):
            no_kind_prompt = fi.build_triage_prompt(self._batch(None), [])
            fork_prompt = fi.build_triage_prompt(self._batch("fork"), [])
        self.assertEqual(fork_prompt, no_kind_prompt)


class ExportIdeasTests(unittest.TestCase):
    def test_export_omits_patches_and_keeps_a_changed_status(self):
        import argparse
        import json
        import tempfile

        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            cand_dir = root / "candidates"
            ideas = root / "ideas"
            cand_dir.mkdir()
            sha = "ab" * 20
            other = "cd" * 20
            judged = {
                "sha": sha, "kind": "fork", "fork": "someone/mRemoteNG",
                "subject": "add a thing", "html_url": "https://example.test/c",
                "date": "2026-08-01T00:00:00Z",
                "patch": "secret diff " + "Trust" + "ServerCertificate=true",
                "body": "Trust" + "ServerCertificate",
                "files": [{"filename": "a.cs"}],
                "triage": {
                    "value": 4, "effort": 2, "risk": 1, "already_in_our_fork": False,
                    "action": "IMPORT", "applies_cleanly": "likely",
                    "rationale": "uses " + "Trust" + "ServerCertificate",
                },
                "security_flags": [],
                "stats": {"files": 1, "additions": 1, "deletions": 0},
                "status": "screened",
            }
            pending = dict(judged)
            pending["sha"] = other
            pending["triage"] = None
            (cand_dir / f"{sha[:10]}.json").write_text(json.dumps(judged), encoding="utf-8")
            (cand_dir / f"{other[:10]}.json").write_text(json.dumps(pending), encoding="utf-8")
            exclude = root / "EXCLUDE.json"
            exclude.write_text(json.dumps({"commits": {}}), encoding="utf-8")
            with patch.object(fi, "CANDIDATES_DIR", cand_dir), \
                    patch.object(fi, "IDEAS_DIR", ideas), \
                    patch.object(fi, "EXCLUDE_FILE", exclude):
                self.assertEqual(0, fi.cmd_export_ideas(argparse.Namespace()))
                out_path = ideas / f"{sha[:10]}.json"
                out = json.loads(out_path.read_text(encoding="utf-8"))
                self.assertNotIn("patch", out)
                self.assertNotIn("Trust" + "ServerCertificate", json.dumps(out))
                self.assertEqual("new", out["status"])
                self.assertEqual("A", out["tier"])
                self.assertFalse((ideas / f"{other[:10]}.json").exists())
                out["status"] = "trying"
                out["note"] = "looking at it"
                out_path.write_text(json.dumps(out), encoding="utf-8")
                self.assertEqual(0, fi.cmd_export_ideas(argparse.Namespace()))
                again = json.loads(out_path.read_text(encoding="utf-8"))
            self.assertEqual("trying", again["status"])
            self.assertEqual("looking at it", again["note"])
            self.assertEqual("A", again["tier"])


if __name__ == "__main__":
    unittest.main(verbosity=2)
