using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace mRemoteNGSpecs.Drivers
{
    /// <summary>
    /// Gives one scenario its own copy of the application with an empty portable Settings folder.
    ///
    /// The portable build resolves its settings path from the executable's own directory and offers
    /// no command-line override, so the only way to isolate a scenario's state is to run the
    /// executable from somewhere else. Copying the whole build output per scenario would cost
    /// hundreds of megabytes and seconds each time; NTFS hard links cost one directory entry per
    /// file and share the same data on disk, so setup is measured in milliseconds regardless of
    /// build size.
    ///
    /// Links are safe here because nothing writes through them: the app only ever creates new files
    /// (Settings\confCons.xml, mRemoteNG.log) inside the scenario directory. Deleting a link never
    /// touches the canonical build.
    /// </summary>
    public sealed class IsolatedDeployment : IDisposable
    {
        private const string ScenarioRootName = "_uiscenarios";

        public string Directory { get; }
        public string ExecutablePath { get; }
        public string SettingsPath { get; }

        private bool _disposed;

        private IsolatedDeployment(string directory)
        {
            Directory = directory;
            ExecutablePath = Path.Combine(directory, "mRemoteNG.exe");
            SettingsPath = Path.Combine(directory, "Settings");
        }

        public static IsolatedDeployment Create(string scenarioName)
        {
            string canonical = FindCanonicalBuildOutput();
            string root = Path.Combine(AppContext.BaseDirectory, ScenarioRootName);
            System.IO.Directory.CreateDirectory(root);

            string safeName = new string(scenarioName.Where(char.IsLetterOrDigit).ToArray());
            if (safeName.Length > 40) safeName = safeName[^40..];
            string dir = Path.Combine(root, $"{safeName}-{Guid.NewGuid().ToString("N")[..8]}");
            System.IO.Directory.CreateDirectory(dir);

            LinkTree(canonical, dir, skipTopLevelDirectory: "Settings");

            // A brand-new Settings folder: no connections, no layout, no theme cache, nothing
            // carried over from a previous scenario or from the maintainer's own profile.
            System.IO.Directory.CreateDirectory(Path.Combine(dir, "Settings"));

            return new IsolatedDeployment(dir);
        }

        /// <summary>Seeds the scenario's connections file before the app starts.</summary>
        public void WriteConnectionsFile(string xml)
        {
            File.WriteAllText(Path.Combine(SettingsPath, "confCons.xml"), xml);
        }

        public string? ReadAppLog()
        {
            string log = Path.Combine(Directory, "mRemoteNG.log");
            if (!File.Exists(log)) return null;

            // The app may still hold the handle.
            using FileStream stream = new(log, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using StreamReader reader = new(stream);
            return reader.ReadToEnd();
        }

        private static void LinkTree(string source, string destination, string? skipTopLevelDirectory)
        {
            foreach (string file in System.IO.Directory.GetFiles(source))
                CreateLinkOrCopy(file, Path.Combine(destination, Path.GetFileName(file)));

            foreach (string dir in System.IO.Directory.GetDirectories(source))
            {
                string name = Path.GetFileName(dir);
                if (string.Equals(name, skipTopLevelDirectory, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (string.Equals(name, ScenarioRootName, StringComparison.OrdinalIgnoreCase))
                    continue;

                string target = Path.Combine(destination, name);
                System.IO.Directory.CreateDirectory(target);
                LinkTree(dir, target, skipTopLevelDirectory: null);
            }
        }

        private static void CreateLinkOrCopy(string existing, string link)
        {
            // Falls back to a copy across volumes or when links are unavailable, so the battery
            // still runs — just more slowly — on a machine where hard links do not apply.
            if (!CreateHardLink(link, existing, IntPtr.Zero))
                File.Copy(existing, link, overwrite: true);
        }

        private static string FindCanonicalBuildOutput()
        {
            // Same layout AppDriver.FindExecutable assumes: the test assembly sits at
            // mRemoteNGSpecs\bin\x64\Release and the app at mRemoteNG\bin\x64\Release.
            string repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
            string output = Path.Combine(repoRoot, "mRemoteNG", "bin", "x64", "Release");

            if (!File.Exists(Path.Combine(output, "mRemoteNG.exe")))
                throw new FileNotFoundException(
                    $"mRemoteNG.exe not found under '{output}'. Build with build.ps1 first.");

            return output;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            try
            {
                System.IO.Directory.Delete(Directory, recursive: true);
            }
            catch (IOException)
            {
                // Windows may still hold a handle from the process that just exited. The directory
                // is left for the next run's sweep rather than failing a test over cleanup.
            }
            catch (UnauthorizedAccessException)
            {
            }
        }

        /// <summary>Removes scenario directories left behind by earlier runs.</summary>
        public static void SweepStaleScenarios(TimeSpan olderThan)
        {
            string root = Path.Combine(AppContext.BaseDirectory, ScenarioRootName);
            if (!System.IO.Directory.Exists(root)) return;

            foreach (string dir in System.IO.Directory.GetDirectories(root))
            {
                try
                {
                    if (DateTime.UtcNow - System.IO.Directory.GetCreationTimeUtc(dir) > olderThan)
                        System.IO.Directory.Delete(dir, recursive: true);
                }
                catch (Exception)
                {
                    // Best effort.
                }
            }
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CreateHardLink(string lpFileName, string lpExistingFileName,
                                                  IntPtr lpSecurityAttributes);
    }
}
