using System.Linq;
using System.Threading;
using System.Windows.Forms;
using mRemoteNG.UI.Controls;
using NUnit.Framework;

namespace mRemoteNGTests.UI.Controls
{
    [TestFixture]
    public class ConnectionContextMenuSimpleTests
    {
        [Test]
        public void VerifyOptionsMenuItemExists()
        {
            ConnectionContextMenu? menu = null;
            
            var thread = new Thread(() =>
            {
                // Passing null for connectionTree might be okay for initialization
                // as long as we don't open the menu.
                menu = new ConnectionContextMenu(null!);
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();

            Assert.That(menu, Is.Not.Null);
            var items = menu!.Items.Cast<ToolStripItem>().ToList();
            
            // Look for Options menu item
            var optionsItem = items.FirstOrDefault(i => i.Name == "_cMenTreeOptions");
            Assert.That(optionsItem, Is.Not.Null, "Options menu item not found");
            
            // Look for separator before Options
            var separator = items.FirstOrDefault(i => i.Name == "_cMenTreeSep5");
             Assert.That(separator, Is.Not.Null, "Separator before Options menu item not found");
        }
    }
}
