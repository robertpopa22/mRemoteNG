# BP-004 — An open issue stays in the queue

**Does:** record that a fix run which only reads unread comments can finish while other open issues have no disposition.

**Does not:** set reply policy (that is charter D7 and [ISSUE-RESPONSE-WORKFLOW.md](../ISSUE-RESPONSE-WORKFLOW.md)), and it does not replace the queue steps in the fix-repo runbook.

## Incident

The fork had eight open issues. The queue kept only records flagged as waiting on us with an unread comment, so a run touched the one open issue with a new reporter comment and a closed issue with new comments. The other open issues, where our earlier message was still the latest word, were not listed. From the outside that looks like a run that ignored them.

## Rule

Every open issue is in the queue. The session report names each one. The outcome on the issue is a fix, or our reply asking for the specific missing detail. If that reply or that named build is already the latest comment, it stands and is not posted again. One follow-up ping is allowed, once, seven days after the ask. A maintainer announcement that is not a defect is listed and does not get a bug reply.
