namespace mRemoteNG.UI.Forms.OptionsPages
{
    /// <summary>
    /// Text the Advanced page shows for the PuTTY executable this app found.
    /// </summary>
    internal static class DetectedPuttyCaption
    {
        public static string For(string? detectedPath, bool fileExists, string detectedLabel, string notDetected)
        {
            if (string.IsNullOrWhiteSpace(detectedPath) || !fileExists)
                return notDetected;

            return detectedLabel + " " + detectedPath;
        }
    }
}
