using System.Drawing;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using mRemoteNG.UI.Window;
using NUnit.Framework;

namespace mRemoteNGTests.UI.Window.ConfigWindowTests
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public class ConfigWindowMenuDisposalTests
    {
        [TestCase(false)]
        [TestCase(true)]
        public void ClearingOrDisposingWindowReleasesMenuItemsAndOwnedImages(bool disposeWindow)
        {
            using var window = new ConfigWindow();
            var menu = (ContextMenuStrip)typeof(ConfigWindow)
                .GetField("CMenIcons", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(window);
            using var firstImage = new Bitmap(1, 1);
            using var secondImage = new Bitmap(1, 1);
            var first = new ToolStripMenuItem("first", firstImage);
            var second = new ToolStripMenuItem("second", secondImage);
            // ToolStripItem.IsDisposed stays false after Dispose() on .NET 10; the Disposed event is the signal.
            bool firstDisposed = false, secondDisposed = false;
            first.Disposed += (_, _) => firstDisposed = true;
            second.Disposed += (_, _) => secondDisposed = true;
            menu.Items.AddRange(new ToolStripItem[] { first, second });

            if (disposeWindow)
                window.Dispose();
            else
                typeof(ConfigWindow).GetMethod("ClearIconMenuItems", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(window, null);

            Assert.That(menu.Items.Count, Is.Zero);
            Assert.That(firstDisposed, Is.True);
            Assert.That(secondDisposed, Is.True);
            Assert.That(() => firstImage.GetPixel(0, 0), Throws.ArgumentException);
            Assert.That(() => secondImage.GetPixel(0, 0), Throws.ArgumentException);
        }
    }
}
