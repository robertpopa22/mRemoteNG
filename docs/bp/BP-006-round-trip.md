# BP-006 — A successful return is not a round trip

**Version:** 1 · **Updated:** 2026-10-02

**Does:** record that a save, export, or schema write can report success while the stored value is unchanged or discarded.

**Does not:** list the current SQL statements (those live in the code and the tests).

## Incident

On 2026-08-16 a SQL save returned without writing the connection Notes, and a change-detection path reported no change when the root name had changed. The unit result was green. A round trip that wrote a value, reloaded it, and compared it is what showed the loss. The same month a schema upgrade statement was invalid on a table that already had rows, which an empty-database check did not see.

## Rule

A test that only checks the return value does not prove persistence. Write a known value, read it back from the store the user uses, and compare. An empty database is not enough for an upgrade that alters a populated table.
