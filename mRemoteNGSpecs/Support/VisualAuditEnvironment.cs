using System.Windows.Forms;
using System.Runtime.CompilerServices;

namespace mRemoteNGSpecs.Support;

internal static class VisualAuditEnvironment
{
    private static bool _initialized;
    public static bool DpiModeApplied { get; private set; }

    [ModuleInitializer]
    internal static void Bootstrap() => Initialize();

    public static void Initialize()
    {
        if (_initialized) return;
        DpiModeApplied = Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        // Match ProgramRoot before creating any preview controls. GDI+ measurement
        // combined with the NG controls' GDI painting gives misleading clipping.
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        _initialized = true;
    }
}
