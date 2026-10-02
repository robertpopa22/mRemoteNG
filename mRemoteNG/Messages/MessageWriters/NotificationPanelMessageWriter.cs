using System;
using System.Runtime.Versioning;
using System.Windows.Forms;
using mRemoteNG.UI;
using mRemoteNG.UI.Window;

namespace mRemoteNG.Messages.MessageWriters
{
    [SupportedOSPlatform("windows")]
    public class NotificationPanelMessageWriter(ErrorAndInfoWindow messageWindow) : IMessageWriter
    {
        private readonly ErrorAndInfoWindow _messageWindow = messageWindow ?? throw new ArgumentNullException(nameof(messageWindow));
        private readonly NotificationMessageQueue _queue = new();

        public void Write(IMessage message)
        {
            if (_messageWindow.lvErrorCollector.IsDisposed)
                return;

            if (_queue.Enqueue(message))
                ScheduleDrain();
        }

        private void ScheduleDrain()
        {
            ListView list = _messageWindow.lvErrorCollector;

            // The panel starts auto-hidden. Its handle is created when the user first opens it,
            // which is after startup messages have already been reported. They stay queued.
            if (!list.IsHandleCreated)
            {
                list.HandleCreated += OnHandleCreated;

                if (!list.IsHandleCreated)
                    return;

                list.HandleCreated -= OnHandleCreated;
            }

            PostDrain(list);
        }

        private void OnHandleCreated(object? sender, EventArgs e)
        {
            ListView list = _messageWindow.lvErrorCollector;
            list.HandleCreated -= OnHandleCreated;

            // HandleCreated runs before the list pushes its columns. Posting the drain puts the
            // items in after that, so the text column exists.
            PostDrain(list);
        }

        private void PostDrain(ListView list)
        {
            try
            {
                list.BeginInvoke((MethodInvoker)Drain);
            }
            catch (InvalidOperationException)
            {
                _queue.ReleaseDrain();
            }
        }

        private void Drain()
        {
            while (true)
            {
                if (_messageWindow.lvErrorCollector.IsDisposed)
                {
                    _queue.Clear();
                    return;
                }

                IMessage? message = _queue.Dequeue();
                if (message is null)
                    return;

                _messageWindow.AddMessage(new NotificationMessageListViewItem(message));
            }
        }
    }
}
