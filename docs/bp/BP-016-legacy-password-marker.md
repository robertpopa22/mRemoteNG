# BP-016 — Successful CBC decryption does not establish a password

**Version:** 1 · **Date:** 2026-10-11

An SQL cancellation test intermittently reached connection deserialization instead
of rejecting authentication. The SQL loader used the absence of a decryption
exception as its password check. Its legacy AES-CBC provider has padding, but no
authentication tag: a wrong key can pass padding and return meaningless text.

A fixed synthetic ciphertext reproduces that case with the real legacy provider.
Before the fix, both cancellation and a correct-password retry skipped the
password request; three unexpected metadata plaintexts were also accepted.
These five regression checks failed before the product change.

The SQL loader now accepts only the exact `ThisIsProtected` or
`ThisIsNotProtected` metadata marker. Rejected plaintext follows the existing
password retry/cancel path. An empty `Protected` field retains its historical
default-key behavior. The authenticator's optional plaintext validator leaves
legacy full-file XML authentication callers unchanged, and clears an earlier
successful key before a new attempt.

This strengthens key validation without changing the SQL cipher, stored data or
database credentials. It does not turn the legacy format into authenticated
encryption. The guard remains in `SqlConnectionsLoader.GetDecryptionKey`; the
metadata writer remains the source of the two supported marker values.

The regression fixture checks that cancellation and invalid markers never read
the connection table. The lab fixture drives the real password dialog and SQL
loader with the collision ciphertext and synthetic in-memory table providers;
it does not claim a live SQL Server round trip.
