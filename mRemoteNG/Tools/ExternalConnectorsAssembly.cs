using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using mRemoteNG.Messages;
using mRemoteNG.Resources.Language;

namespace mRemoteNG.Tools
{
    /// <summary>
    /// Answers one question, once per process: can <c>ExternalConnectors.dll</c> be loaded?
    ///
    /// That assembly holds the password-vault connectors (LAPS, Secret Server, Passwordstate,
    /// 1Password, Password Safe, Vault/OpenBao, AWS EC2), and the connect path calls into it
    /// directly. When the file is gone the failure does not surface as a clear error: the JIT
    /// cannot compile the method that references those types, so the very first connection
    /// attempt dies with a bare <see cref="FileNotFoundException"/> and the app crashes
    /// (#175, #191, #192). The reporter of #192 found the cause on their machine: Windows
    /// Defender had quarantined the DLL, a plausible false positive for a library whose whole
    /// job is to read credentials and launch other programs' CLIs.
    ///
    /// The calls into the assembly now sit in methods of their own, so only a connection that
    /// uses a vault or the EC2 lookup ever needs it (<see cref="IsNeededBy"/>). For those, the
    /// probe here is made first, so a missing file becomes a message that says what is missing,
    /// where it was expected, and what most likely took it, instead of a crash report with no
    /// hint in it. A DLL that loaded once stays loaded for the
    /// life of the process (the image is mapped and cannot be removed under it), so the answer
    /// is cached.
    /// </summary>
    public static class ExternalConnectorsAssembly
    {
        public const string Name = "ExternalConnectors";

        private static readonly object Sync = new();
        private static bool? _available;
        private static string? _failure;

        /// <summary>Where the loader expects to find the file.</summary>
        public static string ExpectedPath => Path.Combine(AppContext.BaseDirectory, Name + ".dll");

        /// <summary>True when the assembly loads. Probed on first use, cached afterwards.</summary>
        public static bool IsAvailable
        {
            get
            {
                lock (Sync)
                {
                    if (_available is null)
                        _available = TryLoad(() => Assembly.Load(Name), out _failure);
                    return _available.Value;
                }
            }
        }

        /// <summary>
        /// Whether opening <paramref name="connection"/> calls into ExternalConnectors: an external
        /// credential provider on the connection or on its RD Gateway, the AWS EC2 hostname lookup,
        /// or a connection with no username that falls back to a default external provider. The
        /// connect paths only call into the assembly in exactly those cases (the calls sit in
        /// methods of their own), so any other connection opens normally without the file.
        /// <paramref name="force"/> is read the way the connect path reads it: an alternative
        /// address skips the EC2 lookup, and "connect without credentials" empties the username,
        /// which is what sends a connection to the default provider.
        /// </summary>
        public static bool IsNeededBy(mRemoteNG.Connection.ConnectionInfo connection,
                                      mRemoteNG.Connection.ConnectionInfo.Force force = mRemoteNG.Connection.ConnectionInfo.Force.None)
        {
            ArgumentNullException.ThrowIfNull(connection);

            if (connection.ExternalCredentialProvider != mRemoteNG.Connection.ExternalCredentialProvider.None)
                return true;
            if (connection.RDGatewayExternalCredentialProvider != mRemoteNG.Connection.ExternalCredentialProvider.None)
                return true;

            bool useAlternativeAddress = force.HasFlag(mRemoteNG.Connection.ConnectionInfo.Force.UseAlternativeAddress)
                                         && !string.IsNullOrWhiteSpace(connection.AlternativeAddress);
            if (!useAlternativeAddress && !string.IsNullOrEmpty(connection.EC2InstanceId))
                return true;

            bool noUsername = force.HasFlag(mRemoteNG.Connection.ConnectionInfo.Force.NoCredentials)
                              || string.IsNullOrEmpty(connection.Username);
            return noUsername
                && string.Equals(Properties.OptionsCredentialsPage.Default.EmptyCredentials, "custom", StringComparison.Ordinal)
                && string.IsNullOrEmpty(Properties.OptionsCredentialsPage.Default.DefaultUsername)
                && Properties.OptionsCredentialsPage.Default.ExternalCredentialProviderDefault != mRemoteNG.Connection.ExternalCredentialProvider.None;
        }

        /// <summary>
        /// Probes the assembly and, when it cannot be loaded, reports why through
        /// <paramref name="messageCollector"/>. Returns the availability so callers can bail out.
        /// </summary>
        public static bool EnsureAvailable(MessageCollector messageCollector)
        {
            ArgumentNullException.ThrowIfNull(messageCollector);

            if (IsAvailable)
                return true;

            messageCollector.AddMessage(MessageClass.ErrorMsg, DescribeCurrentFailure());
            return false;
        }

        /// <summary>
        /// Records a missing assembly in the log at startup, without a popup: most connections do
        /// not need it, so a user who uses no credential vault should not be interrupted at every
        /// start. The popup comes when a connection that does need it is opened.
        /// </summary>
        public static void LogIfUnavailable(MessageCollector messageCollector)
        {
            ArgumentNullException.ThrowIfNull(messageCollector);

            if (!IsAvailable)
                messageCollector.AddMessage(MessageClass.WarningMsg, DescribeCurrentFailure(), true);
        }

        private static string DescribeCurrentFailure()
        {
            string failure;
            lock (Sync)
            {
                failure = _failure ?? string.Empty;
            }

            return DescribeMissing(ExpectedPath, File.Exists(ExpectedPath), failure);
        }

        /// <summary>
        /// Runs <paramref name="load"/> and classifies the outcome. The three exceptions the
        /// loader raises for a file that is absent, unreadable or not a valid image mean "not
        /// available" and their text is kept for the report; anything else is a bug and is left
        /// to propagate.
        /// </summary>
        public static bool TryLoad(Func<Assembly> load, out string? failure)
        {
            ArgumentNullException.ThrowIfNull(load);

            try
            {
                _ = load();
                failure = null;
                return true;
            }
            catch (Exception ex) when (ex is FileNotFoundException or FileLoadException or BadImageFormatException)
            {
                failure = ex.Message;
                return false;
            }
        }

        /// <summary>
        /// The text shown to the user. Names the file and the folder it was expected in, and says
        /// whether the file is there at all, because that decides what the user should do next:
        /// a file that is gone points at quarantine; a file that is present but will not load
        /// points at a damaged or mismatched copy.
        /// </summary>
        public static string DescribeMissing(string expectedPath, bool fileExists, string? loaderMessage)
        {
            string state = fileExists
                ? Language.ExternalConnectorsPresentButUnloadable
                : Language.ExternalConnectorsFileGone;

            return string.Format(CultureInfo.CurrentCulture,
                                 Language.ExternalConnectorsMissing,
                                 Name + ".dll",
                                 expectedPath,
                                 state,
                                 string.IsNullOrWhiteSpace(loaderMessage) ? "-" : loaderMessage);
        }
    }
}
