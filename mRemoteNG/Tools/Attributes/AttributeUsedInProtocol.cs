using System;
using mRemoteNG.Connection.Protocol;

namespace mRemoteNG.Tools.Attributes
{
    [AttributeUsage(AttributeTargets.Property)]
    public class AttributeUsedInProtocol(params ProtocolType[] supportedProtocolTypes) : Attribute
    {
        public ProtocolType[] SupportedProtocolTypes { get; } = supportedProtocolTypes;
    }
}
