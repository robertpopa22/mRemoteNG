# P1-P5 Execution Snapshot

Date: 2026-02-08
Repo: mRemoteNG/mRemoteNG
Cutoff for stale triage: 2025-02-08

## Summary

- P1 duplicate open: 6
- P2 need2check open: 207
- P2 need2check stale (>365d): 0
- P3 in progress open: 30
- P3 in progress stale (>365d): 0
- P3 in development open: 35
- P3 in development stale (>365d): 0
- P4 version debt open: 328
- P5 release candidates (top): 30

## Fork-side P5 Progress (post-snapshot)

- #2735 SmartSize focus resilience hardening implemented in `mRemoteNG/UI/Forms/frmMain.cs`:
  - queue active-session refocus on `WM_ACTIVATEAPP` when `DisableRefocus=false`
  - resilient active-tab lookup fallback in `ActivateConnection()`
  - local validation:
    - `Release|x64` solution build passed
    - targeted regressions passed (`ConnectionsService`, `XmlConnectionsLoaderTests`, `PuttySessionNameDecoderTests`)
- #847 RDP fullscreen/redirect-keys guardrail implemented:
  - exposed `RedirectKeysEnabled` on `RdpProtocol`
  - fullscreen context-menu action now locks only when redirect-keys mode is active and session is already fullscreen
  - prevents accidental fullscreen exit in this state
- #1650 fullscreen-exit refocus hardening implemented:
  - `RdpProtocol` now restores/activates main form and refocuses active session after `OnLeaveFullScreenMode`
  - reduces cases where mRemoteNG falls to background after leaving fullscreen via RDP control bar

## P1 - Duplicate (open)

- #520 - Alt Tab on Windows 10 (https://github.com/mRemoteNG/mRemoteNG/issues/520)
- #1684 - Panel should close when last containing connection/tab closes (https://github.com/mRemoteNG/mRemoteNG/issues/1684)
- #1837 - Can't find Use VM ID property? (https://github.com/mRemoteNG/mRemoteNG/issues/1837)
- #1874 - Use stored credentials in hostname for connexion using HTTP/HTTPs protocol (https://github.com/mRemoteNG/mRemoteNG/issues/1874)
- #2537 - log4net.dll old version (https://github.com/mRemoteNG/mRemoteNG/issues/2537)
- #3051 - Secret exposed in mRemoteNG/UI/Forms/OptionsPages/SecurityPage.Designer.cs - mRemoteNG (https://github.com/mRemoteNG/mRemoteNG/issues/3051)

## P2 - Need 2 check (Batch 1)


## P3 - Stale in-progress labels

### In progress stale

### In development stale

## P4 - Version label debt

- 1.77.3: 97
- 1.77.2: 86
- 1.78.*: 84
- 1.8 (Fenix): 35
- 1.77.4: 24
- 1.76.20: 6

## P5 - Release stabilization candidates

