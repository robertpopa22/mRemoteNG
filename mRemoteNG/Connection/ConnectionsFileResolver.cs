using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;
using mRemoteNG.App;
using mRemoteNG.App.Info;
using mRemoteNG.Messages;
using mRemoteNG.Properties;

namespace mRemoteNG.Connection
{
    [SupportedOSPlatform("windows")]
    public static class ConnectionsFileResolver
    {
        /// <summary>
        /// A candidate <c>confCons.xml</c> location that was discovered on disk.
        /// </summary>
        public sealed record Candidate(string Path, DateTime LastWriteTimeUtc, long Size, string Label);

        /// <summary>
        /// Scan every well-known location where a connections file could live and
        /// return one <see cref="Candidate"/> per existing file. Paths are de-duplicated
        /// case-insensitively on their full paths.
        /// </summary>
        public static IReadOnlyList<Candidate> DiscoverCandidates()
        {
            List<string> seen = [];
            List<Candidate> result = [];

            void AddIfExists(string path, string label)
            {
                if (string.IsNullOrWhiteSpace(path)) return;
                string full;
                try { full = System.IO.Path.GetFullPath(path); }
                catch { return; }
                if (seen.Any(s => string.Equals(s, full, StringComparison.OrdinalIgnoreCase))) return;
                if (!File.Exists(full)) return;
                try
                {
                    var info = new FileInfo(full);
                    // Touching Length / LastWriteTimeUtc can still throw if the file
                    // is deleted or permissions change between the File.Exists check
                    // and this read — treat that as "not present" and move on.
                    long size = info.Length;
                    DateTime mtimeUtc = info.LastWriteTimeUtc;
                    seen.Add(full);
                    result.Add(new Candidate(full, mtimeUtc, size, label));
                }
                catch
                {
                    // ignore — candidate vanished during the scan
                }
            }

            // Installed-edition candidates — check BOTH folder naming conventions:
            //   * "mRemoteNG Connection Manager"  (Application.ProductName, what newer
            //     builds write to when running as installed edition)
            //   * "mRemoteNG"                      (legacy / short name, what older
            //     installs used and what still ships on many user boxes)
            // This is exactly the upgrade scenario #95 reported: the old location on
            // disk uses the short name, a new install starts writing to the long name,
            // and without checking both the picker never surfaces the legacy file.
            string[] installedFolderNames = string.IsNullOrEmpty(Application.ProductName)
                ? new[] { "mRemoteNG" }
                : new[] { Application.ProductName, "mRemoteNG" }.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();

            foreach (string folderName in installedFolderNames)
            {
                AddIfExists(
                    System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                                           folderName,
                                           ConnectionsFileInfo.DefaultConnectionsFile),
                    $"Installed LocalAppData ({folderName})");

                AddIfExists(
                    System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                                           folderName,
                                           ConnectionsFileInfo.DefaultConnectionsFile),
                    $"Installed Roaming ({folderName})");
            }

            // Portable-edition main: <exedir>\Settings\confCons.xml
            string exeDir = System.IO.Path.GetDirectoryName(
                Assembly.GetAssembly(typeof(ConnectionInfo))?.Location) ?? string.Empty;
            if (!string.IsNullOrEmpty(exeDir) && SettingsFileInfo.ProgramFolderSettingsApply)
            {
                AddIfExists(
                    System.IO.Path.Combine(exeDir, SettingsFileInfo.PortableSettingsFolderName,
                                           ConnectionsFileInfo.DefaultConnectionsFile),
                    "Portable (exe\\Settings)");
                AddIfExists(
                    System.IO.Path.Combine(exeDir, ConnectionsFileInfo.DefaultConnectionsFile),
                    "Portable (exe root, legacy)");
            }

            // The saved Options path (user-configured custom location) — include only
            // if it actually exists on disk; we don't want to list a stale setting.
            try
            {
                string saved = OptionsConnectionsPage.Default.ConnectionFilePath;
                if (!string.IsNullOrWhiteSpace(saved))
                {
                    AddIfExists(Environment.ExpandEnvironmentVariables(saved), "Saved (Options)");
                }
            }
            catch
            {
                // Settings subsystem failure is not fatal here; just skip.
            }

