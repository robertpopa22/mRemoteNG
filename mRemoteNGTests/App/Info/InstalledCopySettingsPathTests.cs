using System.IO;
using System.Linq;
using mRemoteNG.App.Info;
using NUnit.Framework;

namespace mRemoteNGTests.App.Info
{
    /// <summary>
    /// #214: the MSI ships the build compiled as portable. An installed copy must keep its settings
    /// in the per-user folder even when its program folder is writable (an elevated run).
    /// </summary>
    [TestFixture]
    public class InstalledCopySettingsPathTests
    {
        private const string Exe = @"C:\Program Files\mRemoteNG";
        private const string AppData = @"C:\Users\someone\AppData\Roaming\mRemoteNG Connection Manager";
        private static readonly string[] ExpectedCopies = ["confCons.xml", "Themes"];

        [Test]
        public void AnInstalledCopyUsesAppDataEvenWhenItsFolderIsWritable() =>
            Assert.That(SettingsFileInfo.ResolvePortableSettingsDirectory(Exe, installedCopy: true, _ => true, AppData),
                        Is.EqualTo(AppData));

        [Test]
        public void AnInstalledCopyUsesAppDataWhenItsFolderIsReadOnly() =>
            Assert.That(SettingsFileInfo.ResolvePortableSettingsDirectory(Exe, installedCopy: true, _ => false, AppData),
                        Is.EqualTo(AppData));

        [Test]
        public void APortableCopyKeepsItsSettingsBesideTheExecutable() =>
            Assert.That(SettingsFileInfo.ResolvePortableSettingsDirectory(@"C:\Tools\mRemoteNG", installedCopy: false, _ => true, AppData),
                        Is.EqualTo(Path.Combine(@"C:\Tools\mRemoteNG", SettingsFileInfo.PortableSettingsFolderName)));

        [Test]
        public void APortableCopyOnAReadOnlyDriveFallsBackToAppData() =>
            Assert.That(SettingsFileInfo.ResolvePortableSettingsDirectory(@"\\server\share\mRemoteNG", installedCopy: false, _ => false, AppData),
                        Is.EqualTo(AppData));

