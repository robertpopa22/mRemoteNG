namespace mRemoteNG.Tools.Clipboard
{
    /// <summary>
    /// An abstraction of an operating system clipboard where
    /// data can be placed on and taken off the clipboard.
    /// </summary>
    public interface IClipboard
    {
        string GetText();
        void SetText(string text);

        /// <summary>
        /// Places sensitive text (e.g. a password) on the clipboard with reduced
        /// exposure: excluded from Windows clipboard history (Win+V) and cloud
        /// sync, and automatically cleared after a short timeout.
        /// </summary>
        void SetSecret(string text);
    }
}
