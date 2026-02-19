using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using mRemoteNG.App;
using mRemoteNG.App.Info;
using System.Runtime.Versioning;

namespace mRemoteNG.Tools
{
    [SupportedOSPlatform("windows")]
    public static class DebugDumper
    {
        public static void CreateDebugBundle()
        {
            using (var sfd = new SaveFileDialog())
            {
                sfd.Filter = "Zip files (*.zip)|*.zip";
                sfd.FileName = $"mRemoteNG_Debug_{DateTime.Now:yyyyMMdd_HHmmss}.zip";
                if (sfd.ShowDialog() != DialogResult.OK) return;

                try
                {
                    CreateDebugBundle(sfd.FileName);
                    MessageBox.Show("Debug bundle created successfully!", "Debug Bundle", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to create debug bundle: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        public static void CreateDebugBundle(string destinationPath)
        {
            using (var archive = ZipFile.Open(destinationPath, ZipArchiveMode.Create))
            {
                AddSystemInfo(archive);
                AddLogFile(archive);
                AddConfigFile(archive);
            }
        }

        private static void AddSystemInfo(ZipArchive archive)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"mRemoteNG Version: {GeneralAppInfo.ApplicationVersion}");
            sb.AppendLine($"OS Version: {Environment.OSVersion}");
            sb.AppendLine($"64-bit OS: {Environment.Is64BitOperatingSystem}");
            sb.AppendLine($"64-bit Process: {Environment.Is64BitProcess}");
            sb.AppendLine($"CLR Version: {Environment.Version}");
            sb.AppendLine($"Current Culture: {System.Globalization.CultureInfo.CurrentCulture.Name}");
            sb.AppendLine($"Portable Edition: {Runtime.IsPortableEdition}");
            
            var entry = archive.CreateEntry("SystemInfo.txt");
            using (var entryStream = entry.Open())
            using (var writer = new StreamWriter(entryStream))
            {
                writer.Write(sb.ToString());
            }
        }

        private static void AddLogFile(ZipArchive archive)
        {
             // Log path is typically %APPDATA%\mRemoteNG\mRemoteNG.log
             string logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "mRemoteNG", "mRemoteNG.log");
             
             if (Runtime.IsPortableEdition)
             {
                 string portableLog = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "mRemoteNG.log");
                 if (File.Exists(portableLog)) logPath = portableLog;
             }

             if (File.Exists(logPath))
             {
                 try {
                     var entry = archive.CreateEntry("mRemoteNG.log");
                     using (var fs = new FileStream(logPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                     using (var entryStream = entry.Open())
                     {
                         fs.CopyTo(entryStream);
                     }
                 } catch (Exception ex) {
                     var entry = archive.CreateEntry("mRemoteNG.log.error.txt");
                     using (var entryStream = entry.Open())
                     using (var writer = new StreamWriter(entryStream))
                     {
                         writer.Write($"Could not read log file: {ex.Message}");
                     }
                 }
             }
        }

        private static void AddConfigFile(ZipArchive archive)
        {
             string configPath = "";
             
             // Try to find the loaded connection file path from properties
             try {
                if (!string.IsNullOrWhiteSpace(Properties.OptionsConnectionsPage.Default.ConnectionFilePath))
                {
                    configPath = Properties.OptionsConnectionsPage.Default.ConnectionFilePath;
                }
             } catch {}

             if (string.IsNullOrEmpty(configPath) || !File.Exists(configPath))
             {
                 // Fallback to default
                 configPath = Path.Combine(SettingsFileInfo.SettingsPath, ConnectionsFileInfo.DefaultConnectionsFile);
             }

             if (File.Exists(configPath))
             {
                 try {
                     string content = File.ReadAllText(configPath);
                     // Sanitize - remove passwords
                     // Regex to replace Password="..." with Password="***REMOVED***"
                     
                     content = Regex.Replace(content, "Password=\"[^\"]*\"", "Password=\"***REMOVED***\"");
                     
                     var entry = archive.CreateEntry("confCons.xml");
                     using (var entryStream = entry.Open())
                     using (var writer = new StreamWriter(entryStream))
                     {
                         writer.Write(content);
                     }
                 } catch (Exception ex) {
                     var entry = archive.CreateEntry("confCons.xml.error.txt");
                     using (var entryStream = entry.Open())
                     using (var writer = new StreamWriter(entryStream))
                     {
                         writer.Write($"Could not read/sanitize config file: {ex.Message}");
                     }
                 }
             }
             else
             {
                 var entry = archive.CreateEntry("confCons.xml.missing.txt");
                 using (var entryStream = entry.Open())
                 using (var writer = new StreamWriter(entryStream))
                 {
                     writer.Write($"Config file not found at: {configPath}");
                 }
             }
        }
    }
}
