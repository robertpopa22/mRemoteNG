"""Run a scoped CLI job and verify the execution evidence before using its answer."""

import json
import os
import queue
import re
import shutil
import subprocess
import tempfile
import threading
from dataclasses import dataclass
from datetime import datetime, timedelta, timezone
from pathlib import Path
from uuid import UUID


@dataclass
class AgentResult:
    state: str
    model: str | None = None
    session: str | None = None
    text: str = ""
    usage: dict | None = None


def read_git(root, *args):
    env = {key: value for key, value in os.environ.items()
           if key not in ("GIT_DIR", "GIT_COMMON_DIR", "GIT_WORK_TREE", "GIT_INDEX_FILE")}
    result = subprocess.run(
        ["git", "--no-optional-locks", *args], cwd=root, capture_output=True,
        encoding="utf-8", errors="replace", timeout=30, check=True, env=env,
    )
    return result.stdout


def detached_base(root, *, clean=True):
    """Require a linked worktree, detached at its own root and free of WIP."""
    if not (root / ".git").is_file() or Path(read_git(root, "rev-parse", "--show-toplevel").strip()).resolve() != root:
        raise ValueError("a separate linked worktree is required")
    if read_git(root, "rev-parse", "--abbrev-ref", "HEAD").strip() != "HEAD":
        raise ValueError("the worktree must use detached HEAD")
    # Agent runs must never pick up the operator's ignored delivery hook.
    if (root / "post-build-local.ps1").exists():
        raise ValueError("local post-build hook found in candidate worktree")
    if clean and read_git(root, "status", "--porcelain", "--untracked-files=all", "--ignored=matching"):
        raise ValueError("the worktree already has changes")
    return read_git(root, "rev-parse", "HEAD").strip()


def _command(provider, model, effort, prompt_file, write):
    executable = shutil.which(provider)
    if not executable:
        raise ValueError("CLI is unavailable")
    if write and provider != "codex":
        raise ValueError("isolated implementation requires the Codex workspace sandbox")
    if provider == "claude":
        tools = "Read,Glob,Grep"
        return [
            executable, "-p", "--output-format", "stream-json", "--verbose",
            "--model", model, "--effort", effort, "--permission-mode", "dontAsk",
            "--strict-mcp-config", "--mcp-config", '{"mcpServers":{}}',
            "--tools", tools, "--allowedTools", tools,
        ]
    if provider == "codex":
        return [
            executable, "-a", "never", "exec", "--json", "--sandbox",
            "workspace-write" if write else "read-only",
            "--ignore-user-config", "-c", "mcp_servers={}",
            "-c", "sandbox_workspace_write.network_access=false", "-m", model,
            "-c", "model_reasoning_effort=" + json.dumps(effort), "-",
        ]
    if provider == "grok":
        return [
            executable, "--no-auto-update", "--prompt-file", str(prompt_file),
            "--output-format", "streaming-json", "--model", model, "--effort", effort,
            "--no-subagents", "--permission-mode", "dontAsk", "--tools", "read,grep,glob",
        ]
    raise ValueError("unsupported provider")


def _stream(process, idle_seconds, on_event):
    events = queue.Queue()

    def read_lines():
        try:
            for line in process.stdout:
                try:
                    event = json.loads(line)
                except ValueError:
                    continue
                if isinstance(event, dict) and event.get("type") not in (None, "heartbeat", "ping", "keepalive"):
                    events.put(event)
        finally:
            events.put(None)

    reader = threading.Thread(target=read_lines, daemon=True)
    reader.start()
    while True:
        try:
            event = events.get(timeout=idle_seconds)
        except queue.Empty:
            return "stalled"
        if event is None:
            try:
                return "completed" if process.wait(timeout=idle_seconds) == 0 else "failed"
            except subprocess.TimeoutExpired:
                return "stalled"
        on_event(event)


def _codex_context(session):
    """Use only the rollout linked to the thread ID emitted by this process."""
    try:
        if not isinstance(session, str) or str(UUID(session)) != session:
            return None
    except (ValueError, TypeError):
        return None
    home = Path(os.environ.get("CODEX_HOME") or Path.home() / ".codex")
    today = datetime.now(timezone.utc)
    matches = []
    for offset in (-1, 0, 1):
        day = today + timedelta(days=offset)
        directory = home / "sessions" / day.strftime("%Y/%m/%d")
        matches.extend(directory.glob(f"rollout-*-{session}.jsonl"))
    if len(matches) != 1:
        return None
    context = None
    with matches[0].open(encoding="utf-8") as stream:
        for line in stream:
            event = json.loads(line)
            if event.get("type") == "turn_context":
                context = event.get("payload")
    return context


