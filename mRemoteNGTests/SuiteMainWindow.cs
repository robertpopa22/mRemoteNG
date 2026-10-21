using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using mRemoteNG.UI.Forms;
using NUnit.Framework;
using NUnit.Framework.Interfaces;

[assembly: mRemoteNGTests.SuiteMainWindow.FailWhenTheMainWindowIsLost]

namespace mRemoteNGTests;

/// <summary>
/// The application has one main window, on its UI thread, for its whole life, and much of the code
/// under test reaches for <see cref="FrmMain.Default"/> on that assumption. The suite provides it:
/// before any test, a background STA thread creates the main window and keeps a message loop
/// running for the rest of the process.
///
/// Before, the main window was created lazily by whichever test asked first, on that test's thread:
/// <list type="bullet">
/// <item>a test's own message-pump thread, which ended with the test. Windows destroyed the main
/// window's windows (the Quick Connect ToolStrip and ComboBox among them), and later tests got a form
/// still holding their handles and its theme subscriptions;</item>
/// <item>NUnit's STA worker (via the field initializer of RdpProtocol), which sits in Join while a
/// UI test's pump thread runs. FrmMain.SelectedConnection invokes onto its owner, so that test
/// deadlocked until its 30-second timeout.</item>
/// </list>
/// </summary>
internal static class SuiteMainWindow
{
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentThreadId();

    private static uint s_nativeThreadId;
    private static bool s_reported;

    internal static Thread? Thread { get; private set; }

    internal static void Start()
    {
        if (FrmMain.IsCreated)
            throw new InvalidOperationException("FrmMain.Default already exists on some other thread.");

        using ManualResetEventSlim ready = new();
        Exception? failure = null;
        Thread = new Thread(() =>
        {
            try
            {
                s_nativeThreadId = GetCurrentThreadId();
                _ = FrmMain.Default.Handle;
            }
            catch (Exception ex)
            {
                failure = ex;
                return;
            }
            finally
            {
                ready.Set();
            }

            PumpForTheRestOfTheProcess();
        })
        {
            IsBackground = true,
            Name = "Suite main window",
        };
        Thread.SetApartmentState(ApartmentState.STA);
        Thread.Start();

        if (!ready.Wait(TimeSpan.FromSeconds(60)))
            throw new TimeoutException("The suite's main window was not created within 60 seconds.");
        if (failure != null)
            throw new InvalidOperationException("Creating the suite's main window failed.", failure);
    }

    private static void PumpForTheRestOfTheProcess()
    {
        for (int failures = 0; failures < 20; failures++)
        {
            try
            {
                Application.Run();
                return;
            }
            catch (Exception ex)
            {
                // An exception in one of the main window's window procedures, invoked from a test on
                // another thread. It fails the test running now instead of ending the process.
                SuiteUiExceptionMode.Record(ex);
            }
        }

        SuiteUiExceptionMode.Record(new InvalidOperationException(
            "The suite's main window thread stopped pumping after 20 exceptions."));
    }

    /// <summary>Empty while the main window is the suite's and alive; otherwise what is wrong.</summary>
    internal static string Describe()
    {
        if (!FrmMain.IsCreated)
            return "FrmMain.Default does not exist";

        FrmMain form = FrmMain.Default;
        if (form.IsDisposed)
            return "FrmMain.Default is disposed";
        if (!form.IsHandleCreated)
            return "FrmMain.Default has no window";

        IntPtr handle = form.Handle;
        uint processId = 0;
        uint owner = IsWindow(handle) ? GetWindowThreadProcessId(handle, out processId) : 0;
        if (owner == 0)
            return $"FrmMain.Default holds window 0x{handle.ToInt64():X}, which no longer exists";
        if (processId != (uint)Environment.ProcessId)
            return $"FrmMain.Default holds window 0x{handle.ToInt64():X}, which now belongs to process {processId}";
        return owner == s_nativeThreadId
            ? ""
            : $"FrmMain.Default's window belongs to thread {owner}, not to the suite's main window thread {s_nativeThreadId}";
    }

    [AttributeUsage(AttributeTargets.Assembly)]
    public sealed class FailWhenTheMainWindowIsLostAttribute : Attribute, ITestAction
    {
        public ActionTargets Targets => ActionTargets.Test;

        public void BeforeTest(ITest test) { }

        public void AfterTest(ITest test)
        {
            // Reported once: the state is permanent for the process, and the test that caused it is
            // the one that just finished.
            if (s_reported || Thread == null)
                return;

            string problem = Describe();
            if (problem.Length == 0)
                return;

            s_reported = true;
            Assert.Fail(problem + ". Every later test now runs against a broken main window.");
        }
    }
}
