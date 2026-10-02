# BP-003 — A DPI bounce can leave a child at the old size

**Version:** 2 · **Updated:** 2026-10-02

**Does:** record that a per-monitor DPI change can leave a child at the size it was built with after the form has settled. Moving the child does not refit it. A tool strip can keep the font cached at the first DPI while the dock panel follows the window.

**Does not:** set DPI policy or describe a machine. Version 1 said the main-window fonts were a separate, unfixed symptom. That limit is withdrawn. The checkbox incident below is unchanged.

## Incident

A confirmation checkbox was logged at device DPI 192 with an 8.25pt font realized as 30px inside a 28×27 client, after the form had bounced 96 → 192 → 96 and settled at 96. The font was taller than the client. Themed paint used the glyph rectangle captured in the constructor, so the bottom of the box was clipped. Layout only changed the control's position. The panel was tall enough, and the test that only checked the checkbox sat inside the panel stayed green. A later dialog in the same session settled at 96 DPI with a 15px font.

The empty-text checkbox at 192 DPI is about 28×27 because the themed glyph is 11px at 96 DPI. AutoSize had not taken the label width after the bounce. The checkbox sets its own font, so it does not follow the form. Assigning the same point size again does not rebuild the pixel font. A height taken from the other monitor is the wrong assertion on this monitor: at 96 DPI the fitted client is shorter than 27px, and the font is no longer 30px.

The same session's main window, on the way from 96 to 192, kept the menu at 4.5pt and 16px while the dock panel became 8.25pt and 30px. The menu font is taken once, at the DPI the strip is created on. On .NET 10 the strip is told the old and new DPI are equal, so it does not take a new font. `SizeInPoints` on a font that is not in points asks the screen DC, so a 16px line is logged as 4.5pt when that DC is twice the window DPI. The pixel height at the window DPI is the measurement that separates those two.

## Rule

When a child keeps its own font, size the em in pixels from the form's device DPI and give the client an explicit size that fits the font and the label. Paint the glyph inside the current client. A point-size font keeps the pixel height it was realized at. A test that only checks the child is inside its parent does not catch a clip inside the child.

Chrome that does not follow the window is rebuilt so its line spacing at the window DPI matches the dock panel, which does follow. The log records the font unit, the raw size, and that line spacing. `SizeInPoints` alone does not.
