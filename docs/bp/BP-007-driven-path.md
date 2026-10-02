# BP-007 — A UI claim counts only the path that was driven

**Version:** 1 · **Updated:** 2026-10-02

**Does:** record that an acceptance claim was wrong because the scenario had not driven the path it named.

**Does not:** set the lab procedure (that is the manual and [LAB-GUEST.md](../LAB-GUEST.md)).

## Incident

On 2026-08-16 an adversarial review of the new UI battery found that most of the claimed coverage had not happened: the scenario did not reach the control, or it treated an expected prompt as a product bug, or it reported a pass on a session that had dropped. A later RDP close measurement was not a logoff, because the wait was shorter than the round trip that would have shown the logoff.

## Rule

Name the path that was driven and what was on the screen. A pass on a different path is not evidence for the report. A scenario that does not reach the symptom is a failed reproduction, and the reply says so.
