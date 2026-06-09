using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using WinClipboard = System.Windows.Forms.Clipboard;

namespace mRemoteNG.Tools.Clipboard
{
    /// <summary>
    /// Represents the Windows system clipboard
    /// </summary>
    public class WindowsClipboard : IClipboard
    {
        private const int SecretClearMilliseconds = 30_000;
        private static readonly System.Threading.Lock _clearTimerLock = new();
        private static System.Windows.Forms.Timer? _clearTimer;

        public string GetText() => WinClipboard.GetText();

        public void SetText(string text) => WinClipboard.SetText(text);

        public void SetSecret(string text)
        {
            DataObject data = new();
            data.SetText(text);
            // Opt out of clipboard managers / Win+V history / cloud sync so the secret
            // is not retained beyond this single paste.
            MarkExcluded(data, "ExcludeClipboardContentFromMonitorProcessing");
            MarkExcluded(data, "CanIncludeInClipboardHistory");
            MarkExcluded(data, "CanUploadToCloudClipboard");
            WinClipboard.SetDataObject(data, true);
            ScheduleClear(text);
        }

        private static void MarkExcluded(DataObject data, string format)
        {
            // A zero DWORD signals "no" for the history/cloud formats; for the
            // monitor-processing format the mere presence of the format is enough.
            data.SetData(format, new MemoryStream(new byte[] { 0, 0, 0, 0 }));
        }

        private static void ScheduleClear(string secret)
        {
            lock (_clearTimerLock)
            {
                _clearTimer?.Stop();
                _clearTimer?.Dispose();
                _clearTimer = new System.Windows.Forms.Timer { Interval = SecretClearMilliseconds };
                _clearTimer.Tick += (_, _) =>
                {
                    lock (_clearTimerLock)
                    {
                        _clearTimer?.Stop();
                        _clearTimer?.Dispose();
                        _clearTimer = null;
                    }
                    try
                    {
                        // Only clear if our secret is still on the clipboard - don't clobber
                        // something the user copied in the meantime.
                        if (WinClipboard.ContainsText() &&
                            string.Equals(WinClipboard.GetText(), secret, StringComparison.Ordinal))
                            WinClipboard.Clear();
                    }
                    catch (ExternalException)
                    {
                        // Clipboard locked by another process; leave it. Worst case the secret
                        // lingers until the user copies something else.
                    }
                };
                _clearTimer.Start();
            }
        }
    }
}
