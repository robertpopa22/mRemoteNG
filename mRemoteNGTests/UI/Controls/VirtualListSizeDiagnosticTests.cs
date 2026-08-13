using System;
using System.Collections.Generic;
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
    /// VirtualObjectListView.SetVirtualListSize swallows a failure to update the control's row
    /// count, which leaves it reporting a stale number while the data source has moved on -- the
    /// suspected source of the #149 expand crash. The failure has never been reproduced, so the
    /// code reports it instead of guessing at a fix. These cover the reporting contract: it must
    /// stay quiet while resizes are working, and it must never be able to break the control.
    /// </summary>
    [NonParallelizable]
    public class VirtualListSizeDiagnosticTests
    {
        private Action<string> _originalHook;

        [SetUp]
        public void SetUp() => _originalHook = VirtualObjectListView.SizeChangeDiagnostic;

        [TearDown]
        public void TearDown() => VirtualObjectListView.SizeChangeDiagnostic = _originalHook;

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

        private static ConnectionTreeModel BuildModel()
        {
            var connectionTreeModel = new ConnectionTreeModel();
            var root = new RootNodeInfo(RootNodeType.Connection);
            var folder = new ContainerInfo { Name = "folder" };
            for (int i = 0; i < 20; i++)
                folder.AddChild(new ConnectionInfo { Name = $"conn{i:00}" });

            root.AddChild(folder);
            connectionTreeModel.AddRootNode(root);
            return connectionTreeModel;
        }

        [Test]
        public void HealthyResizesReportNothing() => RunWithMessagePump(tree =>
        {
            List<string> reports = [];
            VirtualObjectListView.SizeChangeDiagnostic = reports.Add;

            tree.ConnectionTreeModel = BuildModel();
            Application.DoEvents();
            tree.ExpandAll();
            Application.DoEvents();
            tree.CollapseAll();
            Application.DoEvents();
            tree.ExpandAll();
            Application.DoEvents();

            // A report here means the control could not take the size it asked for, which is
            // exactly the condition being hunted -- it must not fire on ordinary use.
            Assert.That(reports, Is.Empty, string.Join(" | ", reports));
        });

        [Test]
        public void AFailingDiagnosticHookCannotBreakTheControl() => RunWithMessagePump(tree =>
        {
            VirtualObjectListView.SizeChangeDiagnostic = _ => throw new InvalidOperationException("hook blew up");

            Assert.DoesNotThrow(() =>
            {
                tree.ConnectionTreeModel = BuildModel();
                Application.DoEvents();
                tree.ExpandAll();
                Application.DoEvents();
            });
        });

        [Test]
        public void NoHookIsSafe() => RunWithMessagePump(tree =>
        {
            VirtualObjectListView.SizeChangeDiagnostic = null;

            Assert.DoesNotThrow(() =>
            {
                tree.ConnectionTreeModel = BuildModel();
                Application.DoEvents();
                tree.ExpandAll();
                Application.DoEvents();
            });
        });
    }
}
