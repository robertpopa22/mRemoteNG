"""Model selection: nothing pinned by default; environment and --*-model overrides apply."""
import importlib
import os
import sys

sys.path.insert(0, os.path.dirname(__file__))

import iis_orchestrator as iis  # noqa: E402

ENV = ("IIS_CODEX_MODEL", "IIS_CODEX_MODEL_FALLBACK", "IIS_GEMINI_MODEL", "IIS_GEMINI_MODEL_FLASH",
       "IIS_CLAUDE_MODEL_FAST", "IIS_CLAUDE_MODEL_DEEP")


def _reload(monkeypatch, **env):
    for name in ENV:
        monkeypatch.delenv(name, raising=False)
    for name, value in env.items():
        monkeypatch.setenv(name, value)
    return importlib.reload(iis)


def test_defaults_pin_no_model(monkeypatch):
    mod = _reload(monkeypatch)
    assert mod.CODEX_MODEL is None and mod.CODEX_MODEL_FALLBACK is None
    assert mod.GEMINI_MODEL is None and mod.gemini_model_for("triage") is None
    assert (mod.CLAUDE_MODEL_SONNET, mod.CLAUDE_MODEL_OPUS) == ("sonnet", "opus")


def test_environment_overrides_and_flash_fallback(monkeypatch):
    mod = _reload(monkeypatch, IIS_CODEX_MODEL="primary-x", IIS_CODEX_MODEL_FALLBACK="deep-x",
                  IIS_GEMINI_MODEL="pro-x")
    assert (mod.CODEX_MODEL, mod.CODEX_MODEL_FALLBACK) == ("primary-x", "deep-x")
    assert mod.gemini_model_for("triage") == "pro-x"
    assert mod.gemini_model_for("implement") == "pro-x"


def test_codex_command_omits_model_unless_configured(monkeypatch, tmp_path):
    seen = []

    def fake_run(cmd, timeout=None, cwd=None, stdin_path=None, env=None):
        seen.append(cmd)
        return 1, "", "simulated failure"

    for model in (None, "chosen-x"):
        mod = _reload(monkeypatch, **({"IIS_CODEX_MODEL": model} if model else {}))
        monkeypatch.setattr(mod, "_run_with_timeout", fake_run)
        monkeypatch.setattr(mod, "kill_stale_processes", lambda: None)
        monkeypatch.setattr(mod, "SCRIPTS_DIR", tmp_path)
        mod.codex_run("short prompt", retries=1)
    without, with_model = seen
    assert "-m" not in without
    assert with_model[with_model.index("-m") + 1] == "chosen-x"
