"""Isolated MRNGRUN regressions. No native model, GitHub, or product execution."""

import hashlib
import json
import subprocess
import sys
from pathlib import Path
from types import SimpleNamespace

import pytest

import iis_candidate as candidate
import iis_orchestrator as iis
import workspace_agent as agents

sys.path.insert(0, str(Path(__file__).resolve().parents[1] / "fork-intel"))
import fork_intel as radar


def test_preview_reads_queue_including_retry_without_writes(tmp_path, monkeypatch, capsys):
    queue = tmp_path / "upstream"
    queue.mkdir()
    for number, failed in ((1, True), (2, False), (3, True)):
        (queue / f"{number}.json").write_text(json.dumps({
            "number": number, "our_status": "triaged", "impl_failed": failed,
            "priority": "P2-bug",
        }), encoding="utf-8")
    status = tmp_path / "status.json"
    warning_log = tmp_path / "build.log"
    warning_log.write_text("mRemoteNG/X.cs(10,2): warning CS8602: text\n"
                           "mRemoteNG/Y.cs(11,3): warning CA2000: text\n", encoding="utf-8")
    monkeypatch.setattr(iis, "ISSUES_DB_DIR", queue)
    monkeypatch.setattr(iis, "STATUS_FILE", status)
    monkeypatch.setattr(iis, "_count_impl_attempts", lambda number: 99 if number == 3 else 0)
    monkeypatch.setattr(iis, "_count_issue_timeouts", lambda number: 0)
    before = {p: p.read_bytes() for p in tmp_path.rglob("*") if p.is_file()}

    def forbidden(*args, **kwargs):
        pytest.fail("preview crossed a write or execution boundary")

    for name in ("update_issue_json", "iis_sync", "run_build", "setup_logging",
                 "flux_test_hygiene", "chain_implement", "_run"):
        monkeypatch.setattr(iis, name, forbidden)
    monkeypatch.setattr(sys, "argv", ["iis", "status"])
    assert iis.main() == 0
    monkeypatch.setattr(sys, "argv", ["iis", "all", "--dry-run", "--max-issues", "1",
                                      "--max-files", "1", "--warning-log", str(warning_log),
                                      "--warning-revision", "fixture"])
    assert iis.main() == 0
    output = capsys.readouterr().out
    assert "#1: retry" in output and "#3" not in output
    assert "Compiler warning files: 1 of 1" in output
    assert "Analyzer warning files: 1 of 1" in output
    assert {p: p.read_bytes() for p in tmp_path.rglob("*") if p.is_file()} == before

    status.write_text("[", encoding="utf-8")
    monkeypatch.setattr(sys, "argv", ["iis", "status"])
    assert iis.main() == 1 and status.read_text(encoding="utf-8") == "["
    monkeypatch.setattr(sys, "argv", ["iis", "sync", "--dry-run"])
    with pytest.raises(SystemExit, match="2"):
        iis.main()


@pytest.mark.parametrize("code, output", [
    (96, "Total: 12\nPassed: 12\n"),
    (0, "Total: 12\nPassed: 11\nFailed: 1\n"),
    (0, "Total: 12\nPassed: 12\nPHANTOM_GROUPS: 1\n"),
    (0, "Total: 12\nPassed: 12\nCRASHES: yes\n"),
    (0, "Total: 0\nPassed: 0\n"),
    (0, "exit zero with no suite summary"),
])
def test_suite_blocks_failures_and_incomplete_evidence(code, output):
    assert not candidate.suite_complete(code, output, 10)


def test_candidate_verification_is_bound_to_patch_and_completed_suite(tmp_path, monkeypatch):
    root, state_dir, common = (tmp_path / name for name in ("root", "state", "common"))
    for path in (root, state_dir, common):
        path.mkdir()
    (root / "test-config.json").write_text('{"metadata":{"min_total_threshold":10}}', encoding="utf-8")
    monkeypatch.setattr(candidate, "detached_base", lambda path, **kwargs: "base")
    monkeypatch.setattr(candidate, "read_git", lambda path, *args:
                        str(common if args[-1] == "--git-common-dir" else state_dir))
    calls = []

    def fake_agent(*args, **kwargs):
        calls.append("agent")
        kwargs["progress"]("session", "item.completed")
        return agents.AgentResult("completed", "chosen", "session", "candidate ready")

    monkeypatch.setattr(candidate, "run_agent", fake_agent)
    monkeypatch.setattr(candidate, "_candidate_diff", lambda *args: (b"exact patch", 0))
    monkeypatch.setattr(candidate.shutil, "which", lambda name: "fixture")

    def build(cmd, **kwargs):
        calls.append(cmd[-1])
        assert kwargs["cwd"] == root
        return SimpleNamespace(returncode=0, stdout="Total: 12\nPassed: 12\n")

    monkeypatch.setattr(candidate.subprocess, "run", build)
    state, checksum = candidate.prepare(root, "base", {"key": "issue-8"}, "chosen", "low", 10)
    assert state == "awaiting-diff-review"
    assert checksum == hashlib.sha256(b"exact patch").hexdigest()
    with pytest.raises(FileExistsError):
        candidate.prepare(root, "base", {"key": "issue-8"}, "chosen", "low", 10)
    with pytest.raises(ValueError, match="review does not match"):
        candidate.verify(root, "base", "other", "reviewed")
    assert calls == ["agent"]
    assert candidate.verify(root, "base", checksum, "reviewed") == "awaiting-required-verification"
    assert calls == ["agent", "build.ps1", "-NoBuild"]
    assert not (state_dir / "iis-verify.lock").exists()
    monkeypatch.setattr(candidate, "_candidate_diff", lambda *args: (b"changed", 0))
    with pytest.raises(ValueError, match="changed after review"):
        candidate.verify(root, "base", checksum, "reviewed")