            mRemoteNG.App.DevLog.Write($"[Resolver] candidates={result.Count} paths=[{string.Join("; ", result.Select(r => r.Path))}]");
            return result;
        }

        /// <summary>
        /// When more than one candidate file is discovered, returns the one the user
        /// chose (or auto-selected newest if prompting is disabled). Returns <c>null</c>
        /// if no candidates exist or if the user cancelled the prompt.
        /// </summary>
        /// <param name="candidates">The candidates to resolve.</param>
        /// <param name="promptFactory">
        /// Factory used to show the picker dialog. Injected so tests can assert behavior
        /// without a real dialog.
        /// </param>
        public static Candidate? Resolve(
            IReadOnlyList<Candidate> candidates,
            Func<IReadOnlyList<Candidate>, Candidate?, (Candidate? Choice, bool RememberChoice)> promptFactory)
        {
            ArgumentNullException.ThrowIfNull(candidates);
            ArgumentNullException.ThrowIfNull(promptFactory);

            if (candidates.Count == 0) return null;

            // Reset button sets ForceConnectionsFilePickerOnNextStart so the
            // picker appears on the very next launch regardless of how many
            // candidate files exist (the default "1 candidate -> silent" fast
            // path would otherwise hide the picker from users who only have
            // a single confCons.xml but still want to pick / move it). Flag
            // is cleared immediately so it only consumes the one launch.
            bool force = OptionsConnectionsPage.Default.ForceConnectionsFilePickerOnNextStart;
            if (force)
            {
                OptionsConnectionsPage.Default.ForceConnectionsFilePickerOnNextStart = false;
                OptionsConnectionsPage.Default.Save();
            }
            if (!force && candidates.Count == 1) return candidates[0];

            // Newest-by-mtime is the default pre-selection.
            Candidate newest = candidates.OrderByDescending(c => c.LastWriteTimeUtc).First();

            // If the user already picked for this EXACT candidate set, honour it
            // silently. The fingerprint covers both the set of paths AND their
            // mtimes, so if a new candidate appears (or an existing one is
            // overwritten with a newer version) the stored fingerprint no longer
            // matches and we re-prompt. This is the "Remember tied to this exact
            // set" semantic — a plain path match would silently keep loading the
            // old file after a new one shows up alongside it, which is the class
            // of bug #95 originally described.
            string savedChoice = OptionsConnectionsPage.Default.ResolvedConnectionFilePath;
            string savedFingerprint = OptionsConnectionsPage.Default.ResolvedCandidatesFingerprint;
            string currentFingerprint = ComputeCandidatesFingerprint(candidates);
            if (!string.IsNullOrWhiteSpace(savedChoice) &&
                string.Equals(savedFingerprint, currentFingerprint, StringComparison.Ordinal))
            {
                Candidate? prior = candidates.FirstOrDefault(
                    c => string.Equals(c.Path, savedChoice, StringComparison.OrdinalIgnoreCase));
                if (prior is not null) return prior;
            }

            (Candidate? Choice, bool RememberChoice) outcome = promptFactory(candidates, newest);
            if (outcome.Choice is null) return null;

            if (outcome.RememberChoice)
            {
                OptionsConnectionsPage.Default.ConnectionFilePath = outcome.Choice.Path;
                OptionsConnectionsPage.Default.ResolvedConnectionFilePath = outcome.Choice.Path;
                OptionsConnectionsPage.Default.ResolvedCandidatesFingerprint = currentFingerprint;
                OptionsConnectionsPage.Default.Save();
            }
            else
            {
                // Session-only choice: remember inside the resolver cache only, no persist.
                OptionsConnectionsPage.Default.ResolvedConnectionFilePath = outcome.Choice.Path;
                OptionsConnectionsPage.Default.ResolvedCandidatesFingerprint = currentFingerprint;
            }

            Runtime.MessageCollector?.AddMessage(MessageClass.InformationMsg,
                $"Connections file resolved to {outcome.Choice.Path} (remembered={outcome.RememberChoice}).");
            return outcome.Choice;
        }

        /// <summary>
        /// Canonical fingerprint of a candidate set: SHA-256 over sorted lowercase
        /// full paths only. Deliberately ignores file sizes / mtimes — the app
        /// itself modifies <c>confCons.xml</c> every save and autosave, so
        /// hashing mtime would invalidate the remembered choice on every second
        /// launch. The fingerprint flips only when the <i>set of known locations</i>
        /// changes (a new install path appeared, an old one was removed), which
        /// is exactly the condition under which we want the picker to re-appear.
        /// </summary>
        internal static string ComputeCandidatesFingerprint(IEnumerable<Candidate> candidates)
        {
            IEnumerable<string> ordered = candidates
                .Select(c => c.Path.ToLowerInvariant())
                .OrderBy(s => s, StringComparer.Ordinal);
            byte[] bytes = Encoding.UTF8.GetBytes(string.Join("\n", ordered));
            byte[] hash = SHA256.HashData(bytes);
            return Convert.ToHexString(hash);
        }
    }
}
