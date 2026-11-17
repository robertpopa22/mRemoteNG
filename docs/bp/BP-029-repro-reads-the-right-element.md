# BP-029 — Check which element a failing repro measured

**Version:** 1 · **Updated:** 2026-11-17

**Does:** record how a UI scenario can fail for the wrong reason, and the check that separates a real repro from a scenario that measured the wrong control.

**Does not:** define the lab procedure ([docs/LAB-GUEST.md](../LAB-GUEST.md)) or the reply wording ([CHARTER.md](../../CHARTER.md) D7).

## Incident

For #253 a lab scenario started the app with the External Tools toolbar saved on the main menu's row and compared the toolbar's top edge with the menu's. It failed on the build before the fix, which looked like a reproduction. It failed again on the build with the fix. The UIA tree in the artifacts showed why: the Connections panel has its own menu strip, and both strips report the AutomationId `msMain`. The scenario had compared the toolbar with the panel's strip. The real main menu had been on the toolbar's row after the fix all along. Matched by its accessible name, the scenario failed before the fix and passed after it.

## Rule

A failing scenario is a repro only after you have checked, in the artifacts, that the element it measured is the one the report is about. AutomationId comes from the field name, and two forms can use the same field name. When a window hosts panels with their own controls, match on AutomationId plus a second property, such as the accessible name or the parent. Run the same scenario before and after the change. A result that is the same both ways says something about the scenario, not about the change.
