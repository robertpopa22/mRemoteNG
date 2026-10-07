using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using NUnit.Framework;

namespace mRemoteNGTests.UI.Forms
{
    /// <summary>
    /// #213: WinForms starts WM_CLOSE with Cancel already true when validating the remembered
    /// focused control fails. A FormClosing handler that does not reset it leaves the form open.
    /// </summary>
    [TestFixture]
    [Apartment(System.Threading.ApartmentState.STA)]
    public class CloseCancelledByValidationTests
    {
        private static (Form form, TextBox box) ShowFormWithVetoingEditor()
        {
            var form = new Form
            {
                ShowInTaskbar = false,
                StartPosition = FormStartPosition.Manual,
                Location = new Point(-4000, -4000),
                Size = new Size(200, 100)
            };
            var box = new TextBox();
            box.Validating += (_, e) => e.Cancel = true;
            form.Controls.Add(box);
            form.Show();
            form.ActiveControl = box;
            return (form, box);
        }

        [Test]
        public void FormClosingArrivesCancelledWhenTheActiveControlFailsValidation()
        {
            (Form form, _) = ShowFormWithVetoingEditor();
            bool? cancelOnEntry = null;
            form.FormClosing += (_, e) => cancelOnEntry = e.Cancel;
            try
            {
                form.Close();

                Assert.That(cancelOnEntry, Is.True, "WinForms did not pre-cancel the close");
                Assert.That(form.IsDisposed, Is.False, "the vetoed form must stay open");
            }
            finally
            {
                form.Dispose();
            }
        }

        [Test]
        public void ResettingCancelInFormClosingLetsTheFormClose()
        {
            (Form form, _) = ShowFormWithVetoingEditor();
            form.FormClosing += (_, e) => e.Cancel = false;
            try
            {
                form.Close();

                Assert.That(form.IsDisposed, Is.True);
            }
            finally
            {
                form.Dispose();
            }
        }
    }
}
