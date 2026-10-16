# BP-019 — A retest request is a promise of a build; check that the build exists

**Version:** 1 · **Updated:** 2026-10-16

**Does:** record that five open issues waited eight days for "the next nightly" while no nightly was being produced, and that the fork's release page must name the build a reporter is asked to test.

**Does not:** describe how packages are published (`scripts/Publish-Release.ps1` and the plan in `docs/plans/UPDATE-FULL-MINI-2026-10-15.md` hold that).

## Incident

Between 2026-10-08 and 2026-10-15 GitHub Actions on the fork produced no runs (the nightly workflow never started after push `c0aba2cc7`). Replies on #198, #215, #216 and #217 asked the reporters to retest "on the next nightly". The release page kept saying "Nightly Build — 20261008". Nobody could retest, nobody said so, and the daily run treated the silence as the reporters' choice. The first locally published FULL came on 2026-10-15 under a new asset name that no earlier reply mentioned.

## Rule

Before a reply asks for a retest, the run confirms that a build newer than the reporter's exists on the release page, by name, and the reply names that file. If no such build exists, the reply says so and gives the date one will. The status step of the daily run reads the release page, not only `gh run list`: a paused CI and a release whose title still carries the old date are a finding, reported to the maintainer in the same run. A `testing` status in the issue database with no newer build on the release is a contradiction, not a wait.
