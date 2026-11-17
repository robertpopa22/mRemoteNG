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
    private readonly TimeSpan _jitStarted;
    private readonly long _jitMethodsStarted;
    private readonly long _allocStarted;
    private readonly int _gc0Started;
    private readonly int _gc2Started;
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
        Write("begin", 0);
        _jitStarted = System.Runtime.JitInfo.GetCompilationTime(currentThread: true);
        _jitMethodsStarted = System.Runtime.JitInfo.GetCompiledMethodCount(currentThread: true);
        _allocStarted = GC.GetAllocatedBytesForCurrentThread();
        _gc0Started = GC.CollectionCount(0);
        _gc2Started = GC.CollectionCount(2);
        _started = Stopwatch.GetTimestamp();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        long elapsed = (long)Stopwatch.GetElapsedTime(_started).TotalMilliseconds;
        _currentId = _previousId;
        // "leave" is not success: using also exits while a product exception propagates.
        try
        {
            RuntimeDiagnostics.RecordOptionsLoadLeave(_id, _parent, _stage, _page, elapsed,
                (long)(System.Runtime.JitInfo.GetCompilationTime(currentThread: true) - _jitStarted).TotalMilliseconds,
                System.Runtime.JitInfo.GetCompiledMethodCount(currentThread: true) - _jitMethodsStarted,
                (GC.GetAllocatedBytesForCurrentThread() - _allocStarted) / 1024,
                GC.CollectionCount(0) - _gc0Started,
                GC.CollectionCount(2) - _gc2Started);
        }
        catch (Exception)
        {
            // Diagnostic failure must neither skip the operation nor replace its exception.
        }
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
    PageRegistry, PageSettings, PageLayout, PageRegister, Reload, PageRevert,
    // #232 finer split of the first open.
    FormInit, FormPages, FormFit, WindowTheme, PageFontCreate, PageFontApply, PageAdaptive,
    PageListAdd, PageTrack, LanguageList, LanguageCombo, AppearanceFields,
    ThemeList, ThemeSelect, ThemeApply
}
