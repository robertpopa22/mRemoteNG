# BP-017 — A path check that normalises one side is not a path check

**Version:** 1 · **Date:** 2026-10-14

The public v1.84.0 x64 zip, copied into the temporary folder of a user whose profile
has an 8.3 short name, closed at start with an unhandled `FileNotFoundException`
for an assembly that was present in `Assemblies\`. The same files started normally
from the long form of the same folder.

The assembly resolver accepts only files inside the application directory. It
compared `Path.GetFullPath(candidate)` with `AppDomain.CurrentDomain.BaseDirectory`.
On .NET, `GetFullPath` expands short names, so a candidate built from
`C:\Users\ROBERT~1.POP\...` came back as `C:\Users\robert.popa\...`, while the
base directory kept the form the process was launched with. No file in
`Assemblies\` ever matched, and startup died on the first one it needed.

`ProgramRoot.IsUnderDirectory` now passes both sides through `GetFullPath` and adds
the trailing separator itself, so a sibling that shares a prefix still fails. The
containment property is unchanged: traversal is still collapsed before the
comparison, and only files under the application directory load.

The regression tests build the base in its 8.3 form on a volume that generates
short names, as the test machine does. The end-to-end check launched a local
build from a short path: before the fix it closed on the first assembly in
`Assemblies\`; after it, the main window opened.
