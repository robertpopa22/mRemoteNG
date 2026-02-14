using System;
using System.ComponentModel;
using System.Drawing.Design;
using System.Windows.Forms;
using System.Windows.Forms.Design;
using mRemoteNG.Resources.Language;

namespace mRemoteNG.UI.Controls.ConnectionInfoPropertyGrid
{
    public class PasswordRevealEditor : UITypeEditor
    {
        public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext? context)
        {
            return UITypeEditorEditStyle.Modal;
        }

        public override object? EditValue(ITypeDescriptorContext? context, IServiceProvider provider, object? value)
        {
            if (provider?.GetService(typeof(IWindowsFormsEditorService)) is IWindowsFormsEditorService edSvc)
            {
                var password = value as string ?? string.Empty;
                
                // We show a simple message box or custom dialog to reveal the password.
                // Using a message box with a caption is the simplest "reveal" mechanism.
                MessageBox.Show(
                    "The password is: " + password,
                    "Password Reveal",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }

            return value;
        }
    }
}
