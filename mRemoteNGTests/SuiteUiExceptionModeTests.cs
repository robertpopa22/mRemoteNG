using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using NUnit.Framework;

namespace mRemoteNGTests;

/// <summary>
/// Pins <see cref="SuiteUiExceptionMode"/>: an exception in a window procedure reaches the test,
/// never the ThreadExceptionDialog.
/// </summary>
[Apartment(ApartmentState.STA)]
public class SuiteUiExceptionModeTests
{
    private const int WmThrow = 0x0400 + 0x2A5; // WM_USER + arbitrary

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool PostMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

    private sealed class ThrowingControl : Control
    {
        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WmThrow)
                throw new InvalidOperationException("thrown from a window procedure");
            base.WndProc(ref m);
        }
    }

    [Test]
    public void TheSuiteThrowsFromWindowProcedures() =>
        Assert.That(SuiteUiExceptionMode.ThrowsFromWindowProcedures, Is.True,
                    "set before any window exists, so no window procedure exception can open a dialog");

    [Test]
    public void AnExceptionInAWindowProcedureReachesTheCodeThatPumpedTheMessage()
    {
        using ThrowingControl control = new();
        Assert.That(PostMessage(control.Handle, WmThrow, IntPtr.Zero, IntPtr.Zero), Is.True);

        // Posted and dispatched by this thread's own pump. A sent message is not used: when Windows
        // delivers it through a kernel callback the exception cannot unwind back to the sender and
        // ends the test host instead, which is the intended outcome on a real failure but not here.
        Assert.That(Application.DoEvents,
                    Throws.InvalidOperationException.With.Message.EqualTo("thrown from a window procedure"));
    }

    [Test]
    public void WhatWinFormsStillRoutesToThreadExceptionIsRecordedForTheTest()
    {
        Application.OnThreadException(new InvalidOperationException("routed to ThreadException"));

        // Drained here so the recorded entry does not fail this test in AfterTest.
        Assert.That(SuiteUiExceptionMode.Drain(), Has.Count.EqualTo(1).And.Some.Contains("routed to ThreadException"));
    }
}
