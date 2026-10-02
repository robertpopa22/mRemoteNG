using System.Drawing;
using System.Windows.Forms;
using mRemoteNG.UI.Controls;
using NUnit.Framework;

namespace mRemoteNGTests.UI.Controls
{
    [TestFixture]
    [Apartment(System.Threading.ApartmentState.STA)]
    public class DeviceDpiFontFitTests
    {
        [Test]
        public void DescribeRecordsUnitRawSizeAndHeightAtTheWindowDpi()
        {
            using Font font = new("Segoe UI", 8.25f, FontStyle.Regular, GraphicsUnit.Point);
            string text = DeviceDpiFontFit.Describe(font, 192);

            Assert.That(text, Does.Contain("Point"));
            Assert.That(text, Does.Contain("size 8.25"));
            Assert.That(text, Does.Contain("h@192="));
        }

        [Test]
        public void AgreedMenuAndDockFontsAreLeftAlone()
        {
            using Font menu = new("Segoe UI", 9f, FontStyle.Regular, GraphicsUnit.Point);
            using Font dock = new("Segoe UI", 8.25f, FontStyle.Regular, GraphicsUnit.Point);

            Font fitted = DeviceDpiFontFit.Match(menu, dock, 96);

            Assert.That(fitted, Is.SameAs(menu));
        }

        [Test]
        public void HalvedMenuFontIsRebuiltToTheDockLineSpacing()
        {
            using Font menu = new("Segoe UI", 4.5f, FontStyle.Regular, GraphicsUnit.Point);
            using Font dock = new("Segoe UI", 8.25f, FontStyle.Regular, GraphicsUnit.Point);

            Font fitted = DeviceDpiFontFit.Match(menu, dock, 192);
            try
            {
                Assert.That(fitted, Is.Not.SameAs(menu));
                Assert.That(fitted.Unit, Is.EqualTo(GraphicsUnit.Pixel));
                float dockHeight = dock.GetHeight(192);
                Assert.That(fitted.GetHeight(192), Is.EqualTo(dockHeight).Within(dockHeight * 0.15f));
            }
            finally
            {
                if (!ReferenceEquals(fitted, menu))
                    fitted.Dispose();
            }
        }

        [Test]
        public void ToolStripIconsFollowTheWindowDpiWhenTheFontIsRebuilt()
        {
            using Font dock = new("Segoe UI", 8.25f, FontStyle.Regular, GraphicsUnit.Point);
            using Font small = new("Segoe UI", 4.5f, FontStyle.Regular, GraphicsUnit.Point);
            using ToolStrip strip = new();
            strip.Font = small;

            bool changed = DeviceDpiFontFit.FitToolStrip(strip, dock, 192);

            Assert.That(changed, Is.True);
            Assert.That(strip.Font.Unit, Is.EqualTo(GraphicsUnit.Pixel));
            Assert.That(strip.ImageScalingSize, Is.EqualTo(new Size(32, 32)));
        }
    }
}
