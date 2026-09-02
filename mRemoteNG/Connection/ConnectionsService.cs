using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using mRemoteNG.App;
using mRemoteNG.App.Info;
using mRemoteNG.Config;
using mRemoteNG.Config.Connections;
using mRemoteNG.Config.Connections.Multiuser;
using mRemoteNG.Config.DataProviders;
using mRemoteNG.Config.Putty;
using mRemoteNG.Connection.Protocol;
using mRemoteNG.Container;
using mRemoteNG.Messages;
using mRemoteNG.Security;
using mRemoteNG.Tools;
using mRemoteNG.Tree;
using mRemoteNG.Tree.Root;
using mRemoteNG.UI;
using mRemoteNG.Resources.Language;
using System.Runtime.Versioning;
using mRemoteNG.UI.Forms;
using mRemoteNG.UI.Window;
using mRemoteNG.Config.DatabaseConnectors;
using mRemoteNG.Config.Serializers.ConnectionSerializers.Sql;
using mRemoteNG.Config.Serializers.Versioning;
using mRemoteNG.Security.SymmetricEncryption;

namespace mRemoteNG.Connection
{
    [SupportedOSPlatform("windows")]
    public class ConnectionsService(PuttySessionsManager puttySessionsManager)
    {
        private static readonly Lock SaveLock = new();
        private static readonly CompositeFormat ConnectionFileAlreadyOpenFormat = CompositeFormat.Parse("Connection file '{0}' is already open.");
        private readonly PuttySessionsManager _puttySessionsManager = puttySessionsManager ?? throw new ArgumentNullException(nameof(puttySessionsManager));
        private readonly IDataProvider<string> _localConnectionPropertiesDataProvider = new FileDataProvider(Path.Combine(SettingsFileInfo.SettingsPath, SettingsFileInfo.LocalConnectionProperties));
        private readonly LocalConnectionPropertiesXmlSerializer _localConnectionPropertiesSerializer = new LocalConnectionPropertiesXmlSerializer();
        private bool _batchingSaves;
        private readonly Lock _debounceTriggerLock = new();
        private string? _debouncedPropertyNameTrigger;
        private bool _saveRequested;
        private bool _saveAsyncRequested;
        private System.Threading.Timer? _saveDebounceTimer;
        private const int SaveDebounceMs = 2000;
        // Cached SQL custom encryption password — avoids re-prompting on every reload (#1646)
        private SecureString? _cachedSqlEncryptionPassword;

        public bool IsConnectionsFileLoaded { get; set; }

        /// <summary>
        /// True when the loaded tree has changed since the last save (or load). Used by the
        /// autosave timer to skip saves when nothing changed — an unconditional periodic save
        /// rewrote the connections file and stamped a new .backup every interval, flooding the
        /// Settings folder (and any sync service watching it) with identical copies.
        /// Runtime-only properties (see the #83 filter) do not count as changes.
        /// </summary>
        public bool HasUnsavedChanges { get; private set; }

        public bool UsingDatabase { get; private set; }
        public string? ConnectionFileName { get; private set; }
        public RemoteConnectionsSyncronizer? RemoteConnectionsSyncronizer { get; set; }
        public DateTime LastSqlUpdate { get; set; }
		public DateTime LastFileUpdate { get; set; }

        public ConnectionTreeModel? ConnectionTreeModel { get; private set; }

        public void NewConnectionsFile(string filename)
        {
            try
            {
                filename.ThrowIfNullOrEmpty(nameof(filename));
                BackUpBeforeReplacing(filename);
                ConnectionTreeModel newConnectionsModel = new();
                newConnectionsModel.AddRootNode(new RootNodeInfo(RootNodeType.Connection));
                SaveConnections(newConnectionsModel, false, new SaveFilter(), filename, true);
                LoadConnections(false, false, filename);
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector.AddExceptionMessage(Language.CouldNotCreateNewConnectionsFile, ex);
            }
        }

