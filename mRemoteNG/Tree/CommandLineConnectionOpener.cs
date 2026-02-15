using System;
using System.Linq;
using System.Runtime.Versioning;
using mRemoteNG.App;
using mRemoteNG.Connection;
using mRemoteNG.Container;
using mRemoteNG.Messages;
using mRemoteNG.Tools.Cmdline;
using mRemoteNG.UI.Controls.ConnectionTree;

namespace mRemoteNG.Tree
{
    [SupportedOSPlatform("windows")]
    public class CommandLineConnectionOpener : IConnectionTreeDelegate
    {
        private readonly IConnectionInitiator _connectionInitiator;

        public CommandLineConnectionOpener(IConnectionInitiator connectionInitiator)
        {
            _connectionInitiator = connectionInitiator ?? throw new ArgumentNullException(nameof(connectionInitiator));
        }

        public void Execute(IConnectionTree connectionTree)
        {
            string? connectTo = StartupArgumentsInterpreter.ConnectTo;
            if (string.IsNullOrEmpty(connectTo))
                return;

            var allConnections = connectionTree.GetRootConnectionNode()
                .GetRecursiveChildList()
                .Where(node => node is not ContainerInfo);

            // Try exact name match first (case-insensitive)
            ConnectionInfo? match = allConnections
                .FirstOrDefault(c => string.Equals(c.Name, connectTo, StringComparison.OrdinalIgnoreCase));

            // Fall back to ConstantID match
            match ??= allConnections
                .FirstOrDefault(c => string.Equals(c.ConstantID, connectTo, StringComparison.OrdinalIgnoreCase));

            if (match != null)
            {
                Runtime.MessageCollector.AddMessage(MessageClass.InformationMsg,
                    $"Auto-connecting to \"{match.Name}\" (--connect argument)");
                _connectionInitiator.OpenConnection(match);
            }
            else
            {
                Runtime.MessageCollector.AddMessage(MessageClass.WarningMsg,
                    $"--connect: connection \"{connectTo}\" not found");
            }
        }
    }
}
