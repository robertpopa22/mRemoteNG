using System;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using mRemoteNG.UI.TaskDialog;
using NUnit.Framework;

namespace mRemoteNGTests.UI.TaskDialog
{
    /// <summary>
    /// The confirmation dialog's layout (#198): the reporter saw it much narrower than designed,
    /// the question wrapped into a sliver and cut off, and the "do not show again" check box under
    /// the Yes/No buttons. These tests show the real dialog through a real message loop and check
    /// what a user would see: every label tall enough for its text, nothing under anything else,
    /// and the dialog no narrower than it was laid out for.
    /// </summary>
    [TestFixture]
    [NonParallelizable]
    public class FrmTaskDialogLayoutTests
    {
        private const TextFormatFlags LabelFlags = TextFormatFlags.WordBreak | TextFormatFlags.TextBoxControl;

        // The reporter's message, with a connection name of the same shape as theirs.
        private static readonly string Question = string.Format(CultureInfo.InvariantCulture,
            "Are you sure you want to disconnect \"{0}\"?", "SERVER-PROD-SQL-01.corp.example.local (Local administrator)");

        private const string DoNotShowAgain = "Do not show this message again.";

        private bool _savedSounds;

        [SetUp]
        public void MuteSounds()
        {
            _savedSounds = CTaskDialog.PlaySystemSounds;
            CTaskDialog.PlaySystemSounds = false;
        }

        [TearDown]
        public void RestoreSounds() => CTaskDialog.PlaySystemSounds = _savedSounds;

        [Test]
        public void TheCloseConfirmationShowsAllOfItsTextWithNothingOverlapping()
        {
            ShowAndInspect(verificationText: DoNotShowAgain, beforeShow: null, inspect: AssertLaidOutForContent);
        }

        [Test]
        public void ADialogNarrowedBeforeItIsShownIsLaidOutAgainAtItsFullWidth()
        {
            // What #198's screenshot shows: the window arrives at a fraction of its width (on the
            // reporter's multi-monitor remote session, most likely through a DPI change between
            // building the dialog and showing it).
            ShowAndInspect(verificationText: DoNotShowAgain,
                           beforeShow: dialog => dialog.Width = dialog.LogicalToDeviceUnits(217),
                           inspect: dialog =>
                           {
                               AssertLaidOutForContent(dialog);
                               Assert.That(dialog.Width, Is.GreaterThanOrEqualTo(dialog.LogicalToDeviceUnits(CTaskDialog.EmulatedFormWidth)),
                                           "the dialog must be restored to the width it was built for");
                           });
        }

        [Test]
        public void AVerificationTextTooLongToSitBesideTheButtonsGoesOnARowOfItsOwn()
        {
            ShowAndInspect(verificationText: DoNotShowAgain + " Connections will then be closed without asking, until this is changed in Options.",
                           beforeShow: null,
                           inspect: dialog =>
                           {
                               AssertLaidOutForContent(dialog);
                               Control check = Find(dialog, "cbVerify");
                               int buttonsBottom = VisibleButtons(dialog).Max(b => b.Bottom);
                               Assert.That(check.Top, Is.GreaterThanOrEqualTo(buttonsBottom), "the check box belongs below the buttons");
                           });
        }

        [Test]
        public void WideningTheDialogRewrapsTheTextAndTheHeightFollows()
        {
            ShowAndInspect(verificationText: DoNotShowAgain, beforeShow: null, inspect: dialog =>
            {
                int before = dialog.ClientSize.Height;
                dialog.Width += dialog.LogicalToDeviceUnits(400);

                AssertLaidOutForContent(dialog);
                Assert.That(dialog.ClientSize.Height, Is.LessThanOrEqualTo(before),
                            "more width means fewer lines, so the dialog must not grow taller");
            });
        }

        [Test]
        public void ShowingAndHidingTheDetailsGrowsAndShrinksTheDialog()
        {
            ShowAndInspect(verificationText: DoNotShowAgain,
                           beforeShow: null,
                           expandedInfo: "Line one of the details." + Environment.NewLine
                                         + "Line two of the details." + Environment.NewLine + "Line three.",
                           inspect: dialog =>
                           {
                               Control details = Find(dialog, "lbShowHideDetails");
                               Control expanded = Find(dialog, "pnlExpandedInfo");
                               int collapsed = dialog.ClientSize.Height;

                               InvokeClick(details);
                               Assert.That(expanded.Visible, Is.True);
                               Assert.That(dialog.ClientSize.Height, Is.GreaterThan(collapsed), "showing the details must make room for them");
                               AssertLaidOutForContent(dialog);

                               InvokeClick(details);
                               Assert.That(expanded.Visible, Is.False);
                               Assert.That(dialog.ClientSize.Height, Is.EqualTo(collapsed), "hiding them must give the room back");
                           });
        }

