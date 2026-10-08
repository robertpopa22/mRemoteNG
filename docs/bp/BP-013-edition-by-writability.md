# BP-013 — Where an application keeps its data is decided by what it is, not by what it can write

**Version:** 1 · **Updated:** 2026-10-08

**Does:** record that choosing a settings folder by whether the program folder is writable gives one installed copy two homes, and that a compile-time edition label can disagree with the package that ships it.

**Does not:** describe the marker file or the migration steps (those live in `SettingsFileInfo`, `lab-msi-edition.ps1` and the tests).

## Incident

Issue #214, reported in #208 on 2026-10-06: an MSI install said "Portable Edition" in Help → About. The installer packaged the build compiled as portable. That build kept its settings beside the executable whenever it could write there, so the same installed copy used `%APPDATA%` for a standard user and `Program Files\mRemoteNG\Settings` when it ran elevated. The lab reproduced both halves on the previous installer before any code changed. The first candidate also showed what a migration can break: once the program folder's copy had been brought into `%APPDATA%`, the connections-file picker found two files and stopped the start-up with a modal dialog.

## Rule

An installed copy's data location comes from an installation fact, such as a file the installer lays down, not from a permission check that changes with elevation. The label people see comes from the same fact. A migration that copies data from an old location also retires the old location, by renaming and never by deleting, once the copy is complete and identical. Otherwise every later discovery step offers the old and new copies as competing choices. A change to packaging, installation or settings locations is proven with an install and an upgrade in the lab before it is committed (D11).
