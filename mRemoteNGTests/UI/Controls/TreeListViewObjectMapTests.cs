using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using BrightIdeasSoftware;
using mRemoteNG.Connection;
using mRemoteNG.Container;
using mRemoteNG.Tree;
using mRemoteNG.Tree.Root;
using mRemoteNG.UI.Controls.ConnectionTree;
using NUnit.Framework;

namespace mRemoteNGTests.UI.Controls
{
    /// <summary>
    /// The tree model keeps a flat row list and a reverse map from object to row index. Any path
    /// that removes rows has to drop those rows from the map too, or a later lookup hands back a
    /// row index that no longer exists -- which is how #149 crashed.
    ///
    /// RebuildChildren was suspected of exactly that, because it removes rows and then calls
    /// RebuildObjectMap with a non-zero start index while RebuildObjectMap only clears the map
    /// when it starts at zero. This test was written to demonstrate it and instead showed the
    /// opposite: after refreshing a branch that lost all of its children the map holds only the
    /// surviving rows. The suspicion was wrong. Kept as an invariant guard on that path.
    /// </summary>
    public class TreeListViewObjectMapTests
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

        private static T GetPrivate<T>(object target, string fieldName)
        {
            FieldInfo field = target.GetType().GetField(fieldName,
                                                        BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"field {fieldName} not found on {target.GetType().Name}");
            return (T)field.GetValue(target);
        }

        [Test]
        public void RefreshingABranchThatLostItsChildrenLeavesNoRowIndexesPastTheEndOfTheList() =>
            RunWithMessagePump(tree =>
        {
            var connectionTreeModel = new ConnectionTreeModel();
            var root = new RootNodeInfo(RootNodeType.Connection);
            // Nested one level below the root: refreshing a root's direct child makes
            // RefreshObjects reassign Roots, which rebuilds the whole list from index 0 and
            // hides the defect being tested here.
            var parent = new ContainerInfo { Name = "parent" };
            var folder = new ContainerInfo { Name = "folder" };
            for (int i = 0; i < 25; i++)
                folder.AddChild(new ConnectionInfo { Name = $"conn{i:00}" });

            parent.AddChild(folder);
            root.AddChild(parent);
            connectionTreeModel.AddRootNode(root);
            tree.ConnectionTreeModel = connectionTreeModel;
            Application.DoEvents();

            tree.ExpandAll();
            Application.DoEvents();

            // Take the children away behind the control's back, then refresh the branch. This
            // drives TreeListView.Tree.RebuildChildren down the path where rows are removed and
            // the branch can no longer expand, so the map is rebuilt from the branch's own index
            // instead of from zero.
            foreach (ConnectionInfo child in new List<ConnectionInfo>(folder.Children.ConvertAll(c => (ConnectionInfo)c)))
                folder.RemoveChild(child);

            tree.RefreshObject(folder);
            Application.DoEvents();

            TreeListView.Tree model = tree.TreeModel;
            ArrayList objectList = GetPrivate<ArrayList>(model, "objectList");
            Dictionary<object, int> map = GetPrivate<Dictionary<object, int>>(model, "mapObjectToIndex");

            List<string> stale = [];
            foreach (KeyValuePair<object, int> entry in map)
            {
                if (entry.Value >= objectList.Count)
                    stale.Add($"{entry.Key} -> {entry.Value}");
            }

            Assert.That(stale, Is.Empty,
                        $"map points at rows past the end of a {objectList.Count} row list: "
                        + string.Join(", ", stale));
        });
    }
}
