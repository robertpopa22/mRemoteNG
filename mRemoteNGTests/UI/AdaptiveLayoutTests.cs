using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using mRemoteNG.UI;
using NUnit.Framework;

namespace mRemoteNGTests.UI;

[TestFixture]
[Apartment(ApartmentState.STA)]
public class AdaptiveLayoutTests
{
    [Test]
    public void LargerFontKeepsStaggeredLabelAndInputInsideTheirCells()
    {
        using var form = new Form { ClientSize = new Size(420, 180), AutoScaleMode = AutoScaleMode.Font };
        var label = new Label { Text = "Username", AutoSize = true, Location = new Point(12, 15) };
        var input = new TextBox { Location = new Point(100, 12), Width = 220, UseSystemPasswordChar = true };
        form.Controls.AddRange([label, input]);
        AdaptiveLayout.Prepare(form);
        using var font = new Font("Segoe UI", 14);
        mRemoteNG.Themes.InterfaceFont.Apply(form, font);
        form.PerformLayout();

        Assert.Multiple(() =>
        {
            Assert.That(label.Width, Is.GreaterThan(60), "caption must not collapse to individual letters");
            Assert.That(input.Right, Is.LessThanOrEqualTo(input.Parent!.ClientSize.Width));
            Assert.That(input.Bottom, Is.LessThanOrEqualTo(input.Parent.ClientSize.Height));
            Assert.That(input.UseSystemPasswordChar, Is.True);
        });
    }

    [Test]
    public void ReturningToSmallerFontShrinksInputRows()
    {
        using var panel = new Panel { Size = new Size(400, 200) };
        var input = new TextBox { Location = new Point(10, 10) };
        panel.Controls.Add(input);
        AdaptiveLayout.Prepare(panel);
        using var large = new Font("Segoe UI", 24);
        using var small = new Font("Segoe UI", 8.25f);
        mRemoteNG.Themes.InterfaceFont.Apply(panel, large);
        int largeHeight = input.Height;

        mRemoteNG.Themes.InterfaceFont.Apply(panel, small);

        Assert.That(input.Height, Is.LessThan(largeHeight));
    }

    [Test]
    public void CollapsedCellsAndHiddenControlsRemainHidden()
    {
        using var table = new TableLayoutPanel { ColumnCount = 2, RowCount = 1, Size = new Size(400, 150) };
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 0));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        var collapsed = new Panel();
        var input = new TextBox();
        collapsed.Controls.Add(input);
        var hidden = new CheckBox { Visible = false };
        table.Controls.Add(collapsed, 0, 0);
        table.Controls.Add(hidden, 1, 0);

        AdaptiveLayout.Prepare(table);

        Assert.Multiple(() =>
        {
            Assert.That(table.ColumnStyles[0].SizeType, Is.EqualTo(SizeType.Absolute));
            Assert.That(table.ColumnStyles[0].Width, Is.Zero);
            Assert.That(input.MinimumSize, Is.EqualTo(Size.Empty));
            Assert.That(hidden.Visible, Is.False);
        });
    }

    [Test]
    public void ExistingTableCellsAndSpansSurvivePreparation()
    {
        using var panel = new Panel { Size = new Size(500, 300) };
        var table = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 2 };
        var input = new TextBox();
        var message = new Label { Text = "Description", AutoSize = true };
        table.Controls.Add(input, 1, 0);
        table.Controls.Add(message, 0, 1);
        table.SetColumnSpan(message, 2);
        panel.Controls.Add(table);

        AdaptiveLayout.Prepare(panel);
        AdaptiveLayout.Prepare(panel);

        Assert.Multiple(() =>
        {
            Assert.That(input.Parent, Is.SameAs(table));
            Assert.That(table.GetPositionFromControl(input), Is.EqualTo(new TableLayoutPanelCellPosition(1, 0)));
            Assert.That(table.GetColumnSpan(message), Is.EqualTo(2));
        });
    }

    [Test]
    public void FieldsExpandWithTheContainerAndRetainInputBehavior()
    {
        using var panel = new Panel { Size = new Size(400, 150) };
        var label = new Label { Text = "Password", Location = new Point(10, 10) };
        var input = new TextBox { Text = "synthetic", UseSystemPasswordChar = true, Location = new Point(100, 10) };
        panel.Controls.AddRange([label, input]);
        AdaptiveLayout.Prepare(panel);
        panel.PerformLayout();
        int initialWidth = input.Width;

        panel.Width = 700;
        panel.PerformLayout();

        Assert.Multiple(() =>
        {
            Assert.That(input.Width, Is.GreaterThan(initialWidth));
            Assert.That(input.UseSystemPasswordChar, Is.True);
            Assert.That(input.Text, Is.EqualTo("synthetic"));
        });
    }
}
