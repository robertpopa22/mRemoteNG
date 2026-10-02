using System.Drawing;
using System.Windows.Forms;
using mRemoteNG.Themes;
using NUnit.Framework;

namespace mRemoteNGTests.Themes;

public class InterfaceFontTests
{
    [Test]
    public void SizeAndEffectsStayInsideTheChooserLimits()
    {
        using Font small = InterfaceFont.Create("Segoe UI", 4f, FontStyle.Bold | FontStyle.Underline);
        using Font large = InterfaceFont.Create("Segoe UI", 30f, FontStyle.Strikeout);

        Assert.Multiple(() =>
        {
            Assert.That(small.SizeInPoints, Is.EqualTo(InterfaceFont.MinSize));
            Assert.That(small.Bold, Is.True);
            Assert.That(small.Underline, Is.False);
            Assert.That(large.SizeInPoints, Is.EqualTo(InterfaceFont.MaxSize));
            Assert.That(large.Strikeout, Is.False);
            Assert.That(InterfaceFont.Describe(small), Is.EqualTo("Segoe UI 6 Bold"));
        });
    }

    [Test]
    public void AnUnknownFamilyFallsBackToSegoeUi()
    {
        using Font font = InterfaceFont.Create("ThisFontDoesNotExistZZZ", 10f, FontStyle.Italic);

        Assert.Multiple(() =>
        {
            Assert.That(font.FontFamily.Name, Is.EqualTo(InterfaceFont.DefaultFamily));
            Assert.That(font.Italic, Is.True);
            Assert.That(font.SizeInPoints, Is.EqualTo(10f));
        });
    }

    [Test]
    public void ApplyCopiesTheFontOntoChildControlsAndMenuItems()
    {
        using Form form = new();
        Button button = new();
        form.Controls.Add(button);
        using ContextMenuStrip menu = new();
        ToolStripMenuItem parent = new("Parent");
        ToolStripMenuItem child = new("Child");
        parent.DropDownItems.Add(child);
        menu.Items.Add(parent);
        form.ContextMenuStrip = menu;

        Font template = InterfaceFont.Create("Segoe UI", 14f, FontStyle.Italic);
        InterfaceFont.Apply(form, template);
        template.Dispose();

        Assert.Multiple(() =>
        {
            Assert.That(button.Font.SizeInPoints, Is.EqualTo(14f));
            Assert.That(button.Font.Italic, Is.True);
            Assert.That(child.Font.Italic, Is.True);
            Assert.That(button.Font.Name, Is.EqualTo("Segoe UI"));
        });
    }
}
