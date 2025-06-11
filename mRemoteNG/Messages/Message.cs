using System;

namespace mRemoteNG.Messages
{
    public class Message(MessageClass messageClass, string messageText, bool onlyLog = false) : IMessage
    {
        public MessageClass Class { get; set; } = messageClass;
        public string Text { get; set; } = messageText;
        public DateTime Date { get; set; } = DateTime.Now;
        public bool OnlyLog { get; set; } = onlyLog;
    }
}