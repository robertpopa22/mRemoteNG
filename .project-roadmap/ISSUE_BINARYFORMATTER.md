# Issue: .NET 10 Startup Crash — BinaryFormatter Removal Breaks DockPanelSuite Theme Loading

**Status:** Fixed (workaround applied)
**Severity:** Critical (app won't start)
**Affected versions:** v1.79.0+ (any build targeting `net10.0`)
**Fixed in:** commit on `release/1.79`

---

## GitHub Issue (ready to submit on upstream mRemoteNG/mRemoteNG)

### Title
`.NET 10: Startup crash — BinaryFormatter removal breaks DockPanelSuite theme loading`

### Labels
`bug`, `priority: critical`, `.NET 10`

### Body

**Describe the bug**

mRemoteNG crashes immediately at startup with `System.NotSupportedException: BinaryFormatter serialization and deserialization are disabled within this application` when running on .NET 10.

**Root cause**

.NET 10 completely removed `BinaryFormatter` from the runtime ([Microsoft announcement](https://github.com/dotnet/runtime/issues/98245)). DockPanelSuite 3.1.1 internally uses `System.Resources.ResourceReader.DeserializeObject()` (which calls `BinaryFormatter`) to load theme indicator images embedded as binary-serialized resources in `.resx` files.

The crash path is:
```
ThemeManager..ctor()
  → ThemeManager.SetActive()
    → ThemeManager.DefaultTheme  [VS2015LightTheme]
      → VS2015ThemeBase..ctor()
        → ImageService..ctor()
          → Resources.get_Dockindicator_PaneDiamond_Hotspot()
            → ResourceReader.DeserializeObject()  ← throws NotSupportedException
```

**Full stack trace**
```
Unhandled exception. System.NotSupportedException: BinaryFormatter serialization and deserialization are disabled within this application. See https://aka.ms/binaryformatter for more information.
   at System.Resources.ResourceReader.DeserializeObject(Int32 typeIndex)
   at System.Resources.ResourceReader.LoadObjectV2(Int32 pos, ResourceTypeCode& typeCode)
   at System.Resources.ResourceReader.LoadObject(Int32 pos, ResourceTypeCode& typeCode)
   at System.Resources.RuntimeResourceSet.ReadValue(ResourceReader reader, Int32 dataPos, Boolean isString, ResourceLocator& locator)
   at System.Resources.RuntimeResourceSet.GetObject(String key, Boolean ignoreCase, Boolean isString)
   at System.Resources.ResourceManager.GetObject(String name, CultureInfo culture, Boolean wrapUnmanagedMemStream)
   at WeifenLuo.WinFormsUI.ThemeVS2012.Resources.get_Dockindicator_PaneDiamond_Hotspot()
   at WeifenLuo.WinFormsUI.ThemeVS2012.ImageService..ctor(ThemeBase theme)
   at WeifenLuo.WinFormsUI.ThemeVS2015.VS2015ThemeBase..ctor(Byte[] resources)
   at WeifenLuo.WinFormsUI.Docking.VS2015LightTheme..ctor()
   at mRemoteNG.Themes.ThemeManager.get_DefaultTheme()
   at mRemoteNG.Themes.ThemeManager.SetActive()
   at mRemoteNG.Themes.ThemeManager..ctor()
   at mRemoteNG.Themes.ThemeManager.getInstance()
   at mRemoteNG.UI.Controls.QuickConnectToolStrip..ctor()
   at mRemoteNG.UI.Forms.FrmMain.InitializeComponent()
   at mRemoteNG.UI.Forms.FrmMain..ctor()
```

**Affected components**

| Project | Binary .resx files | Impact |
|---------|-------------------|--------|
| mRemoteNG | 89 | Startup crash via DockPanelSuite theme loading |
| ExternalConnectors | 2 | Forms with binary-serialized resources |
| ObjectListView | 2 | Already had workaround (`EnableUnsafeBinaryFormatterSerialization=true`) |
| mRemoteNGTests | 4 | Test forms with binary-serialized resources |

No direct C# usage of `BinaryFormatter`, `IFormatter`, or `SoapFormatter` exists in the codebase. The dependency is exclusively through DockPanelSuite 3.1.1 and WinForms `.resx` resource deserialization.

**Fix applied (workaround)**

1. Added `System.Runtime.Serialization.Formatters` 10.0.2 NuGet package (Microsoft's compatibility package that re-adds BinaryFormatter to .NET 9/10)
2. Set `<EnableUnsafeBinaryFormatterSerialization>true</EnableUnsafeBinaryFormatterSerialization>` in all affected `.csproj` files
3. Applied uniformly across: `mRemoteNG.csproj`, `ExternalConnectors.csproj`, `mRemoteNGTests.csproj`
4. `ObjectListView.NetCore.csproj` already had the flag set

**Files changed**
- `Directory.Packages.props` — added `System.Runtime.Serialization.Formatters` 10.0.2
- `mRemoteNG/mRemoteNG.csproj` — added package reference + `EnableUnsafeBinaryFormatterSerialization`
- `ExternalConnectors/ExternalConnectors.csproj` — added `EnableUnsafeBinaryFormatterSerialization`
- `mRemoteNGTests/mRemoteNGTests.csproj` — added `EnableUnsafeBinaryFormatterSerialization`

**Long-term solution (roadmap)**

The `System.Runtime.Serialization.Formatters` package is marked as **legacy/unsupported** by Microsoft. The proper long-term fixes are:

1. **Upgrade DockPanelSuite** to a version that doesn't use `BinaryFormatter` for resource deserialization (none available as of Feb 2026)
2. **Fork DockPanelSuite** and convert binary-serialized resources to non-binary format (PNG files loaded directly instead of `System.Drawing.Bitmap` in .resx)
3. **Convert project .resx files** to use `System.Resources.Extensions` `PreserializedResourceWriter` format

**References**
- [BinaryFormatter removed in .NET 9](https://github.com/dotnet/runtime/issues/98245)
- [BinaryFormatter migration guide](https://learn.microsoft.com/en-us/dotnet/standard/serialization/binaryformatter-migration-guide/)
- [Compatibility package](https://learn.microsoft.com/en-us/dotnet/standard/serialization/binaryformatter-migration-guide/compatibility-package)
- [DockPanelSuite 3.1.1 NuGet](https://www.nuget.org/packages/DockPanelSuite/3.1.1)

**Environment**
- .NET 10.0.2 runtime
- DockPanelSuite 3.1.1
- Windows 10/11
