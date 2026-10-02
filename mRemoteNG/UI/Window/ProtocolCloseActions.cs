using System;

namespace mRemoteNG.UI.Window
{
    /// <summary>
    /// A protocol thread can close its tab after the tab is already gone.
    /// That race stays inside this call.
    /// </summary>
    internal static class ProtocolCloseActions
    {
        public static void CloseTab(Action close)
        {
            try
            {
                close();
            }
            catch (ObjectDisposedException)
            {
            }
            catch (InvalidOperationException)
            {
            }
        }
    }
}
