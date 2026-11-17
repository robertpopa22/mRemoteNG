using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using mRemoteNG.Themes;
using mRemoteNG.UI.Forms;
using mRemoteNG.UI.Window;
using NUnit.Framework;

namespace mRemoteNGTests.UI
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public class ThemeSubscriptionLifetimeTests
    {
        [TestCase(typeof(FrmExport), "ExportForm_Load")]
        [TestCase(typeof(ConfigWindow), "Config_Load")]
        [TestCase(typeof(UpdateWindow), "Update_Load")]
        public void RepeatedLoadSubscribesOnceAndDisposeUnsubscribes(Type formType, string loadHandler)
        {
            using var form = (Form)Activator.CreateInstance(formType);
            var load = formType.GetMethod(loadHandler, BindingFlags.Instance | BindingFlags.NonPublic);
            var manager = ThemeManager.getInstance();

            load.Invoke(form, new object[] { form, EventArgs.Empty });
            load.Invoke(form, new object[] { form, EventArgs.Empty });

            Assert.That(SubscriptionCount(manager, form), Is.EqualTo(1));
            form.Dispose();
            Assert.That(SubscriptionCount(manager, form), Is.Zero);
        }

        private static int SubscriptionCount(ThemeManager manager, Form form)
        {
            var handlers = (Delegate)typeof(ThemeManager)
                .GetField("ThemeChangedEvent", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(manager);
            return handlers?.GetInvocationList().Count(handler => ReferenceEquals(handler.Target, form)) ?? 0;
        }
    }
}
