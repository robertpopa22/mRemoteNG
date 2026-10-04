# BP-008 — An issue we opened is still in the queue

**Version:** 1 · **Updated:** 2026-10-03

**Does:** record that a crash report filed under our own account was left unprocessed because the status queue required an external comment.

**Does not:** restate the queue. The procedure is `.claude/commands/mremoteng-fix-repo.md`. The predicate is `iis_is_waiting_for_us`.

## Incident

On 2026-10-03 the app opened a crash report under the maintainer account. The issue had no comments. The status command kept only open issues with an unread external comment, so the report never entered the work queue. It stayed open until it was noticed by hand.

## Rule

Zero comments does not remove an open issue from the queue. The author being us does not remove it either. A disposition such as `wontfix` is what keeps an announcement we already settled out of the queue.