        /// <summary>
        /// Copies an existing connections file aside before <see cref="NewConnectionsFile"/> writes an
        /// empty tree over it. Starting a new file is offered as recovery when loading failed, and the
        /// reason for that failure is not always the file (a plugin assembly that would not load was
        /// reported the same way, #175) — so the answer must never be a silently destroyed file. This
        /// runs regardless of the user's backup settings, because it is the last copy of that data.
        /// </summary>
        internal static void BackUpBeforeReplacing(string filename)
        {
            if (!File.Exists(filename))
                return;

            try
            {
                string backupPath = $"{filename}.{DateTime.Now.ToString("yyyyMMdd-HHmmssfff", CultureInfo.InvariantCulture)}.replaced.backup";
                if (File.Exists(backupPath))
                    backupPath = $"{filename}.{Guid.NewGuid():N}.replaced.backup";

                File.Copy(filename, backupPath);
                Runtime.MessageCollector.AddMessage(MessageClass.WarningMsg,
                                                    string.Format(CultureInfo.CurrentCulture,
                                                                  Language.ConnectionsFileReplacedBackupCreated,
                                                                  filename, backupPath));
            }
            catch (Exception ex)
            {
                // Losing the backup is not a reason to lose the choice the user made, but it is a
                // reason to say so loudly.
                Runtime.MessageCollector.AddExceptionMessage(Language.ConnectionsFileBackupFailed, ex,
                                                             MessageClass.WarningMsg);
            }
        }

        public static ConnectionInfo? CreateQuickConnect(string connectionString, ProtocolType protocol)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(connectionString))
                {
                    Runtime.MessageCollector.AddMessage(MessageClass.WarningMsg, Language.QuickConnectNoHostname);
                    return null;
                }

                // Extract RDP-specific flags before parsing host/port.
                // Supported flags: -ra[:true|false]  (UseRestrictedAdmin)
                //                  -rcg[:true|false] (UseRemoteCredentialGuard)
                // Example: "myserver -ra:false -rcg:false"
                bool? rdpRestrictedAdminOverride = null;
                bool? rdpRcgOverride = null;

                if (connectionString.Contains(' '))
                {
                    string[] parts = connectionString.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    connectionString = parts[0];

                    foreach (string part in parts.Skip(1))
                    {
                        if (part.Equals("-ra", StringComparison.OrdinalIgnoreCase) ||
                            part.Equals("-ra:true", StringComparison.OrdinalIgnoreCase))
                            rdpRestrictedAdminOverride = true;
                        else if (part.Equals("-ra:false", StringComparison.OrdinalIgnoreCase))
                            rdpRestrictedAdminOverride = false;
                        else if (part.Equals("-rcg", StringComparison.OrdinalIgnoreCase) ||
                                 part.Equals("-rcg:true", StringComparison.OrdinalIgnoreCase))
                            rdpRcgOverride = true;
                        else if (part.Equals("-rcg:false", StringComparison.OrdinalIgnoreCase))
                            rdpRcgOverride = false;
                    }
                }

                UriBuilder uriBuilder = new()
                {
                    Scheme = "dummyscheme"
                };
                string explicitUsername = string.Empty;

                if (connectionString.Contains('@'))
                {
                    string[] x = connectionString.Split('@');
                    explicitUsername = x[0];
                    connectionString = x[1];
                }
                if (connectionString.Contains(':'))
                {
                    string[] x = connectionString.Split(':');
                    connectionString = x[0];
                    uriBuilder.Port = Convert.ToInt32(x[1], CultureInfo.InvariantCulture);
                }

                uriBuilder.Host = connectionString;

                ConnectionInfo newConnectionInfo = new();
                newConnectionInfo.CopyFrom(DefaultConnectionInfo.Instance);

                newConnectionInfo.Name = Properties.OptionsTabsPanelsPage.Default.IdentifyQuickConnectTabs
                    ? string.Format(CultureInfo.InvariantCulture, Language.Quick, connectionString)
                    : connectionString;

                newConnectionInfo.Protocol = protocol;
                newConnectionInfo.Hostname = connectionString;
                if (!string.IsNullOrWhiteSpace(explicitUsername))
                {
                    newConnectionInfo.Username = explicitUsername;
                }

                if (uriBuilder.Port == -1)
                {
                    newConnectionInfo.SetDefaultPort();
                }
                else
                {
                    newConnectionInfo.Port = uriBuilder.Port;
                }

                if (string.IsNullOrEmpty(newConnectionInfo.Panel))
                {
                    // Use the currently active panel instead of hardcoding "General" (#1682)
                    if (FrmMain.IsCreated && FrmMain.Default.pnlDock.ActiveDocument is ConnectionWindow activeCw)
                        newConnectionInfo.Panel = activeCw.TabText;
                    else
                        newConnectionInfo.Panel = "General";
                }

                newConnectionInfo.IsQuickConnect = true;

