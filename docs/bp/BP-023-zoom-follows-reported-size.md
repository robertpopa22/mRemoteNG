# BP-023 — Scale the picture for the size the session reports

**Version:** 1 · **Updated:** 2026-10-21

**Does:** record why a session smaller than its panel needs the control's zoom rather than SmartSizing, and why the zoom must follow the size the remote desktop reports instead of the size that was requested.

**Does not:** describe the adaptive-resolution controller or its thresholds (`docs/RUNTIME_DIAGNOSTICS.md`, `rdp_resolution`), or the `[rdp-zoom]` log line.

## Incident

On 2026-10-20 a Custom 1288x815 SmartSize connection in a 1610x1019 panel showed the remote desktop unscaled, in a box with a grey border, while SmartSizing read back as on. A lab run on the Windows target confirmed it: a 480x288 session stayed centred at its own size in a 668x500 surface, also after toggling SmartSizing. SmartSizing only scales a session down to its surface. The control's `ZoomLevel` extended property enlarges it, accepts any whole percentage, and is refused (0x80070032) while SmartSizing is on. The adaptive mode, which renders a smaller session and relied on SmartSizing to stretch it back, had the same defect.

The first zoom fix then set the zoom for the size it had just requested. On the road, a step down was requested 14 seconds before the link dropped: the request returned without error, the server never applied it, and the picture was zoomed 125 % over a session still at full size. The output window measured 2012x1273 in a 1610x1019 surface, with scroll bars. A second case: after an automatic reconnection the session came back at full size while the earlier zoom was still on. A step down happens exactly when the link is failing, so its request is the one most likely to be lost.

## Rule

A session smaller than its surface is enlarged with `ZoomLevel` (SmartSizing off), never with SmartSizing. The zoom is decided in one place, from the size the remote desktop reports (`OnRemoteDesktopSizeChange`, or the control's desktop size before the first report), never from a request. Anything that cannot tell the real size yet, such as a reconnection or a refused request, takes the zoom off rather than keeping one computed for another size. Prove it in the lab by measuring the control's output window (`IHWindowClass`) against its surface, not by reading the property back.
