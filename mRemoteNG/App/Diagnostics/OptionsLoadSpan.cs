using System;
using System.Diagnostics;
using System.Threading;

namespace mRemoteNG.App.Diagnostics;

/// <summary>Pairs a pre-operation record with scope exit, including exception unwinding.</summary>
internal sealed class OptionsLoadSpan : IDisposable
{
    private static int _nextId;
    [ThreadStatic] private static int _currentId;
    private readonly int _previousId;
    private readonly int _id = Interlocked.Increment(ref _nextId);
    private readonly int _parent;
    private readonly OptionsLoadStage _stage;
    private readonly string _page;
    private readonly long _started;
    private bool _disposed;

    internal static int CurrentId => _currentId;

    internal OptionsLoadSpan(OptionsLoadStage stage, string? page = null, int parent = 0)
    {
        _previousId = _currentId;
        _parent = parent == 0 ? _previousId : parent;
        _stage = stage;
        // Never write localized captions, settings or arbitrary caller text.
        _page = page switch
        {
            null => "none",
            "StartupExitPage" or "AppearancePage" or "ConnectionsPage" or "TabsPanelsPage" or
            "NotificationsPage" or "CredentialsPage" or "SqlServerPage" or "UpdatesPage" or
            "ThemePage" or "SecurityPage" or "AdvancedPage" or "BackupPage" or "ConfigurationPage" => page,
            _ => "other"
        };
        _currentId = _id;
        _started = Stopwatch.GetTimestamp();
        Write("begin", 0);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _currentId = _previousId;
        // "leave" is not success: using also exits while a product exception propagates.
        Write("leave", (long)Stopwatch.GetElapsedTime(_started).TotalMilliseconds);
    }

    private void Write(string phase, long elapsedMs)
    {
        try
        {
            RuntimeDiagnostics.RecordOptionsLoad(_id, _parent, _stage, _page, phase, elapsedMs);
        }
        catch (Exception)
        {
            // Diagnostic failure must neither skip the operation nor replace its exception.
            return;
        }
    }
}

internal enum OptionsLoadStage
{
    Open, WindowCreate, SelectPage, Refresh, DockShow, WindowLoad, EmbedForm,
    FormCreate, FormLoad, FormShow, FormFont, WindowFont, PageCreate, PageLanguage,
    PageRegistry, PageSettings, PageLayout, PageRegister, Reload, PageRevert
}
