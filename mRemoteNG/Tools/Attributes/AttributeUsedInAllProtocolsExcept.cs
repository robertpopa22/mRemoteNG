using System;
using System.Linq;
using mRemoteNG.Connection.Protocol;

namespace mRemoteNG.Tools.Attributes
{
    public class AttributeUsedInAllProtocolsExcept(params ProtocolType[] exceptions) : AttributeUsedInProtocol(Enum
                .GetValues(typeof(ProtocolType))
                .Cast<ProtocolType>()
                .Except(exceptions)
                .ToArray())
    {
    }
}
