using System.Drawing;
using mRemoteNG.App.Info;
using mRemoteNG.UI.Forms;
using NUnit.Framework;

namespace mRemoteNGTests.UI.Forms
{
    /// <summary>
    /// #208: at 200% the splash text overlapped because the canvas and line positions stayed at
    /// 96-DPI pixels while the point-sized fonts doubled.
    /// </summary>
    [TestFixture]
    public class SplashScreenDpiLayoutTests
    {
        private static SizeF Measure(string text, Font font, int dpi)
        {
            using Bitmap bitmap = new(1, 1);
            bitmap.SetResolution(dpi, dpi);
            using Graphics g = Graphics.FromImage(bitmap);
            return g.MeasureString(text, font);
        }

        [TestCase(96)]
        [TestCase(144)]
        [TestCase(192)]
        public void TheTextLinesFitTheCanvasWithoutOverlapping(int dpi)
        {
            using Font versionFont = new("Segoe UI", 16f);
            using Font subtitleFont = new("Segoe UI", 14f, FontStyle.Bold);
            using Font taglineFont = new("Segoe UI", 11f, FontStyle.Italic);
            SizeF version = Measure($"v. {GeneralAppInfo.ApplicationVersion} — Community Edition", versionFont, dpi);
            SizeF subtitle = Measure("Multi-Remote Next Generation Connection Manager", subtitleFont, dpi);
            SizeF tagline = Measure("AI-assisted open source · 16 protocols · .NET 10", taglineFont, dpi);

            Size canvas = FrmSplashScreenNew.CanvasSize(dpi);
            var tops = FrmSplashScreenNew.LineTops(dpi);

            Assert.Multiple(() =>
            {
                // MeasureString pads each line; allow the padding GDI+ adds at the bottom.
                float slack = 0.25f * dpi / 96f * 16f;
                Assert.That(tops.Version + version.Height - slack, Is.LessThanOrEqualTo(tops.Subtitle), "version runs into the subtitle");
                Assert.That(tops.Subtitle + subtitle.Height - slack, Is.LessThanOrEqualTo(tops.Tagline), "subtitle runs into the tagline");
                Assert.That(tops.Tagline + tagline.Height - slack, Is.LessThanOrEqualTo(canvas.Height), "tagline runs off the canvas");
                Assert.That(subtitle.Width, Is.LessThan(canvas.Width));
                Assert.That(version.Width, Is.LessThan(canvas.Width));
            });
        }

        [Test]
        public void TheCanvasScalesWithTheDpi()
        {
            Assert.That(FrmSplashScreenNew.CanvasSize(96), Is.EqualTo(new Size(900, 240)));
            Assert.That(FrmSplashScreenNew.CanvasSize(192), Is.EqualTo(new Size(1800, 480)));
        }
    }
}
