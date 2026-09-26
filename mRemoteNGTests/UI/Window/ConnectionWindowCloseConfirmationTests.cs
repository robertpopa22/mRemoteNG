using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using mRemoteNG.Connection;
using mRemoteNG.Connection.Protocol;
using mRemoteNG.UI.Tabs;
using mRemoteNG.UI.Window;
using NUnit.Framework;
using WeifenLuo.WinFormsUI.Docking;

namespace mRemoteNGTests.UI.Window
{
    /// <summary>
    /// Closing the only tab of a panel with "ask for every connection" asked twice: the tab asked,
    /// then the panel -- auto-closed because it was empty -- asked again about the tab the user had
    /// just confirmed. The panel's question counted connDock.Documents, and DockPanelSuite only
    /// removes a tab from its panel on Form.Disposed, after the tab's whole Dispose has run. The
    /// panel's queued close runs in a message pump nested inside that Dispose (RDP teardown pumps),
    /// so the closing tab was still listed and still counted.
    /// </summary>
    [TestFixture]
    public class ConnectionWindowCloseConfirmationTests
    {
        private static void RunWithMessagePump(Action testAction)
        {
            Exception? caught = null;
            Thread thread = new(() =>
            {
                Form form = new()
                {
                    Width = 400,
                    Height = 300,
                    ShowInTaskbar = false,
                    StartPosition = FormStartPosition.Manual,
                    Location = new System.Drawing.Point(-10000, -10000)
                };

                form.Load += (_, _) =>
                {
                    try
                    {
                        testAction();
                    }
                    catch (Exception ex)
                    {
                        caught = ex;
                    }
                    finally
                    {
                        Application.ExitThread();
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

        private static int LiveConnectionTabCount(ConnectionWindow window)
        {
            MethodInfo method = typeof(ConnectionWindow).GetMethod("LiveConnectionTabCount", BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new AssertionException("ConnectionWindow.LiveConnectionTabCount not found");
            return (int)method.Invoke(window, null)!;
        }

        private static DockPanel ConnDock(ConnectionWindow window)
        {
            FieldInfo field = typeof(ConnectionWindow).GetField("connDock", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                ?? throw new AssertionException("ConnectionWindow.connDock not found");
            return field.GetValue(window) as DockPanel
                ?? throw new AssertionException("ConnectionWindow.connDock is null");
        }

        [Test]
        public void ATabBeingDisposedIsStillListedButNotCountedAsAConnection() => RunWithMessagePump(() =>
        {
            Form hostForm = new()
            {
                Width = 800,
                Height = 600,
                ShowInTaskbar = false,
                StartPosition = FormStartPosition.Manual,
                Location = new System.Drawing.Point(-10000, -10000)
            };
            DockPanel hostDockPanel = new()
            {
                Dock = DockStyle.Fill,
                DocumentStyle = DocumentStyle.DockingWindow,
                Theme = new VS2015LightTheme()
            };
            hostForm.Controls.Add(hostDockPanel);

            // Keep the panel open once its tab is gone: this test is about what the panel counts,
            // and must never reach a real close confirmation.
            Type optionsType = typeof(ConnectionWindow).Assembly.GetType("mRemoteNG.Properties.OptionsTabsPanelsPage")
                ?? throw new AssertionException("OptionsTabsPanelsPage not found");
            object options = optionsType.GetProperty("Default", BindingFlags.Static | BindingFlags.Public)?.GetValue(null)
                ?? throw new AssertionException("OptionsTabsPanelsPage.Default not found");
            PropertyInfo autoClose = optionsType.GetProperty("AutoClosePanelOnLastTabClose")
                ?? throw new AssertionException("AutoClosePanelOnLastTabClose not found");
            object? previousAutoClose = autoClose.GetValue(options);
            autoClose.SetValue(options, false);

            try
            {
                hostForm.Show();
                Application.DoEvents();

                using ConnectionWindow window = new(new DockContent(), "Close confirmation test");
                window.Show(hostDockPanel, DockState.Document);
                Application.DoEvents();

                ConnectionTab tab = window.AddConnectionTab(new ConnectionInfo { Name = "Conn1", Protocol = ProtocolType.RDP }, switchToConnection: false)
                    ?? throw new AssertionException("AddConnectionTab returned null");
                Application.DoEvents();

                Assert.That(LiveConnectionTabCount(window), Is.EqualTo(1), "an open tab is a connection");

                // A child disposed during the tab's own Dispose stands in for the RDP control whose
                // teardown pumps messages -- the moment the panel's queued close used to run.
                int listedWhileDisposing = -1;
                int countedWhileDisposing = -1;
                Control child = new();
                child.Disposed += (_, _) =>
                {
                    if (!tab.Disposing) return;
                    listedWhileDisposing = ConnDock(window).Documents.Count();
                    countedWhileDisposing = LiveConnectionTabCount(window);
                };
                tab.Controls.Add(child);

                tab.Dispose();
                Application.DoEvents();

                Assert.Multiple(() =>
                {
                    Assert.That(listedWhileDisposing, Is.EqualTo(1),
                                "precondition: DockPanelSuite still lists a tab while it is being disposed");
                    Assert.That(countedWhileDisposing, Is.EqualTo(0),
                                "a tab being disposed must not count as a connection the panel would close");
                    Assert.That(LiveConnectionTabCount(window), Is.EqualTo(0), "a disposed tab is not a connection");
                });
            }
            finally
            {
                autoClose.SetValue(options, previousAutoClose);
                hostForm.Dispose();
            }
        });
    }
}
