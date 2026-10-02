using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using mRemoteNG.Messages;
using mRemoteNG.Messages.MessageWriters;
using NUnit.Framework;

namespace mRemoteNGTests.Messages
{
    [TestFixture]
    public class NotificationMessageQueueTests
    {
        private NotificationMessageQueue _queue = null!;

        [SetUp]
        public void Setup() => _queue = new NotificationMessageQueue();

        private static Message Msg(string text) => new(MessageClass.InformationMsg, text);

        private List<string> DrainAll()
        {
            List<string> drained = [];
            while (_queue.Dequeue() is { } message)
                drained.Add(message.Text);
            return drained;
        }

        [Test]
        public void MessagesComeOutInTheOrderTheyWentIn()
        {
            _queue.Enqueue(Msg("first"));
            _queue.Enqueue(Msg("second"));
            _queue.Enqueue(Msg("third"));

            Assert.That(DrainAll(), Is.EqualTo(new[] { "first", "second", "third" }));
        }

        [Test]
        public void AMessageQueuedDuringADrainKeepsItsPlace()
        {
            _queue.Enqueue(Msg("background"));
            Assert.That(_queue.Dequeue()!.Text, Is.EqualTo("background"));

            Assert.That(_queue.Enqueue(Msg("from the ui thread")), Is.False);
            Assert.That(_queue.Dequeue()!.Text, Is.EqualTo("from the ui thread"));
        }

        [Test]
        public void OnlyTheFirstMessageAsksForADrain()
        {
            Assert.That(_queue.Enqueue(Msg("one")), Is.True);
            Assert.That(_queue.Enqueue(Msg("two")), Is.False);
            Assert.That(_queue.Enqueue(Msg("three")), Is.False);
        }

        [Test]
        public void EmptyingTheQueueLetsTheNextMessageScheduleADrain()
        {
            _queue.Enqueue(Msg("one"));
            DrainAll();

            Assert.That(_queue.Enqueue(Msg("two")), Is.True);
        }

        [Test]
        public void ReleasingADrainLetsTheNextMessageScheduleOne()
        {
            _queue.Enqueue(Msg("one"));
            _queue.ReleaseDrain();

            Assert.That(_queue.Enqueue(Msg("two")), Is.True);
            Assert.That(DrainAll(), Is.EqualTo(new[] { "one", "two" }));
        }

        [Test]
        public void ClearingDiscardsTheQueueAndTheDrain()
        {
            _queue.Enqueue(Msg("one"));
            _queue.Enqueue(Msg("two"));
            _queue.Clear();

            Assert.That(_queue.Count, Is.Zero);
            Assert.That(_queue.Enqueue(Msg("three")), Is.True);
        }

        [Test]
        public void AnEmptyQueueHandsBackNothing()
        {
            Assert.That(_queue.Dequeue(), Is.Null);
            Assert.That(_queue.Count, Is.Zero);
        }

        [Test]
        public void ConcurrentReportersLoseNothingAndScheduleOneDrain()
        {
            const int threads = 8;
            int scheduleRequests = 0;

            Parallel.For(0, threads, t =>
            {
                for (int i = 0; i < 50; i++)
                {
                    if (_queue.Enqueue(Msg($"{t}:{i}")))
                        System.Threading.Interlocked.Increment(ref scheduleRequests);
                }
            });

            Assert.That(scheduleRequests, Is.EqualTo(1));
            Assert.That(_queue.Count, Is.EqualTo(threads * 50));

            ConcurrentBag<string> drained = [];
            Parallel.For(0, 4, _ =>
            {
                while (_queue.Dequeue() is { } message)
                    drained.Add(message.Text);
            });

            Assert.That(drained.Distinct().Count(), Is.EqualTo(threads * 50));
        }
    }
}
