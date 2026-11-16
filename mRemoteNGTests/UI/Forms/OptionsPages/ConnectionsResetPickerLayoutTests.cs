using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using mRemoteNG.Resources.Language;
using mRemoteNG.Themes;
using mRemoteNG.UI;
using mRemoteNG.UI.Forms.OptionsPages;
using NUnit.Framework;

namespace mRemoteNGTests.UI.Forms.OptionsPages;

[TestFixture]
[Apartment(ApartmentState.STA)]
public class ConnectionsResetPickerLayoutTests
{
    [TestCase(1f, 8.25f)]
    [TestCase(2f, 8.25f)]
    [TestCase(2f, 14f)]
    public void ResetPickerStaysBelowCheckboxesAfterScalingAndResize(float scale, float fontSize)
    {
        var previousCulture = Language.Culture;
        try
        {
            Language.Culture = CultureInfo.GetCultureInfo("fr-FR");
            using var page = new ConnectionsPage();
            page.ApplyLanguage();
            page.Scale(new SizeF(scale, scale));
            using var font = InterfaceFont.Create("Segoe UI", fontSize, FontStyle.Regular);
            InterfaceFont.Apply(page, font);
            AdaptiveLayout.Prepare(page);
            var button = page.Controls.Find("btnResetResolver", true).Single();
            var lastCheckbox = page.Controls.Find("chkSlowClickRename", true).Single();

            foreach (int width in new[] { 610, 400, 800 })
            {
                page.Size = new Size((int)(width * scale), (int)(400 * scale));
                page.PerformLayout();
                Assert.That(PositionInPage(button).Top, Is.GreaterThanOrEqualTo(PositionInPage(lastCheckbox).Bottom),
                    "Reset picker must stay after the options when the page grows, shrinks or scales");
            }
        }
        finally { Language.Culture = previousCulture; }
    }

    private static Rectangle PositionInPage(Control control)
    {
        var bounds = control.Bounds;
        for (var parent = control.Parent; parent is not null && parent is not ConnectionsPage; parent = parent.Parent)
            bounds.Offset(parent.Location);
        return bounds;
    }
}
