using System.Collections.Generic;
using System.Threading;

namespace mRemoteNG.Messages.MessageWriters
{
    /// <summary>
    /// The single ordering point between the threads that report messages and the UI thread
    /// that renders them. Panel order is enqueue order.
    /// </summary>
    internal sealed class NotificationMessageQueue
    {
        private readonly Lock _lock = new();
        private readonly Queue<IMessage> _queue = new();
        private bool _drainScheduled;

        /// <summary>
        /// Queues a message. True when the caller must schedule a drain. False when one is
        /// already scheduled and will take this message with it.
        /// </summary>
        public bool Enqueue(IMessage message)
        {
            lock (_lock)
            {
                _queue.Enqueue(message);

                if (_drainScheduled)
                    return false;

                _drainScheduled = true;
                return true;
            }
        }

        /// <summary>
        /// Takes the next message, oldest first. An empty queue also gives up the drain,
        /// so those two steps cannot leave a newly queued message with nothing scheduled.
        /// </summary>
        public IMessage? Dequeue()
        {
            lock (_lock)
            {
                if (_queue.Count == 0)
                {
                    _drainScheduled = false;
                    return null;
                }

                return _queue.Dequeue();
            }
        }

        public void ReleaseDrain()
        {
            lock (_lock)
                _drainScheduled = false;
        }

        public void Clear()
        {
            lock (_lock)
            {
                _queue.Clear();
                _drainScheduled = false;
            }
        }

        public int Count
        {
            get
            {
                lock (_lock)
                    return _queue.Count;
            }
        }
    }
}
