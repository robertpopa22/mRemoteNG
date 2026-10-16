using mRemoteNG.Messages;
using NSubstitute;
using NUnit.Framework;


namespace mRemoteNGTests.Messages
{
    public class MessageCollectorTests
    {
        private MessageCollector _messageCollector;

        [SetUp]
        public void Setup()
        {
            _messageCollector = new MessageCollector();
        }

        [Test]
        public void InitializesWithNoMessages()
        {
            Assert.That(_messageCollector.Messages, Is.Empty);
        }

        [Test]
        public void MessageAddedToList()
        {
            var message = Substitute.For<IMessage>();
            _messageCollector.AddMessage(message);
            Assert.That(_messageCollector.Messages, Does.Contain(message));
        }

        [Test]
        public void ListCanBeCleared()
        {
            var message = Substitute.For<IMessage>();
            _messageCollector.AddMessage(message);
            _messageCollector.ClearMessages();
            Assert.That(_messageCollector.Messages, Is.Empty);
        }

        [Test]
        public void NotifiedWhenMessageAdded()
        {
            var wasCalled = false;
            _messageCollector.CollectionChanged += (sender, args) => wasCalled = true;
            var message = Substitute.For<IMessage>();
            _messageCollector.AddMessage(message);
            Assert.That(wasCalled, Is.True);
        }

        [Test]
        public void BatchAddAddsAllItems()
        {
            var msg1 = Substitute.For<IMessage>();
            var msg2 = Substitute.For<IMessage>();
            var msgCollection = new[] {msg1, msg2};
            _messageCollector.AddMessages(msgCollection);
            Assert.That(_messageCollector.Messages, Is.EquivalentTo(msgCollection));
        }

        [Test]
        public void OneNotificationRaisedForBatchAdd()
        {
            var notificationCount = 0;
            _messageCollector.CollectionChanged += (sender, args) => notificationCount++;
            var msg1 = Substitute.For<IMessage>();
            var msg2 = Substitute.For<IMessage>();
            _messageCollector.AddMessages(new[] { msg1, msg2 });
            Assert.That(notificationCount, Is.EqualTo(1));
        }

        [Test]
        public void TheListIsTrimmedToTheCapKeepingTheNewestMessages()
        {
            for (int i = 0; i < 10_005; i++)
                _messageCollector.AddMessage(MessageClass.InformationMsg, "m" + i, true);

            var all = System.Linq.Enumerable.ToList(_messageCollector.Messages);
            Assert.That(all, Has.Count.EqualTo(10_000));
            Assert.That(all[0].Text, Is.EqualTo("m5"));
            Assert.That(all[^1].Text, Is.EqualTo("m10004"));
        }

        [Test]
        public void ANonListBackingStoreIsTrimmedOneAtATime()
        {
            var field = typeof(MessageCollector).GetField("_messageList",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var store = new System.Collections.ObjectModel.Collection<IMessage>();
            field.SetValue(_messageCollector, store);

            for (int i = 0; i < 10_003; i++)
                _messageCollector.AddMessage(MessageClass.InformationMsg, "m" + i, true);

            Assert.That(store, Has.Count.EqualTo(10_000));
            Assert.That(store[0].Text, Is.EqualTo("m3"));
        }
    }
}