- #2785 - PuTTY saved sessions with CJK characters fail to display and connect | updated 2026-02-08 | https://github.com/mRemoteNG/mRemoteNG/issues/2785
- #822 - mRemoteNG doesn't start if keyfile not available | updated 2026-02-08 | https://github.com/mRemoteNG/mRemoteNG/issues/822
- #3069 - Exeption occurred on closing panel with connections inside | updated 2026-02-07 | https://github.com/mRemoteNG/mRemoteNG/issues/3069
- #2972 - 1password integration doesn't work from credentials inside options | updated 2026-02-07 | https://github.com/mRemoteNG/mRemoteNG/issues/2972
- #3092 - 1Password integration doesn't fetch username and password for RDP connection | updated 2026-02-07 | https://github.com/mRemoteNG/mRemoteNG/issues/3092
- #3044 - With external tool, if password contains a comma, the comma acts as a divider and the variable get split - 1.78.2.3228 | updated 2026-02-07 | https://github.com/mRemoteNG/mRemoteNG/issues/3044
- #1969 - The startup connection file could not be loaded. connectionFilePath cannot be null or empty | updated 2026-02-07 | https://github.com/mRemoteNG/mRemoteNG/issues/1969
- #850 - Config panel column width NOT remembered between minimize/maximize action | updated 2026-02-07 | https://github.com/mRemoteNG/mRemoteNG/issues/850
- #2721 - Site Download Page Error | updated 2026-02-07 | https://github.com/mRemoteNG/mRemoteNG/issues/2721
- #2735 - Lost focus on RDP's in SmartSize mode. | updated 2026-02-07 | https://github.com/mRemoteNG/mRemoteNG/issues/2735
- #2420 - Public Disclosure of issue 726 | updated 2026-02-07 | https://github.com/mRemoteNG/mRemoteNG/issues/2420
- #2290 - mysql db problem - new item creation impossible | updated 2026-02-07 | https://github.com/mRemoteNG/mRemoteNG/issues/2290
- #1883 - Error when saving to SQL | updated 2026-02-07 | https://github.com/mRemoteNG/mRemoteNG/issues/1883
- #1916 - MySQL error Missing the DataColumn 'DisableCursorBlinking' | updated 2026-02-07 | https://github.com/mRemoteNG/mRemoteNG/issues/1916
- #847 - RDP fullscreen and redirect keys | updated 2026-02-07 | https://github.com/mRemoteNG/mRemoteNG/issues/847
- #1650 - Application automatically goes to background in various scenarios | updated 2026-02-07 | https://github.com/mRemoteNG/mRemoteNG/issues/1650
- #1649 - When a master password is defined, mRemoteNG may be lock after a while or when minimized | updated 2026-02-07 | https://github.com/mRemoteNG/mRemoteNG/issues/1649
- #290 - Focus issue in PuTTYNG session after switching tabs | updated 2026-02-07 | https://github.com/mRemoteNG/mRemoteNG/issues/290
- #274 - Cannot connect to TightVNC Server When Unauthenticated  | updated 2026-02-07 | https://github.com/mRemoteNG/mRemoteNG/issues/274
- #811 - Error during startup System.Xml.XmlException | updated 2026-02-07 | https://github.com/mRemoteNG/mRemoteNG/issues/811
- #1640 - Password protection | updated 2026-02-07 | https://github.com/mRemoteNG/mRemoteNG/issues/1640
- #251 - Add global SmartSize option for RDP/VNC | updated 2026-02-07 | https://github.com/mRemoteNG/mRemoteNG/issues/251
- #2653 - RDP clipboard not working with Server 2025 | updated 2026-02-07 | https://github.com/mRemoteNG/mRemoteNG/issues/2653
- #3005 - SQL Server Connection - mRemoteNG 1.78.2 NB 3228 | updated 2026-02-07 | https://github.com/mRemoteNG/mRemoteNG/issues/3005
- #2651 - [Feature Request] Improved SSH Reconnect Experience | updated 2026-02-07 | https://github.com/mRemoteNG/mRemoteNG/issues/2651
- #2987 - Where is user.config in mR 1.78.2 NB 3228? | updated 2026-02-07 | https://github.com/mRemoteNG/mRemoteNG/issues/2987
- #1634 - Expose protocol as variable for external tools | updated 2026-02-07 | https://github.com/mRemoteNG/mRemoteNG/issues/1634
- #192 - RDP using multiple monitors | updated 2026-02-07 | https://github.com/mRemoteNG/mRemoteNG/issues/192
- #2510 - COM object that has been separated from its underlying RCW cannot be used. | updated 2026-02-07 | https://github.com/mRemoteNG/mRemoteNG/issues/2510
- #2509 - Opening Command pass password option | updated 2026-02-07 | https://github.com/mRemoteNG/mRemoteNG/issues/2509

## Next Moves

1. P1: attempt duplicate closure/cross-linking where rights allow.
2. P2: run triage comments on batch 1 and classify fixed/duplicate/cannot-repro.
3. P3: relabel stale in-progress issues to Need 2 check where applicable.
4. P4: prepare label policy note for modern version taxonomy.
5. P5: continue with next runtime candidate (`#847` RDP fullscreen/redirect-keys behavior).
