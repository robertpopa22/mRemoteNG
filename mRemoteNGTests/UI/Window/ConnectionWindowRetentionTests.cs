using System;
using System.Linq;
using System.Runtime.CompilerServices;
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

    [Test]
    public void DisposedPanelsAreNotKeptAliveByTheSharedTheme()
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
