using System;
using System.Linq;
using System.Security;
using System.Runtime.Versioning;
using mRemoteNG.App;
using mRemoteNG.Connection;
using mRemoteNG.Tools;
using mRemoteNG.Tree.Root;

namespace mRemoteNG.Security
{
    /// <summary>
    /// Re-authentication gate for actions that expose a stored secret in the clear
    /// (copy-to-clipboard, reveal). Honors the connection-file threat model: a prompt
    /// is only meaningful when the user has set a custom master password. When no
    /// custom password is set the file is encrypted with the public legacy key
    /// (<see cref="ConnectionFileDefaults.LegacyEncryptionKey"/>), so prompting for a
    /// value that is effectively public would be security theater — the gate passes
    /// through in that case.
    /// </summary>
    [SupportedOSPlatform("windows")]
    public static class MasterPasswordGate
    {
        /// <summary>
        /// Collects the master password from the user. Replaceable so tests can drive the
        /// gate without showing the modal <see cref="MiscTools.PasswordDialog"/>.
        /// </summary>
        internal static Func<string?, Optional<SecureString>> PasswordPrompt { get; set; } =
            label => MiscTools.PasswordDialog(label, false);

        /// <summary>
        /// Returns <c>true</c> if the caller may proceed to expose a secret.
        /// If a custom master password is set, the user is prompted and must enter it
        /// correctly; otherwise (default key) the gate passes through.
        /// </summary>
        /// <param name="connectionInfo">
        /// The connection whose secret is being exposed. Used to resolve the correct
        /// root node (and thus the correct master password) when more than one
        /// connection file is loaded. When null, the first connection root is used.
        /// </param>
        /// <param name="label">Optional label shown in the prompt (e.g. the item name).</param>
        public static bool VerifyMasterPasswordIfSet(ConnectionInfo? connectionInfo = null, string? label = null)
        {
            // Resolve the root the connection actually belongs to, so a custom password
            // on a secondary loaded file is honored instead of falling back to the
            // primary root (which could leave the action ungated).
            RootNodeInfo? root = connectionInfo?.GetRootParent() as RootNodeInfo
                ?? Runtime.ConnectionsService.ConnectionTreeModel?.RootNodes
                    .OfType<RootNodeInfo>()
                    .FirstOrDefault(node => node.Type == RootNodeType.Connection);

            // No custom master password -> nothing real to verify against.
            if (root is not { Password: true })
                return true;

            Optional<SecureString> password = PasswordPrompt(label);
            if (!password.Any() || password.First().Length == 0)
                return false;

            return root.IsPasswordMatch(password.First());
        }
    }
}
