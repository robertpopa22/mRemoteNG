using System.Drawing;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using mRemoteNG.UI;
using mRemoteNG.UI.Forms.OptionsPages;
using NUnit.Framework;

namespace mRemoteNGTests.UI.Forms;

[TestFixture]
[Apartment(ApartmentState.STA)]
public class OptionsPageNarrowFirstLayoutTests
{
    [TestCase(0)]
    [TestCase(10)]
    public void PageFirstLaidOutInANarrowPanelWidensWithIt(int firstWidth)
    {
        using var form = new Form { ClientSize = new Size(1000, 700) };
        var host = new Panel { Location = Point.Empty, Size = new Size(firstWidth, 700) };
        form.Controls.Add(host);
        using var page = new StartupExitPage { Dock = DockStyle.Fill };
        page.ApplyLanguage();
        AdaptiveLayout.Prepare(page);
        _ = form.Handle;
        host.Controls.Add(page);
        host.PerformLayout();
        Application.DoEvents();

        host.Width = 900;
        host.PerformLayout();
        Application.DoEvents();

        CheckBox[] boxes = Descendants(page).OfType<CheckBox>().ToArray();
        Assert.That(boxes, Is.Not.Empty);
        Assert.That(boxes.Max(c => c.Width), Is.GreaterThan(300),
            "a page first laid out in a narrow panel must not keep its captions at a few letters (#232)");
    }

    private static System.Collections.Generic.IEnumerable<Control> Descendants(Control root)
    {
        foreach (Control child in root.Controls)
        {
            yield return child;
            foreach (Control nested in Descendants(child)) yield return nested;
        }
    }
}
