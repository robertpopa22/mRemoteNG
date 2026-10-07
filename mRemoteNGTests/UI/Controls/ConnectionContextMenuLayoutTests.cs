using System;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using mRemoteNG.Connection;
using mRemoteNG.Connection.Protocol;
using mRemoteNG.Container;
using mRemoteNG.Tree;
using mRemoteNG.Tree.Root;
using mRemoteNG.UI.Controls;
using mRemoteNG.UI.Controls.ConnectionTree;
using NUnit.Framework;

namespace mRemoteNGTests.UI.Controls
{
    [TestFixture]
    public class ConnectionContextMenuLayoutTests
    {
        [Test]
        public void ConnectionMenu_ShowsTheShortRow_AndHidesFileCommands()
        {
            Run(tree =>
            {
                ConnectionInfo connection = new() { Name = "Alpha", Protocol = ProtocolType.RDP };
                ContainerInfo folder = new() { Name = "Folder" };
                RootNodeInfo root = new(RootNodeType.Connection);
                folder.AddChild(connection);
                root.AddChild(folder);
                ConnectionTreeModel model = new();
                model.AddRootNode(root);
                tree.ConnectionTreeModel = model;
                Application.DoEvents();
                tree.Expand(root);
                Application.DoEvents();
                tree.Expand(folder);
                Application.DoEvents();
                tree.SelectObject(connection, true);
                Application.DoEvents();
                Assert.That(tree.SelectedNode, Is.SameAs(connection),
                    () => $"selected '{tree.SelectedNode?.Name ?? "none"}' ({tree.SelectedNode?.GetType().Name ?? "none"}), count {tree.GetSelectedNodes().Count}");

                ConnectionContextMenu menu = (ConnectionContextMenu)tree.ContextMenuStrip;
                menu.ShowHideMenuItems();

                string[] visible = menu.Items.Cast<ToolStripItem>()
                    .Where(item => item.Available && item is not ToolStripSeparator)
                    .Select(item => item.Name)
                    .ToArray();

                Assert.That(visible, Is.EqualTo(new[]
                {
                    "_cMenTreeConnect",
                    "_cMenTreeConnectWithOptions",
                    "_cMenTreeReconnect",
                    "_cMenTreeDuplicate",
                    "_cMenTreeCopy",
                    "_cMenTreePaste",
                    "_cMenTreeRename",
                    "_cMenTreeDelete",
                    "_cMenTreeNew",
                    "_cMenTreeMore"
                }));
                Assert.That(menu.Items["_cMenTreePaste"].Enabled, Is.False);

                ToolStripMenuItem more = (ToolStripMenuItem)menu.Items["_cMenTreeMore"];
                string[] moreVisible = more.DropDownItems.Cast<ToolStripItem>()
                    .Where(item => item.Available)
                    .Select(item => item.Name)
                    .ToArray();
                Assert.That(moreVisible, Does.Contain("_cMenTreeFavorite"));
                Assert.That(moreVisible, Does.Contain("_cMenTreeCopyHostname"));
                Assert.That(moreVisible, Does.Not.Contain("_cMenTreeToolsTransferFile"));

                ToolStripMenuItem connectWithOptions = (ToolStripMenuItem)menu.Items["_cMenTreeConnectWithOptions"];
                Assert.That(connectWithOptions.DropDownItems["_cMenTreeClearCachedRdpCredentials"].Available, Is.True);
                Assert.That(connectWithOptions.DropDownItems["_cMenTreeWindowedFullscreen"].Available, Is.True);
                connection.Protocol = ProtocolType.SSH2;
                menu.ShowHideMenuItems();
                Assert.That(connectWithOptions.DropDownItems["_cMenTreeWindowedFullscreen"].Available, Is.False);
            });
        }

        [Test]
        public void RootMenu_ShowsFileCommands_AndHidesConnect()
        {
            Run(tree =>
            {
                RootNodeInfo root = new(RootNodeType.Connection);
                ConnectionTreeModel model = new();
                model.AddRootNode(root);
                tree.ConnectionTreeModel = model;
                Application.DoEvents();
                tree.SelectObject(root, true);
                Application.DoEvents();
                Assert.That(tree.SelectedNode, Is.SameAs(root),
                    () => $"selected '{tree.SelectedNode?.Name ?? "none"}' ({tree.SelectedNode?.GetType().Name ?? "none"}), count {tree.GetSelectedNodes().Count}");

                ConnectionContextMenu menu = (ConnectionContextMenu)tree.ContextMenuStrip;
                menu.ShowHideMenuItems();

                string[] visible = menu.Items.Cast<ToolStripItem>()
                    .Where(item => item.Available && item is not ToolStripSeparator)
                    .Select(item => item.Name)
                    .ToArray();

                Assert.That(visible, Does.Contain("_cMenTreeNew"));
                Assert.That(visible, Does.Contain("_cMenTreeImport"));
                Assert.That(visible, Does.Contain("_cMenTreeExportFile"));
                Assert.That(visible, Does.Contain("_cMenTreeExpandAll"));
                Assert.That(visible, Does.Contain("_cMenTreeOptions"));
                Assert.That(visible, Does.Not.Contain("_cMenTreeConnect"));
                Assert.That(visible, Does.Not.Contain("_cMenTreeMore"));
            });
        }

        private static void Run(Action<ConnectionTree> action)
        {
            Exception failure = null;
            Thread thread = new(() =>
            {
                using Form form = new()
                {
                    Width = 800,
                    Height = 600,
                    ShowInTaskbar = false,
                    StartPosition = FormStartPosition.Manual,
                    Location = new System.Drawing.Point(-12000, -12000)
                };
                form.Load += (_, _) =>
                {
                    bool previousReadOnly = mRemoteNG.Properties.OptionsDBsPage.Default.SQLReadOnly;
                    mRemoteNG.Properties.OptionsDBsPage.Default.SQLReadOnly = false;
                    try
                    {
                        ConnectionTree tree = new() { Dock = DockStyle.Fill };
                        form.Controls.Add(tree);
                        Application.DoEvents();
                        action(tree);
                    }
                    catch (Exception ex)
                    {
                        failure = ex;
                    }
                    finally
                    {
                        mRemoteNG.Properties.OptionsDBsPage.Default.SQLReadOnly = previousReadOnly;
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
                Assert.Fail("Connection context menu layout test did not finish within 30 seconds.");
            }

            if (failure != null)
                throw failure;
        }
    }
}
