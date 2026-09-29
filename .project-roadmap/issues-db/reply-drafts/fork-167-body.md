This fork uses automated development and verification, directed by a human maintainer. We also use mRemoteNG daily. That gives us useful evidence, but it cannot reproduce every reporter's network, server, locale or display setup.

## What to expect from a reply

- We address your latest result and keep distinct symptoms separate. "Improved, but still clipped" is partial confirmation.
- We name the checks actually run, what appeared in the UI, their limits, and the exact published build to try. A passing test or a commit alone does not prove your problem is fixed or that a download is available.
- Your "still broken" is evidence. We revisit the explanation and attempt a local reproduction before asking you for another test.
- Questions are bounded: only missing details that distinguish the remaining causes. Please remove credentials and identifying server/account details from shared logs; do not upload your connection database.
- A promise we make—local testing, vendor follow-up or human review—remains our action even when we wrote the last comment. We review contributor patches and recent closed-thread feedback too.

## Attempts, follow-up and closure

After two fixes based on an unproven premise, the next step is instrumentation rather than another guessed fix. After three failed rounds, automated attempts stop for human review or new evidence. If a thread needs human attention, say so plainly.

For a testing request with no response, we send at most one short follow-up after seven days. Silence is not confirmation. We may close a fix shipped in a stable release when our unit, lab-UI and applicable backend checks support it, stating exactly what was and was not verified. An unreproducible report may be closed as not planned for bookkeeping, with an invitation to reopen. We never close over an unanswered report that it is still broken.

Within each triage pass, we aim to acknowledge actionable feedback within two working days. This is a working target, not an automatic response service.

Thank you for reporting, retesting and contributing patches. Those results determine whether the work actually resolves the problem in your setup. Feedback on this process is welcome here.
