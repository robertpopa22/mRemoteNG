using System.Drawing;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using mRemoteNG.UI.Tabs;
using NUnit.Framework;
using WeifenLuo.WinFormsUI.Docking;
using WeifenLuo.WinFormsUI.ThemeVS2015;

namespace mRemoteNGTests.UI.Tabs
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public class DockPaneStripNGMenuDisposalTests
    {
        [TestCase(false)]
        [TestCase(true)]
        public void ClearingOrDisposingStripReleasesMenuItemsAndOwnedImages(bool disposeStrip)
        {
            using var theme = new VS2015LightTheme();
            using var panel = new DockPanel { Theme = theme };
            using var content = new DockContent { DockPanel = panel };
            using var pane = panel.Theme.Extender.DockPaneFactory.CreateDockPane(content, DockState.Document, false);
            using var strip = new DockPaneStripNG(pane);
            var menu = (ContextMenuStrip)typeof(DockPaneStripNG)
                .GetProperty("SelectMenu", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(strip);
            using var firstImage = new Bitmap(1, 1);
            using var secondImage = new Bitmap(1, 1);
            var first = new ToolStripMenuItem("first", firstImage);
            var second = new ToolStripMenuItem("second", secondImage);
            // ToolStripItem.IsDisposed stays false after Dispose() on .NET 10; the Disposed event is the signal.
            bool firstDisposed = false, secondDisposed = false;
            first.Disposed += (_, _) => firstDisposed = true;
            second.Disposed += (_, _) => secondDisposed = true;
            menu.Items.AddRange(new ToolStripItem[] { first, second });

            if (disposeStrip)
                strip.Dispose();
            else
                typeof(DockPaneStripNG).GetMethod("ClearWindowListItems", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(strip, null);

            Assert.That(menu.Items.Count, Is.Zero);
            Assert.That(firstDisposed, Is.True);
            Assert.That(secondDisposed, Is.True);
            Assert.That(() => firstImage.GetPixel(0, 0), Throws.ArgumentException);
            Assert.That(() => secondImage.GetPixel(0, 0), Throws.ArgumentException);
        }
    }
}
