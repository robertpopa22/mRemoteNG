# Responsive native layouts

Standalone input dialogs use `ResponsiveForm`. It converts legacy fixed coordinate
rows into table layouts, keeps action buttons outside the scrolling body, applies
the configured interface font, and limits the initial window to its working area.
Docked tool panes and Options pages use the same `AdaptiveLayout` preparation.
Session viewports retain their protocol-specific sizing.

Input cells expand with their container. Captions wrap, action rows wrap when
necessary, and scrolling keeps content accessible when the available screen is
smaller than the content. Existing table cells, spans, explicitly collapsed cells,
visibility, enabled states, input values and password masking are preserved.
Layouts are prepared once per control instance. After initial designer scaling,
legacy font-scaled containers use DPI scaling so a font change remeasures content
without multiplying input minimum widths.
Property grids own their internal view fonts and row metrics; the shared font
walker does not replace them.
Numeric arrow glyphs and Marlett window symbols retain their own fonts.
NG buttons, checkboxes and radio buttons measure wrapped captions with the same
text flags as their painting. Content rows remain AutoSize; native combo rows
also account for the height assigned by the Windows control. The main menu puts
items that do not fit into its overflow menu.

## Windows theme default

New settings follow the Windows app theme by default: `vs2015lightNG` for light,
`vs2015darkNG` for dark. Options > Theme exposes the switch. Disabling it restores
the saved manual theme. Following Windows does not replace that saved choice.
An existing explicit manual preference is retained when upgrading.

## Visual verification

Run the interactive fixtures in the isolated Windows lab, not against an operator
profile:

```powershell
.\lab-run.ps1 -Filter 'FullyQualifiedName~VisualLayoutAuditAcceptanceTests|FullyQualifiedName~FormGalleryAuditTests' -Artifacts
```

`VisualLayoutAuditAcceptanceTests` starts the real executable with synthetic
connections. It navigates all 13 Options pages at two window sizes, covers Windows
light and dark defaults, a manual theme, and 8.25, 14 and 24 point fonts. It records
the real native window DPI and restores its temporary Windows theme setting.

`FormGalleryAuditTests` discovers product forms and Options pages. It records a
disposition for every type, renders isolated classes with synthetic data, captures
resized windows and a 14 point font, and checks that interactive fields can be
reached through scrolling. SQL tabs are captured without connecting to a server.
Main, Options, tree and configuration panes are covered by real-executable
navigation instead of bare class previews. Screenshots, layout diagnostics and
the inventory are collected beneath `_evidence` by the lab runner.

Inspect the captured pixels as well as the geometry assertions. Class previews
are layout evidence; they do not establish remote protocol, directory-service or
external database behavior. Repeat at the target Windows scale and verify the
recorded native DPI. A single-monitor startup at another scale does not prove a
live move between monitors with different DPI.

`CommonDialogVisualAuditTests` captures Windows file, font and color pickers and
cancels each one. These system-owned dialogs retain the layout provided by Windows.
