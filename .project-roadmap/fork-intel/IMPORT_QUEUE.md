# Import Queue

Generated 2026-07-25 from the fork radar. **Nothing is applied automatically.**

Both mRemoteNG and its forks are GPL-2.0, so importing is licence-compatible. `git cherry-pick` preserves the original author and `-x` records the source commit; add a `Ported-from:` trailer with the upstream URL so the origin stays visible.

After a decision, record it so future runs stop proposing it:

```bash
python .project-roadmap/fork-intel/fork_intel.py mark --sha <sha> --decision imported|rejected|deferred --note "why"
```

## Tier B - worth porting by hand

### `3f94a2c239` Dark mode: follow the OS, honor the theming setting, dark title bars - needs manual review

Follow-OS dark mode + DWM dark title bars addresses open #47. Clean idea, but flips ThemingActive default and our ThemeManager/settings diverged; re-derive carefully.  
Source: https://github.com/mRemoteNG/mRemoteNG/commit/3f94a2c23980a384cbf15386ae7ffc506a92e6e5

Counter-opinions: codex **REJECT** / gemini **NEEDS_HUMAN**
- codex: REJECT - OS matching is valuable, but this untested patch conflicts with live-switch and high-contrast theming, assumes restart-only behavior, and requires a scoped reimplementation.
- gemini: NEEDS_HUMAN - Valuable dark mode UX improvements matching modern Windows settings, but requires careful refactoring of settings and ThemeManager initialization to prevent regressions.

Port by hand - the patch will not apply cleanly over our tree. Read the source diff, reimplement, and credit the original author in the commit body.

### `932e6f6116` Enhance connection handling and UI features - needs manual review

Mixed bag: new inheritance props (ExternalAddressProvider, RDP StartProgram, gateway token), notification detail, plus personal junk (.vscode, WorldOfFanXP.xml). Partly overlaps our upstream ports; cherry-pick only if users ask.  
Source: https://github.com/mRemoteNG/mRemoteNG/commit/932e6f611674e6227db18d977f67a1b577af25a2

Counter-opinions: codex **REJECT** / gemini **REJECT**
- codex: REJECT - A 732-line mixed, untested commit also bypasses notification filters, leaks writer subscriptions, duplicates shipped UI/retry features, adds untranslated labels, and uses noncanonical tooling.
- gemini: REJECT - This is a mixed bag of personal settings, French locale scripts, and features already integrated or overlapping with our upstream ports. Not suitable for import.

Port by hand - the patch will not apply cleanly over our tree. Read the source diff, reimplement, and credit the original author in the commit body.

