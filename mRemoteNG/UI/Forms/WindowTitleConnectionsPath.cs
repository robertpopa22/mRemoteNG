using System.IO;

namespace mRemoteNG.UI.Forms
{
    internal static class WindowTitleConnectionsPath
    {
        public static string For(string? connectionFileName, bool showCompletePath)
        {
            if (string.IsNullOrEmpty(connectionFileName))
                return string.Empty;
            if (!showCompletePath)
                return Path.GetFileName(connectionFileName);
            return Path.GetFullPath(connectionFileName);
        }
    }
}
