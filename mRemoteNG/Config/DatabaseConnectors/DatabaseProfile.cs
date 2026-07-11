using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using mRemoteNG.App;
using mRemoteNG.App.Info;
using mRemoteNG.Messages;
using mRemoteNG.Security.SymmetricEncryption;

namespace mRemoteNG.Config.DatabaseConnectors
{
    public class DatabaseProfile
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = DatabaseConnectorFactory.MsSqlType;
        public string Host { get; set; } = string.Empty;
        public string DatabaseName { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string EncryptedPassword { get; set; } = string.Empty;
        public bool ReadOnly { get; set; }
        public string AuthType { get; set; } = string.Empty; // For SQL Server specific auth types
        
        // Add other properties that are present in OptionsDBsPage if needed
        // For now covering the main ones used in SqlServerPage.cs
    }

    public static class DatabaseProfileManager
    {
        private const string ProfilesFileName = "databaseProfiles.json";
        // The profiles used to live next to the exe (GeneralAppInfo.HomePath). For an MSI install
        // under C:\Program Files that directory is read-only for a normal user, so saving threw
        // "Access to the path ... is denied" (#145). Store them in the same user-writable location
        // as the rest of the settings (%APPDATA%\mRemoteNG when installed, the portable Settings
        // folder otherwise) and migrate any existing file forward on first load.
        private static readonly string ProfilesPath = Path.Combine(SettingsFileInfo.SettingsPath, ProfilesFileName);
        private static readonly string LegacyProfilesPath = Path.Combine(GeneralAppInfo.HomePath, ProfilesFileName);
        private static readonly JsonSerializerOptions s_jsonOptions = new() { WriteIndented = true };
        private static IList<DatabaseProfile> _profiles = new List<DatabaseProfile>();

        public static IList<DatabaseProfile> Profiles
        {
            get
            {
                if (_profiles.Count == 0)
                {
                    LoadProfiles();
                }
                return _profiles;
            }
        }

        public static void LoadProfiles()
        {
            MigrateLegacyProfiles();
            if (File.Exists(ProfilesPath))
            {
                try
                {
                    string json = File.ReadAllText(ProfilesPath);
                    var loadedProfiles = JsonSerializer.Deserialize<List<DatabaseProfile>>(json);
                    if (loadedProfiles != null)
                    {
                        _profiles = loadedProfiles;
                    }
                }
                catch (Exception ex)
                {
                    Runtime.MessageCollector.AddExceptionMessage("Failed to load database profiles", ex);
                }
            }
        }

        public static void SaveProfiles()
        {
            try
            {
                string? directory = Path.GetDirectoryName(ProfilesPath);
                if (!string.IsNullOrEmpty(directory))
                    Directory.CreateDirectory(directory);

                string json = JsonSerializer.Serialize(_profiles, s_jsonOptions);
                File.WriteAllText(ProfilesPath, json);
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector.AddExceptionMessage("Failed to save database profiles", ex);
            }
        }

        // Copies a pre-#145 databaseProfiles.json from the exe directory into the user-writable
        // settings location the first time we run after the move. Copy (not move) so it also works
        // when the old file sits in a read-only Program Files install; the stale copy is harmless.
        private static void MigrateLegacyProfiles()
        {
            try
            {
                if (File.Exists(ProfilesPath) || !File.Exists(LegacyProfilesPath))
                    return;

                string? directory = Path.GetDirectoryName(ProfilesPath);
                if (!string.IsNullOrEmpty(directory))
                    Directory.CreateDirectory(directory);

                File.Copy(LegacyProfilesPath, ProfilesPath);
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector.AddExceptionMessage("Failed to migrate database profiles", ex, MessageClass.WarningMsg);
            }
        }

        public static void AddProfile(DatabaseProfile profile)
        {
            // Remove existing profile with same name if any (upsert behavior)
            var existing = _profiles.FirstOrDefault(p => p.Name.Equals(profile.Name, StringComparison.OrdinalIgnoreCase));
            if (existing != null)
            {
                _profiles.Remove(existing);
            }
            _profiles.Add(profile);
            SaveProfiles();
        }

        public static void RemoveProfile(string profileName)
        {
            var existing = _profiles.FirstOrDefault(p => p.Name.Equals(profileName, StringComparison.OrdinalIgnoreCase));
            if (existing != null)
            {
                _profiles.Remove(existing);
                SaveProfiles();
            }
        }

        public static void ApplyProfileToSettings(DatabaseProfile profile)
        {
             Properties.OptionsDBsPage.Default.SQLServerType = profile.Type;
             Properties.OptionsDBsPage.Default.SQLHost = profile.Host;
             Properties.OptionsDBsPage.Default.SQLDatabaseName = profile.DatabaseName;
             Properties.OptionsDBsPage.Default.SQLUser = profile.Username;
             Properties.OptionsDBsPage.Default.SQLPass = profile.EncryptedPassword; // Already encrypted
             Properties.OptionsDBsPage.Default.SQLReadOnly = profile.ReadOnly;
             if (!string.IsNullOrEmpty(profile.AuthType))
                 Properties.OptionsDBsPage.Default.SQLAuthType = profile.AuthType;
        }
        
        public static DatabaseProfile CreateProfileFromCurrentSettings(string name)
        {
             return new DatabaseProfile
             {
                 Name = name,
                 Type = Properties.OptionsDBsPage.Default.SQLServerType,
                 Host = Properties.OptionsDBsPage.Default.SQLHost,
                 DatabaseName = Properties.OptionsDBsPage.Default.SQLDatabaseName,
                 Username = Properties.OptionsDBsPage.Default.SQLUser,
                 EncryptedPassword = Properties.OptionsDBsPage.Default.SQLPass,
                 ReadOnly = Properties.OptionsDBsPage.Default.SQLReadOnly,
                 AuthType = Properties.OptionsDBsPage.Default.SQLAuthType
             };
        }
    }
}
