using System;
using System.Reflection;
using mRemoteNG.App;
using mRemoteNG.Connection;
using mRemoteNG.Tree.Smart;
using NUnit.Framework;

namespace mRemoteNGTests.Tree.Smart
{
    /// <summary>
    /// #227: every connections load builds a new tree with new smart group nodes. A node that stays
    /// subscribed to a process-lifetime event keeps its whole tree alive, so frequent SQL sync
    /// reloads accumulated one tree per reload until a 32-bit process ran out of memory.
    /// </summary>
    [TestFixture]
    public class SmartGroupNodeLifetimeTests
    {
        private static int SubscriberCount(object source, string eventFieldName)
        {
            FieldInfo? field = source.GetType().GetField(eventFieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"event backing field {eventFieldName} not found");
            return (field!.GetValue(source) as Delegate)?.GetInvocationList().Length ?? 0;
        }

        [Test]
        public void ConnectedNodeDoesNotSubscribeToTheProcessWideLoadEvent()
        {
            int before = SubscriberCount(Runtime.ConnectionsService, nameof(Runtime.ConnectionsService.ConnectionsLoaded));

            var node = new ConnectedGroupNode();
            node.Initialize();

            Assert.That(SubscriberCount(Runtime.ConnectionsService, nameof(Runtime.ConnectionsService.ConnectionsLoaded)),
                        Is.EqualTo(before),
                        "a ConnectedGroupNode subscribed to ConnectionsService.ConnectionsLoaded, which outlives its tree");
            node.Detach();
        }

        [Test]
        public void RecentNodeStopsListeningAfterDetach()
        {
            RecentConnectionsService service = RecentConnectionsService.Instance;
            int before = SubscriberCount(service, nameof(RecentConnectionsService.RecentConnectionsChanged));

            var node = new RecentGroupNode();
            node.Initialize();
            Assert.That(SubscriberCount(service, nameof(RecentConnectionsService.RecentConnectionsChanged)),
                        Is.EqualTo(before + 1));

            node.Detach();
            Assert.That(SubscriberCount(service, nameof(RecentConnectionsService.RecentConnectionsChanged)),
                        Is.EqualTo(before),
                        "a detached RecentGroupNode is still reachable from the process-lifetime recent list");
        }
    }
}
