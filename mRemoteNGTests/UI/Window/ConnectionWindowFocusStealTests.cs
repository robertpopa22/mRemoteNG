using System;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using mRemoteNG.UI.Window;
using NUnit.Framework;
using WeifenLuo.WinFormsUI.Docking;

namespace mRemoteNGTests.UI.Window
{
    /// <summary>
    /// #143 took five reporter test cycles and four wrong fixes. The cause was ours: the docking
    /// library raises ActiveContentChanged as a focus *refresh*, not only on a real tab switch, and
    /// our handler answered every one of them by calling Protocol.Focus(). Clicking the tree search
    /// box therefore made the application hand keyboard focus straight back to the RDP session --
    /// the reclaim and the thief were the same code.
    ///
    /// The fix gates the refocus on the active content actually changing identity. These tests lock
    /// that gate directly, with no RDP session involved, so the regression cannot come back quietly:
    /// an end-to-end check needs a live remote host and an interactive desktop, which no CI agent
    /// reliably has.
    ///
    /// The tracker must also mirror ActiveContent verbatim -- including transitions to null when the
    /// dock empties. An earlier version of the fix updated it only on the happy path, which froze it
    /// on a departed tab and silently skipped the refocus after a tab was moved out of the panel and
    /// back. That defect was caught in review, not by a test; now it is covered.
    /// </summary>
    [TestFixture]
    public class ConnectionWindowFocusStealTests
    {
        private static void RunWithMessagePump(Action<ConnectionWindow> testAction)
        {
            Exception? caught = null;
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

                form.Load += (_, _) =>
                {
                    ConnectionWindow? window = null;
                    try
                    {
                        window = new ConnectionWindow(new DockContent(), "Focus Steal Test");
                        testAction(window);
                    }
                    catch (Exception ex)
                    {
                        caught = ex;
                    }
                    finally
                    {
                        try { window?.Dispose(); } catch { /* teardown only */ }
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

        private static FieldInfo TrackerField()
        {
            FieldInfo? field = typeof(ConnectionWindow).GetField(
                "_lastActivatedContent", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null,
                        "_lastActivatedContent is gone — the #143 identity gate has been changed or "
                        + "removed; re-verify that a focus refresh cannot steal the search box.");
            return field!;
        }

        private static void RaiseActiveContentChanged(ConnectionWindow window)
        {
            MethodInfo? handler = typeof(ConnectionWindow).GetMethod(
                "ConnDockOnActiveContentChanged", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(handler, Is.Not.Null, "ConnDockOnActiveContentChanged not found");
            handler!.Invoke(window, [window, EventArgs.Empty]);
        }

        [Test]
        public void TheHandlerTracksActiveContentSoARefreshCanBeToldFromASwitch() =>
            RunWithMessagePump(window =>
            {
                FieldInfo tracker = TrackerField();

                // A handler invocation must always leave the tracker equal to the dock's current
                // active content. If it does not, "changed" cannot be distinguished from "the
                // library refreshed focus", which is exactly the bug.
                RaiseActiveContentChanged(window);

                object? dock = typeof(ConnectionWindow)
                    .GetField("connDock", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.GetValue(window);
                Assert.That(dock, Is.Not.Null, "connDock not found");

                object? activeContent = dock!.GetType()
                    .GetProperty("ActiveContent")?.GetValue(dock);

                Assert.That(tracker.GetValue(window), Is.SameAs(activeContent),
                            "the tracker must mirror ActiveContent after every event, including "
                            + "when it is null — otherwise a tab moved out and back compares equal "
                            + "and never regains focus");
            });

        [Test]
        public void AnEmptyDockResetsTheTrackerInsteadOfFreezingOnTheDepartedTab() =>
            RunWithMessagePump(window =>
            {
                FieldInfo tracker = TrackerField();

                // Simulate the tracker still pointing at a tab that has since left the panel.
                tracker.SetValue(window, new DockContent());

                RaiseActiveContentChanged(window);

                // With no content in the dock the tracker has to fall back to null. Leaving the
                // stale reference is what made "move tab to another panel and back" skip the
                // refocus.
                Assert.That(tracker.GetValue(window), Is.Null);
            });

        [Test]
        public void RepeatedRefreshesOfTheSameContentAreIdempotent() =>
            RunWithMessagePump(window =>
            {
                FieldInfo tracker = TrackerField();

                // The docking library can raise this many times for one user action. Every one of
                // them must leave the same state; only a genuine content change may act.
                RaiseActiveContentChanged(window);
                object? first = tracker.GetValue(window);

                RaiseActiveContentChanged(window);
                RaiseActiveContentChanged(window);

                Assert.That(tracker.GetValue(window), Is.SameAs(first));
            });
    }
}
