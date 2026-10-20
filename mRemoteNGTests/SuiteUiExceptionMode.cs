using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using NUnit.Framework;
using NUnit.Framework.Interfaces;

[assembly: mRemoteNGTests.SuiteUiExceptionMode.FailOnUiThreadException]

namespace mRemoteNGTests;

/// <summary>
/// An exception thrown inside a window procedure is caught by WinForms. With no handler on that
/// thread it opens the ThreadExceptionDialog ("Unhandled exception has occurred in your
/// application") on the interactive desktop and the thread stops inside it. If no test waits on
/// that thread, the run goes on and reports success. On 2026-10-20 that happened twice in front of
/// a maintainer watching the build machine over RDP ("Error creating window handle", a ToolStrip
/// hosting a ComboBox, in the test host), and it was taken for a product crash. The dialog shows the
/// exception only up to the window procedure, so it never said which test or thread was involved.
///
/// Two measures turn such an exception into a failure that carries its whole stack:
/// <list type="bullet">
/// <item>The whole process throws from window procedures, so the exception reaches the code that
/// pumped the message: a test fails with it, and a thread nobody waits on crashes the test host,
/// which the runner reports.</item>
/// <item>Anything WinForms still routes to <see cref="Application.ThreadException"/> on a test's
/// thread is recorded and fails that test.</item>
/// </list>
/// </summary>
[SetUpFixture]
public sealed class SuiteUiExceptionMode
{
    private static readonly ConcurrentQueue<string> s_caught = new();

    /// <summary>True once the process-wide mode was applied before any window existed.</summary>
    internal static bool ThrowsFromWindowProcedures { get; private set; }

    [OneTimeSetUp]
    public void ThrowFromWindowProcedures()
    {
        try
        {
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.ThrowException, threadScope: false);
            ThrowsFromWindowProcedures = true;
        }
        catch (InvalidOperationException)
        {
            // The mode can only be set while no window exists in the process.
            TestContext.Progress.WriteLine(
                "WARNING: a window existed before the suite started; exceptions in window procedures "
                + "fall back to the per-test Application.ThreadException check.");
        }
    }

    [OneTimeTearDown]
    public void NothingWasLeftUnreported()
    {
        List<string> late = Drain();
        if (late.Count > 0)
            Assert.Fail("UI-thread exceptions raised after their test had finished:" + Environment.NewLine
                        + string.Join(Environment.NewLine, late));
    }

    /// <summary>
    /// Attached before every test: the handler lives in the thread's WinForms context, which
    /// Application.Run and ExitThread replace, and adding it replaces any other handler rather than
    /// joining it, so attaching once per thread does not last.
    /// </summary>
    internal static void WatchCurrentThread() => Application.ThreadException += Record;

    private static void Record(object sender, ThreadExceptionEventArgs e) =>
        s_caught.Enqueue($"[thread {Environment.CurrentManagedThreadId} '{Thread.CurrentThread.Name}'] {e.Exception}");

    internal static List<string> Drain()
    {
        List<string> caught = [];
        while (s_caught.TryDequeue(out string? entry))
            caught.Add(entry);
        return caught;
    }

    [AttributeUsage(AttributeTargets.Assembly)]
    public sealed class FailOnUiThreadExceptionAttribute : Attribute, ITestAction
    {
        public ActionTargets Targets => ActionTargets.Test;

        public void BeforeTest(ITest test) => WatchCurrentThread();

        public void AfterTest(ITest test)
        {
            List<string> caught = Drain();
            if (caught.Count > 0)
                Assert.Fail("WinForms caught an exception in a window procedure (it would have shown the "
                            + "ThreadExceptionDialog):" + Environment.NewLine
                            + string.Join(Environment.NewLine, caught.Select(c => "  " + c)));
        }
    }
}
