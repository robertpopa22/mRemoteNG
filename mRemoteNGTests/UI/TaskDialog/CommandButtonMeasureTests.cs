using System.Drawing;
using System.Windows.Forms;
using mRemoteNG.UI.TaskDialog;
using NUnit.Framework;

namespace mRemoteNGTests.UI.TaskDialog;

public class CommandButtonMeasureTests
{
    [Test]
    public void LargeTextIsMeasuredTheWayItIsDrawn()
    {
        const string text = "Disconnect from the server and close every open session in this panel";
        using Bitmap bitmap = new(10, 10);
        using Graphics graphics = Graphics.FromImage(bitmap);
        using Font font = new("Segoe UI", 9f);
        Size proposed = new(120, int.MaxValue);

        Size measured = CommandButton.MeasureLargeText(graphics, text, font, proposed.Width);
        Size drawn = TextRenderer.MeasureText(graphics, text, font, proposed, CommandButton.LargeTextFormat);
        Size gdiPlus = Size.Ceiling(graphics.MeasureString(text, font, proposed.Width));

        Assert.Multiple(() =>
        {
            Assert.That(measured, Is.EqualTo(drawn));
            Assert.That(measured, Is.Not.EqualTo(gdiPlus));
        });
    }

    [Test]
    public void AFocusedButtonHasAFocusRingInsideItsFace()
    {
        Rectangle face = new(0, 0, 180, 48);

        Assert.Multiple(() =>
        {
            Assert.That(CommandButton.FocusRing(face, focused: true), Is.EqualTo(new Rectangle(3, 3, 174, 42)));
            Assert.That(CommandButton.FocusRing(face, focused: false), Is.EqualTo(Rectangle.Empty));
        });
    }
}
