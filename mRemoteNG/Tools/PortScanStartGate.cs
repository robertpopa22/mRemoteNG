using System;
using System.Collections.Generic;
using System.Net;
using mRemoteNG.Messages;

namespace mRemoteNG.Tools
{
    /// <summary>
    /// Decides whether a port scan may start, before the window changes the Scan button to Stop.
    /// A rejected scan names the reason and does not construct a running scanner.
    /// </summary>
    public static class PortScanStartGate
    {
        public static bool TryCreate(
            string addressText,
            IEnumerable<int> ports,
            int timeoutMilliseconds,
            int maxConcurrentHosts,
            out PortScanner? scanner,
            out string reason)
        {
            scanner = null;
            reason = "";

            if (!IpRangeParser.TryParse(addressText, out IPAddress? start, out IPAddress? end, out string ipError))
            {
                reason = ipError;
                return false;
            }

            try
            {
                scanner = new PortScanner(start!, end!, ports, timeoutMilliseconds, maxConcurrentHosts, SilentSink.Instance);
                return true;
            }
            catch (Exception ex)
            {
                scanner = null;
                reason = string.IsNullOrWhiteSpace(ex.Message) ? ex.GetType().Name : ex.Message;
                return false;
            }
        }

        private sealed class SilentSink : IOperationMessageSink
        {
            public static readonly SilentSink Instance = new();

            public void Information(string message, bool onlyLog = false)
            {
            }

            public void Warning(string message, bool onlyLog = false)
            {
            }

            public void Error(string message, bool onlyLog = false)
            {
            }

            public void Exception(string message, Exception exception, bool onlyLog = false)
            {
            }
        }
    }
}
