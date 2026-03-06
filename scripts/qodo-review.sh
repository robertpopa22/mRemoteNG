#!/bin/bash
# qodo-review.sh — On-demand Qodo code review
#
# Creates a temporary draft PR targeting main so Qodo Code Review triggers.
# Must be run by a human (not a bot) — Qodo ignores bot-created PRs.
#
# Usage:
#   ./qodo-review.sh              # Review last commit on current branch
#   ./qodo-review.sh 5            # Review last 5 commits
#   ./qodo-review.sh 3 main       # Review last 3 commits on main
#   ./qodo-review.sh cleanup      # Close all open review PRs
#
# Prerequisites: gh CLI authenticated (gh auth login), jq installed

set -euo pipefail

REPO="robertpopa22/mRemoteNG"
REVIEW_BRANCH_PREFIX="review/qodo-"

# --- Preflight checks ---
for cmd in gh jq git; do
    if ! command -v "$cmd" &>/dev/null; then
        echo "ERROR: '$cmd' is required but not found in PATH."
        exit 2
    fi
done

# Validate origin matches REPO
ORIGIN_URL=$(git remote get-url origin 2>/dev/null || true)
if ! echo "$ORIGIN_URL" | grep -qi "robertpopa22/mRemoteNG"; then
    echo "ERROR: origin remote ($ORIGIN_URL) does not match $REPO."
    echo "       Run from the correct repo clone."
    exit 2
fi

# --- Cleanup mode ---
if [ "${1:-}" = "cleanup" ]; then
    echo "Closing all open Qodo review PRs..."
    gh pr list --repo "$REPO" --state open --json number,title,headRefName \
        --jq '.[] | select(.title | startswith("Qodo Review"))' |
    while IFS= read -r line; do
        PR_NUM=$(echo "$line" | jq -r '.number')
        BRANCH=$(echo "$line" | jq -r '.headRefName')
        # Only delete branches with the review/qodo- prefix (safety guard)
        if [[ "$BRANCH" != ${REVIEW_BRANCH_PREFIX}* ]]; then
            echo "  SKIP PR #${PR_NUM} — branch '${BRANCH}' is not a review branch"
            continue
        fi
        echo "  Closing PR #${PR_NUM}, deleting ${BRANCH}..."
        gh pr close "$PR_NUM" --repo "$REPO" --comment "Cleanup." 2>/dev/null || true
        git push origin --delete "$BRANCH" 2>/dev/null || true
    done
    echo "Done."
    exit 0
fi

# --- Review mode ---
N="${1:-1}"

# Validate N is a positive integer
if ! [[ "$N" =~ ^[1-9][0-9]*$ ]]; then
    echo "ERROR: N must be a positive integer, got '${N}'."
    exit 2
fi

BRANCH="${2:-$(git rev-parse --abbrev-ref HEAD)}"
TIMESTAMP=$(date +%Y%m%d-%H%M%S)
REVIEW_BRANCH="${REVIEW_BRANCH_PREFIX}${TIMESTAMP}"

# Validate HEAD~N exists
COMMIT_COUNT=$(git rev-list --count HEAD 2>/dev/null || echo 0)
if [ "$N" -ge "$COMMIT_COUNT" ]; then
    echo "ERROR: Requested ${N} commits but branch '${BRANCH}' only has ${COMMIT_COUNT}."
    exit 2
fi

echo "=== Qodo On-Demand Review ==="
echo "Branch: ${BRANCH}"
echo "Commits: last ${N}"
echo ""

# Ensure we're on the right branch
git checkout "$BRANCH" --quiet

HEAD_SHA=$(git rev-parse HEAD)
BASE_SHA=$(git rev-parse "HEAD~${N}")

echo "Range: $(git log --oneline "${BASE_SHA}..${HEAD_SHA}" | head -5)"
echo ""

# Create review branch from the old state, cherry-pick commits on top
git checkout -b "$REVIEW_BRANCH" "$BASE_SHA" --quiet
git cherry-pick "${BASE_SHA}..${HEAD_SHA}" --allow-empty --quiet
git push origin "$REVIEW_BRANCH" --quiet

# Create draft PR targeting main (must target default branch for Qodo)
if [ "$BRANCH" != "main" ]; then
    echo "NOTE: Qodo only reviews PRs targeting 'main'."
    echo "      Creating draft PR against main instead of ${BRANCH}."
fi

PR_URL=$(gh pr create \
    --repo "$REPO" \
    --base "main" \
    --head "$REVIEW_BRANCH" \
    --draft \
    --title "Qodo Review: last ${N} commits on ${BRANCH}" \
    --body "**On-demand Qodo review** of the last ${N} commit(s) on \`${BRANCH}\`.

**Draft PR — do not merge.** Commits are already on \`${BRANCH}\`.
Close this PR after reading the review, or run: \`./qodo-review.sh cleanup\`")

echo "Draft PR created: ${PR_URL}"
echo ""
echo "Qodo will post its review in 1-3 minutes."
echo "After reading, run: ./qodo-review.sh cleanup"

# Return to original branch
git checkout "$BRANCH" --quiet
