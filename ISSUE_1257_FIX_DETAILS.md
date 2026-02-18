# Fix #1257: Dispose OptionPages in FrmOptions

This change addresses a resource leak where opening the Options panel multiple times causes an exception (and likely a crash) due to exhaustion of GDI handles or other resources.

## Changes

-   **mRemoteNG/UI/Forms/frmOptions.cs**:
    -   Added a custom `Dispose(bool disposing)` override.
    -   This method explicitly iterates through the `_optionPages` list and calls `Dispose()` on each `OptionsPage`.
    -   It clears the `_optionPages` list.
    -   It also disposes the `components` container (as the designer code used to do).

-   **mRemoteNG/UI/Forms/frmOptions.Designer.cs**:
    -   Removed the default `Dispose` implementation to avoid conflict and ensure the custom cleanup logic is used.

## Rationale

`FrmOptions` caches instances of `OptionsPage` in a `List<OptionsPage> _optionPages`. These pages are `UserControl`s. When `FrmOptions` is disposed (e.g., when `OptionsWindow` is recreated by `FrmMain`), the default `Form.Dispose` only disposes controls that are currently in the `Controls` collection. Since only the *active* option page is in `Controls` at any time, all other inactive pages were leaked.

By explicitly disposing all pages in `_optionPages`, we ensure that all resources (including child controls and their handles) are released, preventing the leak and the subsequent crash after multiple recreations.
