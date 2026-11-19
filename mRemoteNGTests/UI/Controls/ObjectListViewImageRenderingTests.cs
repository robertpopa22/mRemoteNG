using System.Drawing;
using System.Threading;
using BrightIdeasSoftware;
using NUnit.Framework;

namespace mRemoteNGTests.UI.Controls;

[TestFixture]
[Apartment(ApartmentState.STA)]
public class ObjectListViewImageRenderingTests
{
    [TestCase(96)]
    [TestCase(192)]
    public void BitmapEndsBeforeTheSpaceReservedForTheLabel(int displayDpi)
    {
        using var list = new ObjectListView();
        list.Columns.Add(new OLVColumn());
        list.AddObject(new object());
        var renderer = new BitmapRenderer
        {
            ListView = list,
            ListItem = list.GetItem(0)
        };
        using var icon = new Bitmap(32, 32);
        icon.SetResolution(96, 96);
        using (var iconGraphics = Graphics.FromImage(icon))
            iconGraphics.Clear(Color.Red);

        using var surface = new Bitmap(96, 80);
        surface.SetResolution(displayDpi, displayDpi);
        using var graphics = Graphics.FromImage(surface);
        graphics.Clear(Color.White);
        var cell = new Rectangle(8, 0, 80, 80);

        int width = renderer.PaintIcon(graphics, cell, icon);

        Assert.That(width, Is.EqualTo(icon.Width));
        Assert.That(surface.GetPixel(cell.X + width - 1, 40).ToArgb(), Is.EqualTo(Color.Red.ToArgb()));
        Assert.That(surface.GetPixel(cell.X + width, 40).ToArgb(), Is.EqualTo(Color.White.ToArgb()));
    }

    private sealed class BitmapRenderer : BaseRenderer
    {
        public int PaintIcon(Graphics graphics, Rectangle cell, Image image) => DrawImage(graphics, cell, image);
    }
}
