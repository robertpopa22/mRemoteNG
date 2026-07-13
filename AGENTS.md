# Agent Instructions — mRemoteNG Fork

This file is a discovery bootstrap for Codex, Gemini, Copilot, and other agents that load `AGENTS.md`. It intentionally does not duplicate project rules.

## Required Instruction Chain

Before doing any work in this repository:

1. Apply the system, user, and host-level instructions supplied to the agent.
2. Read the [parent ecosystem canon](../CLAUDE.md).
3. Read the complete [mRemoteNG project canon](CLAUDE.md).
4. Follow `CLAUDE.md` for repository scope, workflow, build, test, Git, and safety rules.

System and user instructions remain highest priority. Among repository documents, the local `CLAUDE.md` is more specific than the parent. Update `CLAUDE.md`, not this bootstrap, when project guidance changes.

## Skills and Workflows

- The repository currently contains no native `SKILL.md` package.
- `.claude/commands/*.md` files are opt-in Claude Code slash-command runbooks, not general agent skills.
- Host-level skills are optional execution aids; they do not override the canonical instruction chain above.