        private static void InvokeClick(Control control) =>
            typeof(Control).GetMethod("OnClick", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
                           .Invoke(control, [EventArgs.Empty]);

        private static void AssertLaidOutForContent(frmTaskDialog dialog)
        {
            Control instruction = Find(dialog, "lbMainInstruction");
            Control instructionPanel = Find(dialog, "pnlMainInstruction");
            Control buttonsPanel = Find(dialog, "pnlButtons");
            Control check = Find(dialog, "cbVerify");

            int needed;
            using (Graphics g = instruction.CreateGraphics())
            {
                needed = TextRenderer.MeasureText(g, instruction.Text, instruction.Font,
                                                  new Size(instruction.ClientSize.Width, int.MaxValue), LabelFlags).Height;
            }

            Assert.Multiple(() =>
            {
                Assert.That(instruction.Height, Is.GreaterThanOrEqualTo(needed),
                            $"the question needs {needed}px at {instruction.ClientSize.Width}px wide and got {instruction.Height}px");
                Assert.That(instruction.Bottom, Is.LessThanOrEqualTo(instructionPanel.ClientSize.Height),
                            "the question runs past the bottom of its panel");

                foreach (Control button in VisibleButtons(dialog))
                {
                    Assert.That(button.Left, Is.GreaterThanOrEqualTo(0), $"{button.Name} starts off the left edge");
                    Assert.That(button.Right, Is.LessThanOrEqualTo(buttonsPanel.ClientSize.Width), $"{button.Name} runs off the right edge");
                    if (check.Visible)
                        Assert.That(check.Bounds.IntersectsWith(button.Bounds), Is.False,
                                    $"the check box {check.Bounds} lies under {button.Name} {button.Bounds}");
                }

                if (check.Visible)
                    Assert.That(check.Bottom, Is.LessThanOrEqualTo(buttonsPanel.ClientSize.Height), "the check box is cut off");

                foreach (Control panel in dialog.Controls.Cast<Control>().Where(c => c.Visible))
                    Assert.That(panel.Bottom, Is.LessThanOrEqualTo(dialog.ClientSize.Height), $"{panel.Name} is cut off at the bottom");
            });
        }

        private static readonly string[] ButtonNames = ["bt1", "bt2", "bt3"];

        private static Control[] VisibleButtons(frmTaskDialog dialog) =>
            ButtonNames.Select(name => Find(dialog, name)).Where(b => b.Visible).ToArray();

        private static Control Find(Form dialog, string name) =>
            dialog.Controls.Find(name, true).Single();

        private static void ShowAndInspect(string verificationText, Action<frmTaskDialog>? beforeShow, Action<frmTaskDialog> inspect,
                                           string expandedInfo = "")
        {
            Exception? caught = null;
            bool inspected = false;

            Thread thread = new(() =>
            {
                using frmTaskDialog dialog = new()
                {
                    Title = "mRemoteNG",
                    MainInstruction = Question,
                    VerificationText = verificationText,
                    ExpandedInfo = expandedInfo,
                    Buttons = ETaskDialogButtons.YesNo,
                    MainIcon = ESysIcons.Question,
                    FooterIcon = ESysIcons.Question,
                    ShowInTaskbar = false,
                    StartPosition = FormStartPosition.Manual,
                    Location = new Point(40, 40),
                    Opacity = 0,
                };
                // What CTaskDialog.ShowTaskDialogBox does.
                dialog.Width = CTaskDialog.EmulatedFormWidth;
                dialog.BuildForm();
                beforeShow?.Invoke(dialog);

                dialog.Shown += (_, _) =>
                {
                    try
                    {
                        inspect(dialog);
                        inspected = true;
                    }
                    catch (Exception ex)
                    {
                        caught = ex;
                    }
                    finally
                    {
                        dialog.Close();
                    }
                };

                Application.Run(dialog);
            });

            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            if (!thread.Join(TimeSpan.FromSeconds(30)))
                Assert.Fail("the dialog did not close within 30 seconds");

            if (caught is not null)
                throw caught;
            Assert.That(inspected, Is.True, "the dialog was never shown");
        }
    }
}
