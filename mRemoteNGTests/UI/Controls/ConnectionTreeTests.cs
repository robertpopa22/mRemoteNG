using System;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using mRemoteNG.Connection;
using mRemoteNG.Container;
using mRemoteNG.Tools.Clipboard;
using mRemoteNG.Tree;
using mRemoteNG.Tree.Root;
using mRemoteNG.UI.Controls.ConnectionTree;
using NSubstitute;
using NUnit.Framework;

namespace mRemoteNGTests.UI.Controls
{
	public class ConnectionTreeTests
	{
		/// <summary>
		/// Runs the given action on a dedicated STA thread with a WinForms message pump.
		/// Required because ConnectionTree inherits from TreeListView/ObjectListView
		/// which forces native Win32 handle creation in its constructor.
		/// On .NET (Core), this deadlocks without an active message pump on the owning STA thread.
		/// </summary>
		private static void RunWithMessagePump(Action<ConnectionTree> testAction)
		{
			Exception caught = null;
			var thread = new Thread(() =>
			{
				var form = new Form
				{
					Width = 400, Height = 300,
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
		public void FilteringIsRetainedAndUpdatedWhenNodeDeleted() => RunWithMessagePump(tree =>
		{
			var filter = new ConnectionTreeSearchTextFilter();
			var connectionTreeModel = new ConnectionTreeModel();
			var root = new RootNodeInfo(RootNodeType.Connection);
			var folder1 = new ContainerInfo {Name = "folder1"};
			var folder2 = new ContainerInfo {Name = "folder2"};
			var con1 = new ConnectionInfo {Name = "con1"};
			var con2 = new ConnectionInfo {Name = "con2"};
			var conDontShow = new ConnectionInfo {Name = "dontshowme" };
			root.AddChildRange(new []{folder1, folder2});
			folder1.AddChildRange(new []{con1, conDontShow});
			folder2.AddChild(con2);
			connectionTreeModel.AddRootNode(root);

			tree.ConnectionTreeModel = connectionTreeModel;
			Application.DoEvents();
			tree.ExpandAll();
			Application.DoEvents();

			filter.FilterText = "con";
			tree.ModelFilter = filter;
			Application.DoEvents();

			connectionTreeModel.DeleteNode(con1);
			Application.DoEvents();

			Assert.That(tree.IsFiltering, Is.True);
			Assert.That(tree.FilteredObjects, Does.Not.Contain(con1));
			Assert.That(tree.FilteredObjects, Does.Not.Contain(conDontShow));
			Assert.That(tree.FilteredObjects, Does.Contain(con2));
		});

		[Test]
		public void CannotAddConnectionToPuttySessionNode() => RunWithMessagePump(tree =>
		{
			var connectionTreeModel = new ConnectionTreeModel();
			var root = new RootNodeInfo(RootNodeType.Connection);
			var puttyRoot = new RootNodeInfo(RootNodeType.PuttySessions);
			connectionTreeModel.AddRootNode(root);
			connectionTreeModel.AddRootNode(puttyRoot);

			tree.ConnectionTreeModel = connectionTreeModel;
			Application.DoEvents();
			tree.ExpandAll();
			Application.DoEvents();

			tree.SelectedObject = puttyRoot;
			tree.AddConnection();

			Assert.That(puttyRoot.Children, Is.Empty);
		});

		[Test]
		public void CannotAddFolderToPuttySessionNode() => RunWithMessagePump(tree =>
		{
			var connectionTreeModel = new ConnectionTreeModel();
			var root = new RootNodeInfo(RootNodeType.Connection);
			var puttyRoot = new RootNodeInfo(RootNodeType.PuttySessions);
			connectionTreeModel.AddRootNode(root);
			connectionTreeModel.AddRootNode(puttyRoot);

			tree.ConnectionTreeModel = connectionTreeModel;
			Application.DoEvents();
			tree.ExpandAll();
			Application.DoEvents();

			tree.SelectedObject = puttyRoot;
			tree.AddFolder();

			Assert.That(puttyRoot.Children, Is.Empty);
		});

		[Test]
		public void CannotDuplicateRootConnectionNode() => RunWithMessagePump(tree =>
		{
			var connectionTreeModel = new ConnectionTreeModel();
			var root = new RootNodeInfo(RootNodeType.Connection);
			connectionTreeModel.AddRootNode(root);
			tree.ConnectionTreeModel = connectionTreeModel;
			Application.DoEvents();
			tree.ExpandAll();
			Application.DoEvents();

			tree.SelectedObject = root;
			tree.DuplicateSelectedNode();

			Assert.That(connectionTreeModel.RootNodes, Has.One.Items);
		});

		[Test]
		public void CanDuplicateConnectionNode() => RunWithMessagePump(tree =>
		{
			var connectionTreeModel = new ConnectionTreeModel();
			var root = new RootNodeInfo(RootNodeType.Connection);
			var con1 = new ConnectionInfo();
			root.AddChild(con1);
			connectionTreeModel.AddRootNode(root);
			tree.ConnectionTreeModel = connectionTreeModel;
			Application.DoEvents();
			tree.ExpandAll();
			Application.DoEvents();

			tree.SelectedObject = con1;
			tree.DuplicateSelectedNode();

			Assert.That(root.Children, Has.Exactly(2).Items);
		});

		[Test]
		public void CannotDuplicateRootPuttyNode() => RunWithMessagePump(tree =>
		{
			var connectionTreeModel = new ConnectionTreeModel();
			var puttyRoot = new RootNodeInfo(RootNodeType.PuttySessions);
			connectionTreeModel.AddRootNode(puttyRoot);
			tree.ConnectionTreeModel = connectionTreeModel;
			Application.DoEvents();
			tree.ExpandAll();
			Application.DoEvents();

			tree.SelectedObject = puttyRoot;
			tree.DuplicateSelectedNode();

			Assert.That(connectionTreeModel.RootNodes, Has.One.Items);
		});

		[Test]
		public void CannotDuplicatePuttyConnectionNode() => RunWithMessagePump(tree =>
		{
			var connectionTreeModel = new ConnectionTreeModel();
			var puttyRoot = new RootNodeInfo(RootNodeType.PuttySessions);
			var puttyConnection = new PuttySessionInfo();
			puttyRoot.AddChild(puttyConnection);
			connectionTreeModel.AddRootNode(puttyRoot);
			tree.ConnectionTreeModel = connectionTreeModel;
			Application.DoEvents();
			tree.ExpandAll();
			Application.DoEvents();

			tree.SelectedObject = puttyConnection;
			tree.DuplicateSelectedNode();

			Assert.That(puttyRoot.Children, Has.One.Items);
		});

		[Test]
		public void DuplicatingWithNoNodeSelectedDoesNothing() => RunWithMessagePump(tree =>
		{
			var connectionTreeModel = new ConnectionTreeModel();
			var puttyRoot = new RootNodeInfo(RootNodeType.PuttySessions);
			connectionTreeModel.AddRootNode(puttyRoot);
			tree.ConnectionTreeModel = connectionTreeModel;
			Application.DoEvents();
			tree.ExpandAll();
			Application.DoEvents();

			tree.SelectedObject = null;
			tree.DuplicateSelectedNode();

			Assert.That(connectionTreeModel.RootNodes, Has.One.Items);
		});

		[Test]
		public void ExpandingAllItemsUpdatesColumnWidthAppropriately() => RunWithMessagePump(tree =>
		{
			var connectionTreeModel = new ConnectionTreeModel();
			var root = new RootNodeInfo(RootNodeType.Connection);
			connectionTreeModel.AddRootNode(root);
			ContainerInfo parent = root;
			foreach (var i in Enumerable.Repeat("", 8))
			{
				var newContainer = new ContainerInfo {IsExpanded = false};
				parent.AddChild(newContainer);
				parent = newContainer;
			}

			tree.ConnectionTreeModel = connectionTreeModel;
			Application.DoEvents();

			var widthBefore = tree.Columns[0].Width;
			tree.ExpandAll();
			Application.DoEvents();
			var widthAfter = tree.Columns[0].Width;

			Assert.That(widthAfter, Is.GreaterThan(widthBefore));
		});

		[Test]
		public void RenamingNodeWithNothingSelectedDoesNothing() => RunWithMessagePump(tree =>
		{
			var connectionTreeModel = new ConnectionTreeModel();
			var root = new RootNodeInfo(RootNodeType.Connection);
			connectionTreeModel.AddRootNode(root);

			tree.ConnectionTreeModel = connectionTreeModel;
			Application.DoEvents();
			tree.ExpandAll();
			Application.DoEvents();
			tree.SelectedObject = null;

			Assert.DoesNotThrow(() => tree.RenameSelectedNode());
		});

		[Test]
		public void CopyHostnameCopiesTheHostnameOfTheSelectedConnection() => RunWithMessagePump(tree =>
		{
			var connectionTreeModel = new ConnectionTreeModel();
			var root = new RootNodeInfo(RootNodeType.Connection);
			var con1 = new ConnectionInfo {Hostname = "MyHost"};
			root.AddChild(con1);
			connectionTreeModel.AddRootNode(root);

			tree.ConnectionTreeModel = connectionTreeModel;
			Application.DoEvents();
			tree.ExpandAll();
			Application.DoEvents();
			tree.SelectedObject = con1;

			var clipboard = Substitute.For<IClipboard>();
			tree.CopyHostnameSelectedNode(clipboard);
			clipboard.Received(1).SetText(con1.Hostname);
		});

		[Test]
		public void CopyHostnameCopiesTheNodeNameOfTheSelectedContainer() => RunWithMessagePump(tree =>
		{
			var connectionTreeModel = new ConnectionTreeModel();
			var root = new RootNodeInfo(RootNodeType.Connection);
			var container = new ContainerInfo { Name = "MyFolder" };
			root.AddChild(container);
			connectionTreeModel.AddRootNode(root);

			tree.ConnectionTreeModel = connectionTreeModel;
			Application.DoEvents();
			tree.ExpandAll();
			Application.DoEvents();
			tree.SelectedObject = container;

			var clipboard = Substitute.For<IClipboard>();
			tree.CopyHostnameSelectedNode(clipboard);
			clipboard.Received(1).SetText(container.Name);
		});

		[Test]
		public void CopyHostnameDoesNotCopyAnythingIfNoNodeSelected() => RunWithMessagePump(tree =>
		{
			var connectionTreeModel = new ConnectionTreeModel();
			var root = new RootNodeInfo(RootNodeType.Connection);
			var con1 = new ConnectionInfo { Hostname = "MyHost" };
			root.AddChild(con1);
			connectionTreeModel.AddRootNode(root);

			tree.ConnectionTreeModel = connectionTreeModel;
			Application.DoEvents();
			tree.ExpandAll();
			Application.DoEvents();
			tree.SelectedObject = null;

			var clipboard = Substitute.For<IClipboard>();
			tree.CopyHostnameSelectedNode(clipboard);
			clipboard.DidNotReceiveWithAnyArgs().SetText("");
		});

		[Test]
		public void CopyHostnameDoesNotCopyAnythingIfHostnameOfSelectedConnectionIsEmpty() => RunWithMessagePump(tree =>
		{
			var connectionTreeModel = new ConnectionTreeModel();
			var root = new RootNodeInfo(RootNodeType.Connection);
			var con1 = new ConnectionInfo { Hostname = string.Empty };
			root.AddChild(con1);
			connectionTreeModel.AddRootNode(root);

			tree.ConnectionTreeModel = connectionTreeModel;
			Application.DoEvents();
			tree.ExpandAll();
			Application.DoEvents();
			tree.SelectedObject = con1;

			var clipboard = Substitute.For<IClipboard>();
			tree.CopyHostnameSelectedNode(clipboard);
			clipboard.DidNotReceiveWithAnyArgs().SetText("");
		});

		[Test]
		public void CopyHostnameDoesNotCopyAnythingIfNameOfSelectedContainerIsEmpty() => RunWithMessagePump(tree =>
		{
			var connectionTreeModel = new ConnectionTreeModel();
			var root = new RootNodeInfo(RootNodeType.Connection);
			var con1 = new ContainerInfo { Name = string.Empty};
			root.AddChild(con1);
			connectionTreeModel.AddRootNode(root);

			tree.ConnectionTreeModel = connectionTreeModel;
			Application.DoEvents();
			tree.ExpandAll();
			Application.DoEvents();
			tree.SelectedObject = con1;

			var clipboard = Substitute.For<IClipboard>();
			tree.CopyHostnameSelectedNode(clipboard);
			clipboard.DidNotReceiveWithAnyArgs().SetText("");
		});
	}
}
