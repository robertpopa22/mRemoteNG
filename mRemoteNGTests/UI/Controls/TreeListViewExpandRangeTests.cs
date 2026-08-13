using System;
using System.Threading;
using System.Windows.Forms;
using mRemoteNG.Connection;
using mRemoteNG.Container;
using mRemoteNG.Tree;
using mRemoteNG.Tree.Root;
using mRemoteNG.UI.Controls.ConnectionTree;
using NUnit.Framework;

namespace mRemoteNGTests.UI.Controls
{
    /// <summary>
    /// TreeListView.Expand redrew rows from the index the tree model holds for an object down to
    /// the last row in the list. A crash report showed those two disagreeing badly -- index 427
    /// against a 41 row list -- so Expand now skips the redraw when the index falls outside the
    /// list instead of letting RedrawItems throw. This test pins the other half of that guard:
    /// an ordinary expand must still redraw and still add its rows. (#149)
    /// </summary>
    public class TreeListViewExpandRangeTests
    {
        private static void RunWithMessagePump(Action<ConnectionTree> testAction)
        {
            Exception caught = null;
            var thread = new Thread(() =>
            {
                var form = new Form
                {
                    Width = 400,
                    Height = 300,
                    ShowInTaskbar = false,
                    StartPosition = FormStartPosition.Manual,
                    Location = new System.Drawing.Point(-10000, -10000)
                };
                form.Load += (s, e) =>
                {
                    try
                    {
                        var tree = new ConnectionTree { UseFiltering = true, Dock = DockStyle.Fill };
                        form.Controls.Add(tree);
                        Application.DoEvents();
                        testAction(tree);
                    }
                    catch (Exception ex)
                    {
                        caught = ex;
                    }
                    finally
                    {
                        form.Close();
                    }
                };
                Application.Run(form);
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            if (!thread.Join(TimeSpan.FromSeconds(30)))
            {
                thread.Interrupt();
                Assert.Fail("Test timed out after 30 seconds (message pump deadlock)");
            }

            if (caught != null)
                throw caught;
        }

        [Test]
        public void ExpandingAnUnfilteredTreeStillRedrawsWithoutThrowing() => RunWithMessagePump(tree =>
        {
            var connectionTreeModel = new ConnectionTreeModel();
            var root = new RootNodeInfo(RootNodeType.Connection);
            var folder = new ContainerInfo { Name = "folder" };
            folder.AddChild(new ConnectionInfo { Name = "conn" });
            root.AddChild(folder);

            connectionTreeModel.AddRootNode(root);
            tree.ConnectionTreeModel = connectionTreeModel;
            Application.DoEvents();

            tree.ExpandAll();
            Application.DoEvents();
            tree.Collapse(folder);
            Application.DoEvents();
            int collapsedCount = tree.GetItemCount();

            Assert.DoesNotThrow(() => tree.Expand(folder));
            Application.DoEvents();

            // The guard must not swallow an ordinary expand: the child row has to come back.
            Assert.That(tree.GetItemCount(), Is.GreaterThan(collapsedCount));
        });
    }
}