        [Test]
        public void TheInstallerShipsTheMarkerThisCodeLooksFor()
        {
            string package = File.ReadAllText(Path.Combine(RepositoryRoot(), "mRemoteNGInstaller", "Package.wxs"));
            string marker = Path.Combine(RepositoryRoot(), "mRemoteNGInstaller", "Installer", "Resources", SettingsFileInfo.InstalledMarkerFileName);

            Assert.Multiple(() =>
            {
                Assert.That(File.Exists(marker), Is.True, "marker source file missing");
                Assert.That(package, Does.Contain(@"Installer\Resources\" + SettingsFileInfo.InstalledMarkerFileName));
                Assert.That(package, Does.Contain(@"<ComponentRef Id=""InstalledMarker"" />"));
            });
        }

        [Test]
        public void SettingsLeftInTheProgramFolderAreCopiedWithoutOverwritingOrMoving()
        {
            string root = Path.Combine(Path.GetTempPath(), "mrng214-" + Path.GetRandomFileName());
            string from = Path.Combine(root, "program", "Settings");
            string to = Path.Combine(root, "appdata");
            try
            {
                Directory.CreateDirectory(Path.Combine(from, "Themes"));
                Directory.CreateDirectory(to);
                File.WriteAllText(Path.Combine(from, "confCons.xml"), "from-program-folder");
                File.WriteAllText(Path.Combine(from, "mRemoteNG.settings"), "old-settings");
                File.WriteAllText(Path.Combine(from, "Themes", "dark.xml"), "theme");
                File.WriteAllText(Path.Combine(to, "mRemoteNG.settings"), "already-per-user");

                var copied = SettingsFileInfo.CopyMissingSettings(from, to);

                Assert.Multiple(() =>
                {
                    Assert.That(copied, Is.EquivalentTo(ExpectedCopies));
                    Assert.That(File.ReadAllText(Path.Combine(to, "confCons.xml")), Is.EqualTo("from-program-folder"));
                    Assert.That(File.ReadAllText(Path.Combine(to, "mRemoteNG.settings")), Is.EqualTo("already-per-user"), "an existing file is never overwritten");
                    Assert.That(File.Exists(Path.Combine(to, "Themes", "dark.xml")), Is.True);
                    Assert.That(File.Exists(Path.Combine(from, "confCons.xml")), Is.True, "the source is copied, not moved");
                });
            }
            finally
            {
                Directory.Delete(root, recursive: true);
            }
        }

        [Test]
        public void AFullyCopiedProgramFolderIsRetiredNotDeleted()
        {
            string root = Path.Combine(Path.GetTempPath(), "mrng214-" + Path.GetRandomFileName());
            string from = Path.Combine(root, "program", "Settings");
            string to = Path.Combine(root, "appdata");
            try
            {
                Directory.CreateDirectory(from);
                File.WriteAllText(Path.Combine(from, "confCons.xml"), "twenty connections");
                File.WriteAllText(Path.Combine(from, "mRemoteNG.settings"), "settings");

                bool retired = SettingsFileInfo.MigrateProgramFolderSettings(from, to);

                Assert.Multiple(() =>
                {
                    Assert.That(retired, Is.True);
                    Assert.That(Directory.Exists(from), Is.False, "no longer offered as a second connections file");
                    Assert.That(File.ReadAllText(Path.Combine(from + SettingsFileInfo.MigratedProgramFolderSuffix, "confCons.xml")),
                                Is.EqualTo("twenty connections"), "nothing deleted");
                    Assert.That(File.ReadAllText(Path.Combine(to, "confCons.xml")), Is.EqualTo("twenty connections"));
                });
            }
            finally
            {
                Directory.Delete(root, recursive: true);
            }
        }

        [TestCase("standard set")]
        [TestCase("longer standard set")]
        public void AProgramFolderThatDiffersFromThePerUserCopyStaysForThePicker(string perUserCopy)
        {
            string root = Path.Combine(Path.GetTempPath(), "mrng214-" + Path.GetRandomFileName());
            string from = Path.Combine(root, "program", "Settings");
            string to = Path.Combine(root, "appdata");
            try
            {
                Directory.CreateDirectory(from);
                Directory.CreateDirectory(to);
                File.WriteAllText(Path.Combine(from, "confCons.xml"), "elevated set");
                File.WriteAllText(Path.Combine(to, "confCons.xml"), perUserCopy);

                bool retired = SettingsFileInfo.MigrateProgramFolderSettings(from, to);

                Assert.Multiple(() =>
                {
                    Assert.That(retired, Is.False);
                    Assert.That(File.ReadAllText(Path.Combine(from, "confCons.xml")), Is.EqualTo("elevated set"));
                    Assert.That(File.ReadAllText(Path.Combine(to, "confCons.xml")), Is.EqualTo(perUserCopy));
                });
            }
            finally
            {
                Directory.Delete(root, recursive: true);
            }
        }

        [Test]
        public void ASecondMigrationPreservesThePreviouslyRetiredFolder()
        {
            DirectoryInfo root = Directory.CreateTempSubdirectory();
            try
            {
                string from = root.CreateSubdirectory("Settings").FullName;
                string retired = from + SettingsFileInfo.MigratedProgramFolderSuffix;
                string to = Path.Combine(root.FullName, "appdata");
                Directory.CreateDirectory(retired);
                File.WriteAllText(Path.Combine(retired, "confCons.xml"), "first migration");
                File.WriteAllText(Path.Combine(from, "confCons.xml"), "second migration");

                Assert.That(SettingsFileInfo.MigrateProgramFolderSettings(from, to), Is.True);

                DirectoryInfo secondRetired = root.GetDirectories("Settings.migrated-*").Single();
                Assert.That(File.ReadAllText(Path.Combine(retired, "confCons.xml")), Is.EqualTo("first migration"));
                Assert.That(File.ReadAllText(Path.Combine(secondRetired.FullName, "confCons.xml")), Is.EqualTo("second migration"));
                Assert.That(File.ReadAllText(Path.Combine(to, "confCons.xml")), Is.EqualTo("second migration"));
                Assert.That(Directory.Exists(from), Is.False);
            }
            finally
            {
                root.Delete(recursive: true);
            }
        }

        [Test]
        public void ASettingsFileThatCannotBeReadLeavesBothCopiesAvailable()
        {
            DirectoryInfo root = Directory.CreateTempSubdirectory();
            try
            {
                string from = root.CreateSubdirectory("Settings").FullName;
                string to = root.CreateSubdirectory("appdata").FullName;
                string source = Path.Combine(from, "confCons.xml");
                string target = Path.Combine(to, "confCons.xml");
                File.WriteAllText(source, "same data");

                using (File.Open(source, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                {
                    Assert.That(SettingsFileInfo.CopyMissingSettings(from, to), Is.Empty);
                    Assert.That(File.Exists(target), Is.False);
                    File.WriteAllText(target, "same data");

                    Assert.That(SettingsFileInfo.MigrateProgramFolderSettings(from, to), Is.False);
                }

                Assert.That(File.ReadAllText(source), Is.EqualTo("same data"));
                Assert.That(File.ReadAllText(target), Is.EqualTo("same data"));
                Assert.That(Directory.Exists(from + SettingsFileInfo.MigratedProgramFolderSuffix), Is.False);
            }
            finally
            {
                root.Delete(recursive: true);
            }
        }

        [TestCase(false, false, true)]
        [TestCase(false, true, true)]
        [TestCase(true, false, false)]
        [TestCase(true, true, true)]
        public void ProgramFolderSettingsAreACandidateOnlyWhereTheyWereUsed(bool installed, bool writable, bool expected) =>
            Assert.That(SettingsFileInfo.ProgramFolderSettingsApplyFor(installed, writable), Is.EqualTo(expected));

        [Test]
        public void NothingIsCopiedWhenTheProgramFolderHasNoSettings()
        {
            string from = Path.Combine(Path.GetTempPath(), "mrng214-absent-" + Path.GetRandomFileName());
            Assert.That(SettingsFileInfo.CopyMissingSettings(from, Path.GetTempPath()), Is.Empty);
            Assert.That(SettingsFileInfo.MigrateProgramFolderSettings(from, Path.GetTempPath()), Is.False);
        }

        private static string RepositoryRoot()
        {
            for (DirectoryInfo? dir = new(TestContext.CurrentContext.TestDirectory); dir != null; dir = dir.Parent)
            {
                if (File.Exists(Path.Combine(dir.FullName, "mRemoteNG.sln")))
                    return dir.FullName;
            }

            Assert.Fail("repository root not found");
            return string.Empty;
        }
    }
}
