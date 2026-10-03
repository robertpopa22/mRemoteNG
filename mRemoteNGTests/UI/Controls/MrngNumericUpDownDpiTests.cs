using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using mRemoteNG.Themes;
using mRemoteNG.UI.Controls;
using NUnit.Framework;

namespace mRemoteNGTests.UI.Controls
{
    [TestFixture]
    [Apartment(System.Threading.ApartmentState.STA)]
    public class MrngNumericUpDownDpiTests
    {
        private static bool _dpiModeSet;

        [OneTimeSetUp]
        public void UsePerMonitorDpi()
        {
            if (_dpiModeSet)
                return;

            try
            {
                Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
            }
            catch (InvalidOperationException)
            {
                // Another fixture already created a window. DeviceDpi still has to follow the setter below.
            }

            _dpiModeSet = true;
        }

        [Test]
        public void ArrowWidthUsesDeviceDpiOnce()
        {
            Assert.That(ThemeManager.getInstance().ActiveAndExtended, Is.True);

            // The suite has already created a window, so SetHighDpiMode cannot run.
            // DeviceDpi follows the thread awareness context.
            IntPtr previousAwareness = SetThreadDpiAwarenessContext(PerMonitorAwareV2);
            Assert.That(previousAwareness, Is.Not.EqualTo(IntPtr.Zero));
            try
            {
                using var spinner = new MrngNumericUpDown
                {
                    Size = new System.Drawing.Size(80, 22)
                };
                spinner.CreateControl();

                SetDeviceDpi(spinner, 96);
                RaiseDpiChanged(spinner);
                MrngButton up = UpButton(spinner);
                Assert.That(up.Width, Is.EqualTo(16));

                spinner.Size = new System.Drawing.Size(120, 30);
                Assert.That(up.Width, Is.EqualTo(16), "a second layout at 96 DPI changed the arrow width");

                SetDeviceDpi(spinner, 192);
                RaiseDpiChanged(spinner);
                Assert.That(up.Width, Is.EqualTo(16 * 192 / 96));

                spinner.Scale(new System.Drawing.SizeF(2, 2));
                Assert.That(up.Width, Is.EqualTo(16 * 192 / 96), "the options-page scale doubled the arrow width");

                spinner.Scale(new System.Drawing.SizeF(2, 2));
                Assert.That(up.Width, Is.EqualTo(16 * 192 / 96), "a second scale doubled the arrow width");

                spinner.Size = new System.Drawing.Size(140, 36);
                RaiseDpiChanged(spinner);
                Assert.That(up.Width, Is.EqualTo(16 * 192 / 96), "layout after the DPI change applied the scale again");

                SetDeviceDpi(spinner, 96);
                RaiseDpiChanged(spinner);
                Assert.That(up.Width, Is.EqualTo(16));
            }
            finally
            {
                SetThreadDpiAwarenessContext(previousAwareness);
            }
        }

        private static readonly IntPtr PerMonitorAwareV2 = new(-4);

        [DllImport("user32.dll")]
        private static extern IntPtr SetThreadDpiAwarenessContext(IntPtr dpiContext);

        private static MrngButton UpButton(MrngNumericUpDown spinner)
        {
            foreach (Control child in spinner.Controls)
            {
                if (child is MrngButton button && button.Text.Equals("\u25B2", StringComparison.Ordinal))
                    return button;
            }

            Assert.Fail("the themed up arrow was not created");
            return null!;
        }

        private static void SetDeviceDpi(Control control, int dpi)
        {
            MethodInfo setter = typeof(Control).GetMethod(
                "set_DeviceDpiInternal",
                BindingFlags.Instance | BindingFlags.NonPublic)!;
            setter.Invoke(control, [dpi]);
            Assert.That(control.DeviceDpi, Is.EqualTo(dpi));
        }

        private static void RaiseDpiChanged(Control control)
        {
            MethodInfo raise = typeof(Control).GetMethod(
                "OnDpiChangedAfterParent",
                BindingFlags.Instance | BindingFlags.NonPublic)!;
            raise.Invoke(control, [EventArgs.Empty]);
        }
    }
}