                // Apply RDP-specific flag overrides (only meaningful for RDP protocol)
                if (protocol == ProtocolType.RDP)
                {
                    if (rdpRestrictedAdminOverride.HasValue)
                        newConnectionInfo.UseRestrictedAdmin = rdpRestrictedAdminOverride.Value;
                    if (rdpRcgOverride.HasValue)
                        newConnectionInfo.UseRCG = rdpRcgOverride.Value;
                }

                return newConnectionInfo;
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector.AddExceptionMessage(Language.QuickConnectFailed, ex);
                return null;
            }
        }

        public void LoadAdditionalConnectionFile(string filename)
        {
            if (string.IsNullOrEmpty(filename)) return;

            try
            {
                // Prevent opening the same file twice (#2331)
                if (ConnectionTreeModel != null &&
                    ConnectionTreeModel.RootNodes.OfType<RootNodeInfo>()
                        .Any(r => string.Equals(r.Filename, filename, StringComparison.OrdinalIgnoreCase)))
                {
                    Runtime.MessageCollector.AddMessage(MessageClass.WarningMsg,
                        string.Format(CultureInfo.InvariantCulture, ConnectionFileAlreadyOpenFormat, filename));
                    return;
                }

                IConnectionsLoader connectionLoader = new XmlConnectionsLoader(filename);
                ConnectionTreeModel? loadedModel = connectionLoader.Load();

                if (loadedModel == null) return;

                if (ConnectionTreeModel == null)
                {
                    LoadConnections(false, false, filename);
                }
                else
                {
                    foreach (ContainerInfo root in loadedModel.RootNodes)
                    {
                        if (root is RootNodeInfo rni && string.IsNullOrEmpty(rni.Filename))
                        {
                            rni.Filename = filename;
                        }
                        ConnectionTreeModel.AddRootNode(root);
                    }
                }

                PersistAdditionalFileList();
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector.AddExceptionMessage(string.Format(CultureInfo.InvariantCulture, Language.LoadFromXmlFailed, filename), ex);
            }
        }

        public void CloseAdditionalConnectionFile(RootNodeInfo rootNode)
        {
            if (ConnectionTreeModel == null || rootNode == null) return;
            if (ConnectionTreeModel.RootNodes.Count <= 1) return;

            // Don't allow closing the primary connection file
            if (string.Equals(rootNode.Filename, ConnectionFileName, StringComparison.OrdinalIgnoreCase))
                return;

            ConnectionTreeModel.RemoveRootNode(rootNode);
            PersistAdditionalFileList();
        }

        public void LoadAdditionalConnectionFiles()
        {
            string saved = Properties.OptionsConnectionsPage.Default.AdditionalConnectionFiles;
            if (string.IsNullOrWhiteSpace(saved)) return;

            string[] files = saved.Split('|', StringSplitOptions.RemoveEmptyEntries);
            foreach (string file in files)
            {
                string expanded = Environment.ExpandEnvironmentVariables(file.Trim());
                if (File.Exists(expanded))
                {
                    LoadAdditionalConnectionFile(expanded);
                }
            }
        }

        private void PersistAdditionalFileList()
        {
            if (ConnectionTreeModel == null)
            {
                Properties.OptionsConnectionsPage.Default.AdditionalConnectionFiles = "";
                Properties.OptionsConnectionsPage.Default.Save();
                return;
            }

            var additionalFiles = ConnectionTreeModel.RootNodes
                .OfType<RootNodeInfo>()
                .Where(r => !string.IsNullOrEmpty(r.Filename) &&
                            !string.Equals(r.Filename, ConnectionFileName, StringComparison.OrdinalIgnoreCase) &&
                            r.Type == RootNodeType.Connection)
                .Select(r => r.Filename)
                .Distinct(StringComparer.OrdinalIgnoreCase);

            Properties.OptionsConnectionsPage.Default.AdditionalConnectionFiles = string.Join("|", additionalFiles);
            Properties.OptionsConnectionsPage.Default.Save();
        }

        /// <summary>
        /// Load connections from a source. <see cref="connectionFileName"/> is ignored if
        /// <see cref="useDatabase"/> is true.
        /// </summary>
        /// <param name="useDatabase"></param>
        /// <param name="import"></param>
        /// <param name="connectionFileName"></param>
        public void LoadConnections(bool useDatabase, bool import, string connectionFileName)
        {
            Stopwatch diagnosticsStopwatch = Stopwatch.StartNew();
            ConnectionTreeModel? oldConnectionTreeModel = ConnectionTreeModel;
            bool oldIsUsingDatabaseValue = UsingDatabase;

            IConnectionsLoader connectionLoader;
            if (useDatabase)
            {
                IDatabaseConnector dbConnector = DatabaseConnectorFactory.DatabaseConnectorFromSettings();
                SqlDataProvider sqlDataProvider = new(dbConnector);
                SqlDatabaseMetaDataRetriever metaDataRetriever = new();
                SqlDatabaseVersionVerifier versionVerifier = new(dbConnector);
                bool triedCached = false;
                connectionLoader = new SqlConnectionsLoader(
                    _localConnectionPropertiesSerializer,
                    _localConnectionPropertiesDataProvider,
                    dbConnector,
                    sqlDataProvider,
                    metaDataRetriever,
                    versionVerifier,
                    new LegacyRijndaelCryptographyProvider(),
                    (filename) =>
                    {
                        // Return cached password on first call (avoids re-prompting on every reload — #1646)
                        if (_cachedSqlEncryptionPassword != null && !triedCached)
                        {
                            triedCached = true;
                            return new Optional<SecureString>(_cachedSqlEncryptionPassword);
                        }
                        // Cached password was wrong or not set — clear cache and prompt
                        _cachedSqlEncryptionPassword?.Dispose();
                        _cachedSqlEncryptionPassword = null;
                        Optional<SecureString> result = MiscTools.PasswordDialog(filename, false);
                        if (result.Any())
                            _cachedSqlEncryptionPassword = result.First();
                        return result;
                    });
            }
            else
            {
                connectionLoader = new XmlConnectionsLoader(connectionFileName);
            }

            ConnectionTreeModel newConnectionTreeModel = null!;
            try
            {
                newConnectionTreeModel = connectionLoader.Load();
                if (useDatabase)
                {
                    LastSqlUpdate = DateTime.Now.ToUniversalTime();
                    TrySaveSqlConnectionsCache(newConnectionTreeModel);
                }
            }
            catch (Exception ex) when (useDatabase)
            {
                string cachePath = Path.Combine(SettingsFileInfo.SettingsPath, SettingsFileInfo.SqlConnectionsCache);
                if (File.Exists(cachePath))
                {
                    Runtime.MessageCollector.AddMessage(MessageClass.WarningMsg,
                        $"Could not load connections from database ({ex.Message}). Loading from local cache in read-only mode.");
                    connectionLoader = new XmlConnectionsLoader(cachePath);
                    newConnectionTreeModel = connectionLoader.Load();
                }
                else
                {
                    throw;
                }
            }

            if (newConnectionTreeModel == null)
            {
                RuntimeDiagnostics.ConnectionLoad(useDatabase, import, 0,
                    diagnosticsStopwatch.ElapsedMilliseconds, "failed");
                DialogFactory.ShowLoadConnectionsFailedDialog(connectionFileName, "Decrypting connection file failed", IsConnectionsFileLoaded);
                return;
            }

            IsConnectionsFileLoaded = true;
            ConnectionFileName = connectionFileName;
            Properties.OptionsConnectionsPage.Default.ConnectionFilePath = connectionFileName;
            Properties.OptionsConnectionsPage.Default.Save();

            UsingDatabase = useDatabase;

            if (!import)
            {
                _puttySessionsManager.AddSessions();
                newConnectionTreeModel.RootNodes.AddRange(_puttySessionsManager.RootPuttySessionsNodes);
            }
            
            // Set Filename on root nodes if not set
            if (!useDatabase)
            {
                foreach (var root in newConnectionTreeModel.RootNodes.OfType<RootNodeInfo>())
                {
                     if (string.IsNullOrEmpty(root.Filename)) root.Filename = connectionFileName;
                }
            }

            ConnectionTreeModel = newConnectionTreeModel;
            UpdateCustomConsPathSetting(connectionFileName);
            RaiseConnectionsLoadedEvent(oldConnectionTreeModel is not null ? new Optional<ConnectionTreeModel>(oldConnectionTreeModel) : new Optional<ConnectionTreeModel>(), newConnectionTreeModel, oldIsUsingDatabaseValue, useDatabase, connectionFileName);
            Runtime.MessageCollector.AddMessage(MessageClass.DebugMsg, $"Connections loaded using {connectionLoader.GetType().Name}");
            RuntimeDiagnostics.ConnectionLoad(useDatabase, import,
                newConnectionTreeModel.GetRecursiveChildList().Count,
                diagnosticsStopwatch.ElapsedMilliseconds, "success");
        }

        /// <summary>
        /// When turned on, calls to <see cref="SaveConnections()"/> or
        /// <see cref="SaveConnectionsAsync"/> will not immediately execute.
        /// Instead, they will be deferred until <see cref="EndBatchingSaves"/>
        /// is called.
        /// </summary>
        public void BeginBatchingSaves()
        {
            _batchingSaves = true;
        }

        /// <summary>
        /// Immediately executes a single <see cref="SaveConnections()"/> or
        /// <see cref="SaveConnectionsAsync"/> if one has been requested
        /// since calling <see cref="BeginBatchingSaves"/>.
        /// </summary>
        public void EndBatchingSaves()
        {
            _batchingSaves = false;

            // Clear the request flags before dispatching. They used to survive the batch, so
            // the first batched operation that asked for an async save turned every later
            // EndBatchingSaves into an async (debounced) save regardless of what that batch
            // actually requested, and every batch fired a save even when nothing had changed.
            // Tree moves, duplicates and deletes all run inside a batch, which is why this
            // leaked into ordinary use. (#148)
            bool asyncRequested = _saveAsyncRequested;
            bool saveRequested = _saveRequested;
            _saveAsyncRequested = false;
            _saveRequested = false;

            if (asyncRequested)
                SaveConnectionsAsync();
            else if (saveRequested)
                SaveConnections();
        }

		/// <summary>
		/// All calls to <see cref="SaveConnections()"/> or <see cref="SaveConnectionsAsync"/>
		/// will be deferred until the returned <see cref="DisposableAction"/> is disposed.
		/// Once disposed, this will immediately executes a single <see cref="SaveConnections()"/>
		/// or <see cref="SaveConnectionsAsync"/> if one has been requested.
		/// Place this call in a 'using' block to represent a batched saving context.
		/// </summary>
		/// <returns></returns>
		public DisposableAction BatchedSavingContext()
        {
			return new DisposableAction(BeginBatchingSaves, EndBatchingSaves);
        }

        /// <summary>
        /// Saves the currently loaded <see cref="ConnectionTreeModel"/> with
        /// no <see cref="SaveFilter"/>.
        /// </summary>
        public void SaveConnections()
        {
            if (ConnectionTreeModel is null || ConnectionFileName is null)
                return;
            SaveConnections(ConnectionTreeModel, UsingDatabase, new SaveFilter(), ConnectionFileName);
        }

        /// <summary>
        /// Saves the given <see cref="ConnectionTreeModel"/>.
        /// If <see cref="useDatabase"/> is true, <see cref="connectionFileName"/> is ignored
        /// </summary>
        /// <param name="connectionTreeModel"></param>
        /// <param name="useDatabase"></param>
        /// <param name="saveFilter"></param>
        /// <param name="connectionFileName"></param>
        /// <param name="forceSave">Bypasses safety checks that prevent saving if a connection file isn't loaded.</param>
        /// <param name="propertyNameTrigger">
        /// Optional. The name of the property that triggered
        /// this save.
        /// </param>
        public void SaveConnections(ConnectionTreeModel connectionTreeModel, bool useDatabase, SaveFilter saveFilter, string connectionFileName, bool forceSave = false, string propertyNameTrigger = "")
        {
            if (connectionTreeModel == null)
                return;

            if (!forceSave && !IsConnectionsFileLoaded)
                return;

            if (_batchingSaves)
            {
                _saveRequested = true;
                return;
            }

            // Clear the dirty flag up front: edits arriving while the save serializes re-arm it,
            // so a change racing the save is re-saved on the next autosave tick instead of being
            // considered already-persisted (clearing after the save would swallow it).
            HasUnsavedChanges = false;

            Stopwatch diagnosticsStopwatch = Stopwatch.StartNew();
            bool diagnosticsSuccess = false;
            int diagnosticsNodeCount = connectionTreeModel.GetRecursiveChildList().Count;
            try
            {
                Runtime.MessageCollector.AddMessage(MessageClass.InformationMsg, "Saving connections...");
                RemoteConnectionsSyncronizer?.Disable();

                bool previouslyUsingDatabase = UsingDatabase;

                if (useDatabase)
                {
                    ISaver<ConnectionTreeModel> saver = (ISaver<ConnectionTreeModel>)new SqlConnectionsSaver(saveFilter, _localConnectionPropertiesSerializer, _localConnectionPropertiesDataProvider);
                    saver.Save(connectionTreeModel, propertyNameTrigger);
                    LastSqlUpdate = DateTime.Now.ToUniversalTime();
                }
                else
                {
                    // XML Saving with support for multiple roots/files
                    foreach (var rootNode in connectionTreeModel.RootNodes.OfType<RootNodeInfo>())
                    {
                        // PuTTY sessions are read-only (imported from registry) — never save them
                        // to disk. Without this check, PuTTY root (which has an empty Filename)
                        // would overwrite the main connections file with only PuTTY data.
                        if (rootNode.Type == Tree.Root.RootNodeType.PuttySessions)
                            continue;

                        string targetFile = rootNode.Filename;
                        if (string.IsNullOrEmpty(targetFile)) targetFile = connectionFileName;

                        // If Save As is detected (connectionFileName arg != ConnectionFileName prop), 
                        // and this is the "main" root (checked by Filename matching ConnectionFileName or being empty),
                        // then redirect to the new connectionFileName.
                        if (connectionFileName != ConnectionFileName && (rootNode.Filename == ConnectionFileName || string.IsNullOrEmpty(rootNode.Filename)))
                        {
                            targetFile = connectionFileName;
                            // Optionally update the root's filename to the new one?
                            // rootNode.Filename = connectionFileName; // Side effect?
                        }

                        var tempModel = new ConnectionTreeModel();
                        tempModel.AddRootNode(rootNode);

                        ISaver<ConnectionTreeModel> saver = new XmlConnectionsSaver(targetFile, saveFilter);
                        saver.Save(tempModel, propertyNameTrigger);
                        
                        if (targetFile == connectionFileName && File.Exists(connectionFileName))
                             LastFileUpdate = File.GetLastWriteTimeUtc(connectionFileName);
                    }
                }

                UsingDatabase = useDatabase;
                ConnectionFileName = connectionFileName;
                RaiseConnectionsSavedEvent(connectionTreeModel, previouslyUsingDatabase, UsingDatabase, connectionFileName);
                Runtime.MessageCollector.AddMessage(MessageClass.InformationMsg, "Successfully saved connections");
                diagnosticsSuccess = true;
            }
            catch (Exception ex)
            {
                HasUnsavedChanges = true; // save failed — keep the model marked dirty for retry
                Runtime.MessageCollector?.AddExceptionMessage(string.Format(CultureInfo.InvariantCulture, Language.ConnectionsFileCouldNotSaveAs, connectionFileName), ex, logOnly: false);
            }
            finally
            {
                RemoteConnectionsSyncronizer?.Enable();
                RuntimeDiagnostics.ConnectionSave(useDatabase,
                    !string.IsNullOrEmpty(propertyNameTrigger), diagnosticsNodeCount,
                    diagnosticsStopwatch.ElapsedMilliseconds, diagnosticsSuccess ? "success" : "failed");
            }
        }

        /// <summary>
        /// Save the currently loaded connections asynchronously
        /// </summary>
        /// <param name="propertyNameTrigger">
        /// Optional. The name of the property that triggered
        /// this save.
        /// </param>
        public void SaveConnectionsAsync(string propertyNameTrigger = "")
        {
            if (_batchingSaves)
            {
                _saveAsyncRequested = true;
                return;
            }

            // Debounce: reset the timer on each call so that rapid-fire PropertyChanged
            // events (e.g. from HostStatusMonitor or bulk edits) coalesce into a single
            // save instead of queuing N independent saves — each of which re-encrypts
            // every password with PBKDF2 at 600K iterations. See issue #83.
            // Coalescing must not let a local-only property mask a database-relevant one. The
            // callback used to close over whichever trigger arrived last, so a root rename
            // followed within the debounce window by an OpenConnections / IsExpanded / Favorite
            // change saved under that second name — and SqlConnectionsSaver skips the database
            // entirely for local-only triggers, dropping the rename with only a debug line. When
            // a window coalesces different triggers, report none, which is never local-only. (#148)
            lock (_debounceTriggerLock)
            {
                if (_saveDebounceTimer == null)
                    _debouncedPropertyNameTrigger = propertyNameTrigger;
                else if (!string.Equals(_debouncedPropertyNameTrigger, propertyNameTrigger, StringComparison.Ordinal))
                    _debouncedPropertyNameTrigger = "";
            }

            _saveDebounceTimer?.Dispose();

            // Hold the multiuser reload off for the length of the debounce, not just for the
            // save itself. SaveConnections disables the syncronizer on entry and re-enables it
            // in its finally, but that leaves this waiting window unguarded: the reload timer
            // could fire inside it and swap ConnectionTreeModel for a freshly loaded one, and
            // because the callback below re-reads the model when it fires rather than capturing
            // it, the pending edit was then serialized away silently — no error, no log. (#148)
            RemoteConnectionsSyncronizer?.Disable();
            _saveDebounceTimer = new System.Threading.Timer(_ =>
            {
                string coalescedTrigger;
                lock (_debounceTriggerLock)
                {
                    coalescedTrigger = _debouncedPropertyNameTrigger ?? propertyNameTrigger;
                    _debouncedPropertyNameTrigger = null;
                    _saveDebounceTimer?.Dispose();
                    _saveDebounceTimer = null;
                }

                ConnectionTreeModel? treeModel = ConnectionTreeModel;
                string? fileName = ConnectionFileName;
                if (treeModel is null || fileName is null)
                {
                    // Nothing to save, so SaveConnections' finally will not run: resume syncing
                    // here or the reload timer stays off for the rest of the session.
                    RemoteConnectionsSyncronizer?.Enable();
                    return;
                }

                lock (SaveLock)
                {
                    SaveConnections(treeModel, UsingDatabase, new SaveFilter(), fileName, propertyNameTrigger: coalescedTrigger);
                }
            }, null, SaveDebounceMs, Timeout.Infinite);
        }

        public static string GetStartupConnectionFileName() =>
            GetStartupConnectionFileName(UI.Forms.FrmChooseConnectionsFile.Prompt);

        internal static string GetStartupConnectionFileName(
            Func<IReadOnlyList<ConnectionsFileResolver.Candidate>,
                 ConnectionsFileResolver.Candidate?,
                 (ConnectionsFileResolver.Candidate? Choice, bool RememberChoice)> promptFactory)
        {
            // Command-line /cons: or /c: override (session-only, not persisted)
            if (!string.IsNullOrWhiteSpace(Tools.Cmdline.StartupArgumentsInterpreter.CustomConnectionFile))
            {
                return Tools.Cmdline.StartupArgumentsInterpreter.CustomConnectionFile;
            }

            // Always enumerate candidates before deciding, even when a
            // ConnectionFilePath is saved in Options. The saved path is treated as
            // one candidate among others: if it is still the only one that exists
            // we return it silently; if newer files exist in other well-known
            // locations we offer the picker. Short-circuiting on the saved path
            // meant users who once picked a path would never be told about a newer
            // file showing up alongside it, which is the regression #95 reported.
            IReadOnlyList<ConnectionsFileResolver.Candidate> candidates = ConnectionsFileResolver.DiscoverCandidates();
            bool userCancelled = false;
            if (candidates.Count > 0)
            {
                ConnectionsFileResolver.Candidate? chosen = ConnectionsFileResolver.Resolve(candidates, promptFactory);
                if (chosen is not null) return chosen.Path;
                // Null after a real prompt = user clicked Cancel. Treat that as
                // "don't auto-load anything from a remembered path" — skip the
                // saved-path fallback below and use the edition default. Without
                // this skip, Cancel would silently load whatever is in
                // ConnectionFilePath, which is exactly the "Cancel still runs the
                // previous session" behaviour the user reported.
                userCancelled = true;
            }

            // No candidate files on disk (or user explicitly cancelled the picker)
            // — fall back to the saved path only when the user did NOT cancel
            // (create-on-first-save path for fresh boxes with a custom setting),
            // and only then to the edition default.
            if (!userCancelled &&
                !string.IsNullOrWhiteSpace(Properties.OptionsConnectionsPage.Default.ConnectionFilePath))
            {
                return Environment.ExpandEnvironmentVariables(Properties.OptionsConnectionsPage.Default.ConnectionFilePath);
            }

            return GetDefaultStartupConnectionFileName();
        }

        public static string GetDefaultStartupConnectionFileName()
        {
            return Runtime.IsPortableEdition ? GetDefaultStartupConnectionFileNamePortableEdition() : GetDefaultStartupConnectionFileNameNormalEdition();
        }

        private static void UpdateCustomConsPathSetting(string filename)
        {
            if (filename == GetDefaultStartupConnectionFileName())
            {
                Properties.OptionsBackupPage.Default.LoadConsFromCustomLocation = false;
            }
            else
            {
                Properties.OptionsBackupPage.Default.LoadConsFromCustomLocation = true;
                Properties.OptionsBackupPage.Default.BackupLocation = filename;
            }
        }

        private static string GetDefaultStartupConnectionFileNameNormalEdition()
        {
            string appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), Application.ProductName ?? "mRemoteNG", ConnectionsFileInfo.DefaultConnectionsFile);
            return File.Exists(appDataPath) ? appDataPath : GetDefaultStartupConnectionFileNamePortableEdition();
        }

        private static string GetDefaultStartupConnectionFileNamePortableEdition()
        {
            return Path.Combine(ConnectionsFileInfo.DefaultConnectionsPath, ConnectionsFileInfo.DefaultConnectionsFile);
        }

        private static void TrySaveSqlConnectionsCache(ConnectionTreeModel connectionTreeModel)
        {
            try
            {
                string cachePath = Path.Combine(SettingsFileInfo.SettingsPath, SettingsFileInfo.SqlConnectionsCache);
                ConnectionTreeModel cacheModel = new();
                foreach (RootNodeInfo root in connectionTreeModel.RootNodes.OfType<RootNodeInfo>())
                    cacheModel.AddRootNode(root);
                XmlConnectionsSaver cacheSaver = new(cachePath, new SaveFilter());
                cacheSaver.Save(cacheModel);

                // The saver stamps a .backup on every write, but only the ConnectionsSaved event
                // path prunes — and this cache save never raises it, so its backups accumulated
                // unbounded (observed live: copies spanning five months). Prune here directly.
                FileBackupPruner.PruneBackupFiles(cachePath, Properties.OptionsBackupPage.Default.BackupFileKeepCount);

                Runtime.MessageCollector.AddMessage(MessageClass.DebugMsg, $"SQL connections cache saved to '{cachePath}'");
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector.AddExceptionStackTrace("Failed to save SQL connections cache", ex);
            }
        }

        #region Events

        /// <summary>
        /// What last armed <see cref="HasUnsavedChanges"/> — the collection action or property
        /// name. Logged by the autosave tick so field logs can attribute residual periodic saves
        /// to their source instead of guessing.
        /// </summary>
        public string? LastChangeReason { get; private set; }

        private void MarkDirtyOnCollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            LastChangeReason = $"collection:{e.Action}";
            HasUnsavedChanges = true;
        }

        private void MarkDirtyOnPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            string property = e.PropertyName ?? "";

            // Same runtime-only filter as SaveConnectionsOnEdit (#83): these are never persisted,
            // so they must not arm the autosave either.
            if (property is nameof(ConnectionInfo.HostReachabilityStatus)
                         or nameof(ConnectionInfo.OpenConnections)
                         or nameof(ConnectionInfo.IsQuickConnect)
                         or nameof(ConnectionInfo.PleaseConnect))
            {
                return;
            }

            LastChangeReason = $"property:{property}";
            HasUnsavedChanges = true;
        }

        public event EventHandler<ConnectionsLoadedEventArgs>? ConnectionsLoaded;
        public event EventHandler<ConnectionsSavedEventArgs>? ConnectionsSaved;

        private void RaiseConnectionsLoadedEvent(Optional<ConnectionTreeModel> previousTreeModel, ConnectionTreeModel newTreeModel, bool previousSourceWasDatabase, bool newSourceIsDatabase, string newSourcePath)
        {
            foreach (ConnectionTreeModel oldTree in previousTreeModel)
            {
                oldTree.CollectionChanged -= MarkDirtyOnCollectionChanged;
                oldTree.PropertyChanged -= MarkDirtyOnPropertyChanged;
            }

            newTreeModel.CollectionChanged += MarkDirtyOnCollectionChanged;
            newTreeModel.PropertyChanged += MarkDirtyOnPropertyChanged;

            ConnectionsLoaded?.Invoke(this, new ConnectionsLoadedEventArgs(previousTreeModel, newTreeModel, previousSourceWasDatabase, newSourceIsDatabase, newSourcePath));

            // Clear AFTER the load event: subscribers mutate the freshly loaded model during
            // handling (e.g. PuTTY saved-session injection), and none of that is a user change —
            // it must not arm the autosave timer's first tick.
            HasUnsavedChanges = false;
        }

        private void RaiseConnectionsSavedEvent(ConnectionTreeModel modelThatWasSaved, bool previouslyUsingDatabase, bool usingDatabase, string connectionFileName)
        {
            ConnectionsSaved?.Invoke(this, new ConnectionsSavedEventArgs(modelThatWasSaved, previouslyUsingDatabase, usingDatabase, connectionFileName));
        }

        #endregion
    }
}