def test_transcript_can_progress_past_the_idle_interval(tmp_path, monkeypatch):
    code = (
        "import json,time\n"
        "for _ in range(7):\n"
        " print(json.dumps({'type':'item.completed'}),flush=True);time.sleep(.13)\n"
        "print(json.dumps({'type':'system','session_id':'fixture'}))\n"
        "print(json.dumps({'type':'assistant','message':{'model':'chosen','content':'answer'}}))\n"
        "print(json.dumps({'type':'result','subtype':'success','usage':{'input_tokens':12}}))\n"
    )
    monkeypatch.setattr(agents, "_command", lambda *args: [sys.executable, "-u", "-c", code])
    result = agents.run_agent("claude", "fixture", tmp_path, "chosen", "medium", .6)
    assert (result.state, result.session, result.usage) == ("completed", "fixture", {"input_tokens": 12})
    result = agents.run_agent("claude", "fixture", tmp_path, "different", "medium", .6)
    assert result.state == "model-unverified" and not result.text


def test_radar_pipeline_preserves_historical_votes_and_never_imports(tmp_path, monkeypatch):
    db, report = tmp_path / "db", tmp_path / "reports"
    candidates = db / "candidates"
    for name, value in {"DB_DIR": db, "FORKS_DIR": db / "forks", "CANDIDATES_DIR": candidates,
                        "REPORTS_DIR": report, "IMPORT_QUEUE": tmp_path / "queue.md",
                        "META_FILE": db / "_meta.json"}.items():
        monkeypatch.setattr(radar, name, value)
    monkeypatch.setattr(radar, "git", lambda *args: "base\n")
    monkeypatch.setattr(radar, "detached_base", lambda root: "base")
    for provider in radar.REVIEW_PROVIDERS:
        monkeypatch.setenv(f"FORK_INTEL_{provider.upper()}_MODEL", provider + "-fixture")
        monkeypatch.setenv(f"FORK_INTEL_{provider.upper()}_EFFORT", "medium")
    sha = "a" * 40
    record = {"sha": sha, "fork": "example/fork", "owner": "example", "subject": "Fix a label",
              "patch": "--- mRemoteNG/Foo.cs\n@@ -1 +1 @@\n-old\n+new",
              "patch_complete": True, "status": "screened", "security_flags": [],
              "stats": {"files": 1, "additions": 1, "deletions": 1}}
    path = candidates / (sha[:10] + ".json")
    radar.write_json(path, record)
    calls = []

    def run(provider, prompt, root, model, effort, idle, **kwargs):
        calls.append((provider, prompt))
        if len(calls) == 1:
            answer = [{"sha": sha, "value": 5, "effort": 1, "risk": 1, "action": "IMPORT"}]
        else:
            answer = {"vote": "APPROVE", "aligned_with_direction": True}
        return agents.AgentResult("completed", model, f"job-{len(calls)}", json.dumps(answer))

    monkeypatch.setattr(radar, "run_agent", run)
    assert radar.main(["triage", "--review-root", str(tmp_path), "--limit", "1"]) == 0
    args = ["preapprove", "--review-root", str(tmp_path), "--sha", sha, "--audit-every", "1"]
    assert radar.main(args) == 0
    pre = radar.read_json(path)["preapproval"]
    assert pre["decision"] == "pre-approved"
    assert [provider for provider, prompt in calls] == ["claude", "codex", "grok", "claude"]
    assert "prior automated triage" not in calls[1][1]
    assert radar.main([*args, "--refresh"]) == 0
    assert len(calls) == 4
    assert radar.main(["report"]) == 0
    assert "PRE-APPROVED" in radar.IMPORT_QUEUE.read_text(encoding="utf-8")
    modified = radar.read_json(path)
    modified["patch"] += "\n+changed"
    radar.write_json(path, modified)
    assert radar.main(["report"]) == 0
    report_text = radar.IMPORT_QUEUE.read_text(encoding="utf-8")
    assert "review required" in report_text and sha in report_text
    assert "git cherry-pick" not in report_text


def test_radar_blocks_unknown_alignment_and_partial_github_patch():
    assert radar.consensus_decision([
        {"reviewer": "codex", "vote": "APPROVE", "aligned": True},
        {"reviewer": "grok", "vote": "APPROVE", "aligned": None},
    ], False) == "manual-review"
    sha = "c" * 40
    detail = {"sha": sha, "stats": {"additions": 2, "deletions": 1},
              "files": [{"filename": "mRemoteNG/A.cs", "additions": 2, "deletions": 1,
                         "patch": "@@ -1 +1 @@\n-old\n+new"}]}
    assert not radar.reviewed_patch(detail, sha)["patch_complete"]
