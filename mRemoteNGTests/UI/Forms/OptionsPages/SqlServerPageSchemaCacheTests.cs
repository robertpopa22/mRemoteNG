using System;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using mRemoteNG.UI.Forms.OptionsPages;
using NUnit.Framework;

namespace mRemoteNGTests.UI.Forms.OptionsPages
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public class SqlServerPageSchemaCacheTests
    {
        [Test]
        public void RepeatedLoadReusesSchemaListAndSelection()
        {
            using var page = new SqlServerPage();
            var load = typeof(SqlServerPage).GetMethod("SqlServerPage_Load", BindingFlags.Instance | BindingFlags.NonPublic);
            var combo = (ComboBox)typeof(SqlServerPage)
                .GetField("DCMSetupddschema", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(page);
            load.Invoke(page, new object[] { page, EventArgs.Empty });

            // A sentinel also covers machines where the initial scan found no Schemas folder.
            combo.DataSource = null;
            combo.Items.Clear();
            combo.Items.Add("cached schema");
            combo.SelectedIndex = 0;

            load.Invoke(page, new object[] { page, EventArgs.Empty });
            typeof(ComboBox).GetMethod("OnDropDown", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(combo, new object[] { EventArgs.Empty });

            Assert.That(combo.Items.Count, Is.EqualTo(1));
            Assert.That(combo.SelectedItem, Is.EqualTo("cached schema"));
        }
    }
}
