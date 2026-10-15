# BP-018 — A rejected COM property is not a failed cast

**Version:** 1 · **Updated:** 2026-10-15

**Does:** record that three attempts to write `BandwidthDetection` assumed the interface cast was failing, when the control rejected the property itself, and how one diagnostic line settled it.

**Does not:** describe the RDP diagnostics fields (`docs/RUNTIME_DIAGNOSTICS.md` holds them).

## Incident

2026-10-14/15: the network auto-detect option was meant to also set `IMsRdpClientAdvancedSettings8.BandwidthDetection`. The write never took effect. Three attempts reworked how the interface was reached (different casts, `AdvancedSettings9` through `IMsRdpClient8`), each assuming the cast was what failed. A diagnostic that logged the step (`cast`, `settings`, `get`, `set`) with the raw HRESULT, run in the lab on an RDC11 control (MSTSC 10.0.26100), showed the cast always succeeded: the get before Connect fails with `0x8345000A`, and the set after `OnConnected` and after `OnLoginComplete` fails with `E_FAIL 0x80004005`. The same runs showed `NetworkConnectionType=7` alone makes the control produce bandwidth samples, and `1` produces none. The property was not settable and not needed.

## Rule

When a COM write has no effect, log the step that failed and the raw HRESULT before changing how the object is reached. Run it in the lab at each phase (before Connect, after connected, after login) and read the result before writing the next fix. A rejected property is removed, not worked around; record it in the diagnostics as `unsupported` so the log says so.
