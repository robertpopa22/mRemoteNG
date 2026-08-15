#!/usr/bin/env python3
"""Sync the live figures in README.md so they cannot go stale.

The README carried a "Quality Gate passed / 80.7% coverage" claim for five months after it stopped
being true, and a test count three hundred behind reality. Numbers that are maintained by hand drift;
this makes them mechanical.

Run at the end of every /mremoteng-fix-repo session:

    python scripts/sync-readme-metrics.py --tests 6467          # figures from this session's run
    python scripts/sync-readme-metrics.py --tests 6467 --check  # verify only, non-zero if stale

Issue counts come from the local issue DB. The SonarCloud state is deliberately NOT auto-written:
its wording carries judgement about which findings matter, and a script rewriting that sentence
would flatten exactly the honesty the README is trying to keep. `--check` warns when the gate state
disagrees with what the README says, so a human updates the prose.
"""
import argparse
import glob
import json
import os
import re
import sys
import urllib.request

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
README = os.path.join(ROOT, "README.md")
SONAR_GATE = ("https://sonarcloud.io/api/qualitygates/project_status"
              "?projectKey=robertpopa22_mRemoteNG")


def fork_issue_counts():
    """(external_total, closed, open) for issues opened by someone other than the maintainer.

    Queried from GitHub, not from the local issue DB: the DB only holds issues the orchestrator has
    synced, so it undercounts (74 records against 89 real reports at the time of writing). A metric
    script that reports a smaller number than reality is worse than no script.
    """
    try:
        import subprocess
        result = subprocess.run(
            ["gh", "issue", "list", "--repo", "robertpopa22/mRemoteNG",
             "--state", "all", "--limit", "500", "--json", "state,author"],
            capture_output=True, text=True, encoding="utf-8", check=True)
        issues = [i for i in json.loads(result.stdout)
                  if i["author"]["login"] != "robertpopa22"]
    except Exception:
        return None, None, None
    closed = sum(1 for i in issues if i["state"] == "CLOSED")
    return len(issues), closed, len(issues) - closed


def sonar_gate_status():
    try:
        with urllib.request.urlopen(SONAR_GATE, timeout=15) as response:
            return json.load(response)["projectStatus"]["status"]
    except Exception as exc:                                    # network optional, never fatal
        return f"UNKNOWN ({exc.__class__.__name__})"


def apply(text, pattern, replacement):
    new, count = re.subn(pattern, replacement, text)
    return new, count


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--tests", type=int, required=True, help="passing test count from this run")
    parser.add_argument("--check", action="store_true", help="report drift, change nothing")
    args = parser.parse_args()

    with open(README, encoding="utf-8") as fh:
        original = fh.read()
    text = original
    tests = f"{args.tests:,}"

    edits = [
        (r"\*\*Quality:\*\* [\d,]+ automated tests", f"**Quality:** {tests} automated tests"),
        (r"\*\*[\d,]+ tests\*\*, 9 groups", f"**{tests} tests**, 9 groups"),
        (r"[\d,]+\+ automated tests", f"{args.tests // 100 * 100:,}+ automated tests"),
        (r"an automated test suite \([\d,]+\+ tests\)", f"an automated test suite ({args.tests // 100 * 100:,}+ tests)"),
    ]
    for pattern, replacement in edits:
        text, _ = apply(text, pattern, replacement)

    external, closed, open_count = fork_issue_counts()
    if external:
        text, _ = apply(
            text,
            r"\*\*\d+ issues have been opened by external reporters and \d+ are closed\*\*; "
            r"\d+ issues are open",
            f"**{external} issues have been opened by external reporters and {closed} are "
            f"closed**; {open_count} issues are open")

    gate = sonar_gate_status()
    # Only the *current-state* claim matters. Release History legitimately records that a past PR
    # passed its gate; matching on that produced a false alarm on the first run. The phrasing is
    # matched on both sides so the check keeps working when the prose is rewritten, which is what
    # broke it the second time.
    claims_red = "Quality Gate is currently RED" in text or "security rating is currently B" in text
    claims_pass = ("Quality Gate green" in text
                   or "SonarCloud Quality Gate passed (A reliability" in text
                   or "Quality Gate PASSED —" in text)
    warnings = []
    if gate == "OK" and claims_red:
        warnings.append("SonarCloud gate is now OK but README still describes it as red — "
                        "update the prose in the quality line and §6.4 by hand.")
    if gate == "ERROR" and claims_pass:
        warnings.append("SonarCloud gate is RED but README claims it passes — fix the prose.")

    if args.check:
        drift = text != original
        for warning in warnings:
            print(f"WARN: {warning}")
        print("README figures are stale" if drift else "README figures are current")
        return 1 if (drift or warnings) else 0

    if text != original:
        with open(README, "w", encoding="utf-8", newline="\n") as fh:
            fh.write(text)
        print(f"README updated: tests={tests}, fork issues closed={closed}/open={open_count}")
    else:
        print("README already current")
    for warning in warnings:
        print(f"WARN: {warning}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
