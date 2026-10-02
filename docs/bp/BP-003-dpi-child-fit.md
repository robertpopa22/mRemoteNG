# BP-003 — A DPI bounce can leave a child at the old size

**Does:** record that a per-monitor DPI change can leave a child control at the previous device DPI after the form has settled, and that moving the child does not refit it.

**Does not:** set DPI policy, describe a machine, or cover mixed fonts on the main window. Those fonts can still disagree after the same bounce, and that is a separate symptom.

## Incident

A confirmation checkbox was logged at device DPI 192 with an 8.25pt font realized as 30px inside a 28×27 client, after the form had bounced 96 → 192 → 96 and settled at 96. The font was taller than the client. Themed paint used the glyph rectangle captured in the constructor, so the bottom of the box was clipped. Layout only changed the control's position. The panel was tall enough, and the test that only checked the checkbox sat inside the panel stayed green. A later dialog in the same session settled at 96 DPI with a 15px font.

The empty-text checkbox at 192 DPI is about 28×27 because the themed glyph is 11px at 96 DPI. AutoSize had not taken the label width after the bounce. The checkbox sets its own font, so it does not follow the form. Assigning the same point size again does not rebuild the pixel font. A height taken from the other monitor is the wrong assertion on this monitor: at 96 DPI the fitted client is shorter than 27px, and the font is no longer 30px.

## Rule

When a child keeps its own font, size the em in pixels from the form's device DPI and give the client an explicit size that fits the font and the label. Paint the glyph inside the current client. A point-size font keeps the pixel height it was realized at. A test that only checks the child is inside its parent does not catch a clip inside the child.
