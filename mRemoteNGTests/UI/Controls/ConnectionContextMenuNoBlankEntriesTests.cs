using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using mRemoteNG.UI.Controls;
using NUnit.Framework;

namespace mRemoteNGTests.UI.Controls
{
    /// <summary>
    /// A menu entry with no caption is invisible to a user as anything except a mysterious blank
    /// row, and it is the exact symptom reported in #160: two entries rendered empty because their
    /// text was never assigned. Nothing in the suite would have caught that -- the items existed,
    /// so every structural assertion passed.
    ///
    /// This walks the whole menu, recursively, and fails on any non-separator item with no caption.
    /// It is a cheap standing guard over a class of defect the automated tests could otherwise only
    /// discover through a user reporting it.
    /// </summary>
    [TestFixture]
    public class ConnectionContextMenuNoBlankEntriesTests
    {
        private static ConnectionContextMenu BuildMenu()
        {
            ConnectionContextMenu? menu = null;
            Exception? failure = null;

            var thread = new Thread(() =>
            {
                try
                {
                    menu = new ConnectionContextMenu(null!);
                }
                catch (Exception ex)
                {
                    failure = ex;
                }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join(TimeSpan.FromSeconds(30));

            if (failure != null)
                throw failure;

            Assert.That(menu, Is.Not.Null, "context menu could not be constructed");
            return menu!;
        }

        private static IEnumerable<ToolStripItem> Flatten(ToolStripItemCollection items)
        {
            foreach (ToolStripItem item in items)
            {
                yield return item;

                if (item is ToolStripDropDownItem dropDown && dropDown.HasDropDownItems)
                {
                    foreach (ToolStripItem child in Flatten(dropDown.DropDownItems))
                        yield return child;
                }
            }
        }

        [Test]
        public void NoMenuEntryRendersWithoutACaption()
        {
            ConnectionContextMenu menu = BuildMenu();

            List<string> blank = Flatten(menu.Items)
                .Where(item => item is not ToolStripSeparator)
                .Where(item => string.IsNullOrWhiteSpace(item.Text))
                .Select(item => string.IsNullOrEmpty(item.Name) ? item.GetType().Name : item.Name)
                .ToList();

            Assert.That(blank, Is.Empty,
                        "menu entries with no caption render as blank rows (#160): "
                        + string.Join(", ", blank));
        }

        [Test]
        public void EveryEntryIsEitherASeparatorOrHasAName()
        {
            // An unnamed item cannot be referenced, localized or asserted on later; it is how the
            // blank entries slipped in unnoticed in the first place.
            ConnectionContextMenu menu = BuildMenu();

            List<string> unnamed = Flatten(menu.Items)
                .Where(item => item is not ToolStripSeparator)
                .Where(item => string.IsNullOrEmpty(item.Name))
                .Select(item => $"{item.GetType().Name}('{item.Text}')")
                .ToList();

            Assert.That(unnamed, Is.Empty, "unnamed menu entries: " + string.Join(", ", unnamed));
        }
    }
}