def run_agent(provider, prompt, cwd, model, effort, idle_seconds, *, write=False, progress=None):
    """Use transcript events for progress. Never retry an uncertain dispatch."""
    if (not model or not re.fullmatch(r"[A-Za-z0-9][A-Za-z0-9._:/-]*", model)
            or not effort or not re.fullmatch(r"[a-z][a-z0-9_-]*", effort) or idle_seconds <= 0):
        return AgentResult("unconfigured")
    models, replies = set(), []
    session = None
    usage = None
    finished = False

    def receive(event):
        nonlocal session, usage, finished
        kind = event["type"]
        message = event.get("message")
        if not isinstance(message, dict):
            message = {}
        if kind in ("system", "thread.started", "session.started", "init"):
            session = event.get("thread_id") or event.get("session_id") or session
        if kind in ("assistant", "message", "assistant_message"):
            if message.get("role", event.get("role", "assistant")) == "assistant":
                actual = message.get("model") or event.get("model")
                if isinstance(actual, str):
                    models.add(actual)
                content = message.get("content", event.get("text"))
                if isinstance(content, str):
                    replies.append(content)
                elif isinstance(content, list):
                    replies.extend(part["text"] for part in content
                                   if isinstance(part, dict) and part.get("type") == "text"
                                   and isinstance(part.get("text"), str))
        if kind == "item.completed":
            item = event.get("item") or {}
            if item.get("type") == "agent_message" and isinstance(item.get("text"), str):
                replies.append(item["text"])
        if kind in ("result", "turn.completed"):
            finished = not event.get("is_error") and event.get("subtype", "success") == "success"
            if isinstance(event.get("result"), str):
                replies.append(event["result"])
            elif isinstance(event.get("text"), str):
                replies.append(event["text"])
            reported = event.get("modelUsage")
            if isinstance(reported, dict):
                models.update(reported)
            reported_usage = event.get("usage")
            if isinstance(reported_usage, dict):
                usage = {key: value for key, value in reported_usage.items()
                         if key in ("input_tokens", "output_tokens", "cached_input_tokens")
                         and type(value) is int}
            if type(event.get("total_cost_usd")) in (int, float):
                usage = dict(usage or {}, cost_usd=event["total_cost_usd"])
        if progress:
            progress(session, kind)

    # Prefer the authenticated subscription routes; do not inherit metered API keys.
    env = {key: value for key, value in os.environ.items()
           if key not in ("ANTHROPIC_API_KEY", "OPENAI_API_KEY", "XAI_API_KEY", "CLAUDECODE",
                          "CLAUDE_CODE_ENTRYPOINT", "GH_TOKEN", "GITHUB_TOKEN")}
    try:
        with tempfile.TemporaryDirectory(prefix="mrng-agent-") as directory:
            prompt_file = Path(directory) / "prompt.txt"
            prompt_file.write_text(prompt, encoding="utf-8")
            cmd = _command(provider, model, effort, prompt_file, write)
            with prompt_file.open("rb") as stdin:
                process = subprocess.Popen(
                    cmd, cwd=cwd, env=env, stdin=stdin, stdout=subprocess.PIPE,
                    stderr=subprocess.DEVNULL, encoding="utf-8", errors="replace",
                )
                try:
                    state = _stream(process, idle_seconds, receive)
                finally:
                    if process.poll() is None:
                        process.terminate()
                        try:
                            process.wait(timeout=5)
                        except subprocess.TimeoutExpired:
                            process.kill()
                            process.wait()
                    process.stdout.close()
        if state == "completed" and provider == "codex":
            context = _codex_context(session) or {}
            if context.get("model"):
                models.add(context["model"])
            if context.get("effort") != effort:
                return AgentResult("effort-unverified", session=session, usage=usage)
    except (OSError, ValueError, TypeError, KeyError, subprocess.SubprocessError):
        return AgentResult("unavailable", session=session, usage=usage)

    actual = next(iter(models)) if len(models) == 1 else None
    if state != "completed":
        return AgentResult(state, actual, session, usage=usage)
    if actual != model:
        return AgentResult("model-unverified", actual, session, usage=usage)
    if not finished or not session or not replies or not replies[-1].strip():
        return AgentResult("incomplete", actual, session, usage=usage)
    return AgentResult("completed", actual, session, replies[-1], usage)
