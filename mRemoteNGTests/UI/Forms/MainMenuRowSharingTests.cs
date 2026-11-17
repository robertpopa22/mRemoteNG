using System.Drawing;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using mRemoteNG.UI.Forms;
using NUnit.Framework;

namespace mRemoteNGTests.UI.Forms;

/// <summary>
/// #253: the main menu and another toolbar must be able to share a row of the top tool strip
/// panel, and the menu must still show all of its menus when there is room.
/// </summary>
[TestFixture]
[Apartment(ApartmentState.STA)]
public class MainMenuRowSharingTests
{
    [Test]
    public void AToolbarDroppedBesideTheMainMenuSharesItsRowAndHidesNoMenu()
    {
        MenuStrip mainMenu = FrmMain.Default.msMain;
        using var form = new Form { ClientSize = new Size(1100, 600) };
        var container = new ToolStripContainer { Dock = DockStyle.Fill };
        // The main window's own settings on a menu with the same six top-level menus.
        var menu = new MenuStrip
        {
            Dock = DockStyle.None,
            Size = mainMenu.Size,
            GripStyle = ToolStripGripStyle.Visible,
            Stretch = mainMenu.Stretch,
            CanOverflow = mainMenu.CanOverflow,
        };
        foreach (ToolStripItem item in mainMenu.Items)
            menu.Items.Add(new ToolStripMenuItem(item.Text) { Overflow = item.Overflow });
        var tools = new ToolStrip { Dock = DockStyle.None };
        tools.Items.Add("WinSCP");
        tools.Items.Add("Remote Command");
        container.TopToolStripPanel.Controls.Add(menu);
        form.Controls.Add(container);
        menu.PerformLayout();
        form.Show();

        container.TopToolStripPanel.Join(tools, new Point(menu.Right + 5, 0));
        Application.DoEvents();

        Assert.Multiple(() =>
        {
            Assert.That(container.TopToolStripPanel.Rows, Has.Length.EqualTo(1), "both strips on one row");
            Assert.That(tools.Top, Is.EqualTo(menu.Top));
            Assert.That(menu.Items.Cast<ToolStripItem>().Where(i => i.IsOnOverflow).Select(i => i.Text), Is.Empty,
                "no menu hidden in the overflow while the row has room");
        });
    }
}
