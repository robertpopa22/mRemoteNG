using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using mRemoteNG.Themes;
using mRemoteNG.UI.Window;
using mRemoteNG.UI.Panels;
using mRemoteNG.Connection;
using mRemoteNG.App;
using NUnit.Framework;
using WeifenLuo.WinFormsUI.Docking;

namespace mRemoteNGTests.UI.Window;

[TestFixture, NonParallelizable, Apartment(ApartmentState.STA)]
public class ConnectionWindowRetentionTests
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference[] LoadAndDisposePanel(DockPanel dock)
    {
        ConnectionWindow panel = PanelAdder.AddPanel("retention test", false)!;
        Assert.That(panel, Is.Not.Null);
        panel.Show(dock, DockState.Document);
        using var tab = panel.AddConnectionTab(new ConnectionInfo { Name = "retention tab" }, false);
        Assert.That(tab, Is.Not.Null);
        WeakReference[] references = [new(panel), new(tab!), new(panel.cmenTab), new(panel.TabPageContextMenuStrip!)];
        panel.Dispose();
        Assert.DoesNotThrow(panel.Dispose, "cleanup must be idempotent after the registry entry is gone");
        Assert.That(panel.IsDisposed, Is.True);
        return references;
    }

    private const uint DesktopAllAccess = 0x10000000; // GENERIC_ALL

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern IntPtr CreateDesktopW(string name, IntPtr device, IntPtr deviceMode, uint flags, uint access, IntPtr security);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetThreadDesktop(IntPtr desktop);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool CloseDesktop(IntPtr desktop);

    // An accessibility client on the interactive desktop (UI automation, a screen reader, an input
    // method) asks the windows of this test for their accessible objects and keeps COM references
    // to them. The DockPanelSuite accessible objects of the tab strip and the auto-hide strip hold
    // their control strongly, so each such reference keeps a disposed panel alive: found in a
    // process dump as ref-counted COM handle -> DockPaneStripAccessibleObject -> strip -> panel.
    // That is not the ownership this test is about, and it made the result depend on what else
    // runs on the machine. The scenario runs on its own desktop, which those clients cannot see.
    [Test]
    public void DisposedPanelsAreNotKeptAliveByTheSharedTheme()
    {
        Exception? failure = null;
        IntPtr desktop = CreateDesktopW("mRemoteNG-retention-" + Guid.NewGuid().ToString("N"), IntPtr.Zero, IntPtr.Zero, 0, DesktopAllAccess, IntPtr.Zero);
        Thread thread = new(() =>
        {
            try
            {
                // When the desktop cannot be created or entered the scenario still runs, on the
                // desktop of the test run.
                if (desktop != IntPtr.Zero) SetThreadDesktop(desktop);
                RunScenario();
            }
            catch (Exception ex) { failure = ex; }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        bool finished = thread.Join(TimeSpan.FromSeconds(60));
        if (finished && desktop != IntPtr.Zero) CloseDesktop(desktop);
        Assert.That(finished, Is.True, "the scenario did not finish within 60 seconds");
        if (failure != null) throw failure;
    }

    private static void RunScenario()
    {
        using Form pump = new() { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual,
            Location = new System.Drawing.Point(-10000, -10000) };
        using DockPanel dock = new() { Dock = DockStyle.Fill, DocumentStyle = DocumentStyle.DockingWindow,
            Theme = ThemeManager.getInstance().ActiveTheme.Theme };
        pump.Controls.Add(dock);
        var previousWindows = Runtime.WindowList;
        Runtime.WindowList = new();
        Exception? failure = null;
        pump.Load += (_, _) =>
        {
            try
            {
                WeakReference[] panels = Enumerable.Range(0, 14).SelectMany(_ => LoadAndDisposePanel(dock)).ToArray();
                // Only this ownership test forces GC, never the application or memory acceptance run.
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
                Assert.That(panels.Count(p => p.IsAlive), Is.Zero, "a disposed panel, tab or menu is still rooted");
                GC.KeepAlive(ThemeManager.getInstance());
            }
            catch (Exception ex) { failure = ex; }
            finally { Runtime.WindowList = previousWindows; Application.ExitThread(); }
        };
        Application.Run(pump);
        if (failure != null) throw failure;
    }
}
