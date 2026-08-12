using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Versioning;

// ReSharper disable ArrangeAccessorOwnerBody

namespace mRemoteNG.Messages
{
    [SupportedOSPlatform("windows")]
    public class MessageCollector : INotifyCollectionChanged
    {
        private const int MaxMessages = 10_000;
        private readonly IList<IMessage> _messageList;
        private readonly object _listLock = new();

        public IEnumerable<IMessage> Messages => _messageList;

        public MessageCollector()
        {
            _messageList = new List<IMessage>();
        }

        public void AddMessage(MessageClass messageClass, string messageText, bool onlyLog = false)
        {
            Message message = new(messageClass, messageText, onlyLog);
            AddMessage(message);
        }

        public void AddMessage(IMessage message)
        {
            AddMessages(new[] {message});
        }

        public void AddMessages(IEnumerable<IMessage> messages)
        {
            List<IMessage> newMessages = new();

            // Messages arrive from background workers (e.g. the port scanner's scan threads) as well as
            // the UI thread, so the backing list must not be mutated concurrently.
            lock (_listLock)
            {
                foreach (IMessage message in messages)
                {
                    _messageList.Add(message);
                    newMessages.Add(message);
                }

                // Prevent unbounded growth in long-running sessions. Trim in one shot: removing from
                // the front one item at a time shifts the whole list on every message once the cap is
                // reached, which is a large cost under a flood of messages.
                int excess = _messageList.Count - MaxMessages;
                if (excess > 0 && _messageList is List<IMessage> backingList)
                    backingList.RemoveRange(0, excess);
                else
                    while (_messageList.Count > MaxMessages)
                        _messageList.RemoveAt(0);
            }

            if (newMessages.Count > 0)
                RaiseCollectionChangedEvent(NotifyCollectionChangedAction.Add, newMessages);
        }

        public void AddExceptionMessage(string message, Exception ex, MessageClass msgClass = MessageClass.ErrorMsg, bool logOnly = true)
        {
            AddMessage(msgClass, message + Environment.NewLine + Tools.MiscTools.GetExceptionMessageRecursive(ex),
                       logOnly);
        }

        public void AddExceptionStackTrace(string message, Exception ex, MessageClass msgClass = MessageClass.ErrorMsg, bool logOnly = true)
        {
            AddMessage(msgClass, message + Environment.NewLine + ex.Message + Environment.NewLine + ex.Demystify().StackTrace,
                       logOnly);
        }

        public void ClearMessages()
        {
            lock (_listLock)
                _messageList.Clear();
        }

        public event NotifyCollectionChangedEventHandler? CollectionChanged;

        private void RaiseCollectionChangedEvent(NotifyCollectionChangedAction action, IList items)
        {
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(action, items));
        }
    }
}