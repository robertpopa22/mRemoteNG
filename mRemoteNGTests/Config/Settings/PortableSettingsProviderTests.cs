using System;
using System.Configuration;
using System.IO;
using mRemoteNG.Config.Settings.Providers;
using NUnit.Framework;

namespace mRemoteNGTests.Config.Settings
{
    /// <summary>
    /// #210: ApplicationSettingsBase.Save() must not rewrite the portable settings file when no
    /// property changed (it was rewritten on every connections reload).
    /// </summary>
    [TestFixture]
    [NonParallelizable]
    public class PortableSettingsProviderTests
    {
        private string _tempDir;
        private string _settingsFile;

        [SetUp]
        public void Setup()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "mRemoteNG-PortableSettings-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDir);
            _settingsFile = Path.Combine(_tempDir, "test.settings");
        }

        [TearDown]
        public void Teardown()
        {
            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, true);
        }

        [Test]
        public void SetPropertyValuesWithoutDirtyValuesDoesNotWriteTheFile()
        {
            var provider = new PortableSettingsProvider { FilePathOverride = _settingsFile };
            provider.SetPropertyValues(new SettingsContext(), Collection("first", dirty: true));
            Assert.That(File.Exists(_settingsFile), Is.True);

            DateTime past = DateTime.UtcNow.AddHours(-1);
            File.SetLastWriteTimeUtc(_settingsFile, past);
            string contentBefore = File.ReadAllText(_settingsFile);

            provider.SetPropertyValues(new SettingsContext(), Collection("second", dirty: false));

            Assert.That(File.GetLastWriteTimeUtc(_settingsFile), Is.EqualTo(past));
            Assert.That(File.ReadAllText(_settingsFile), Is.EqualTo(contentBefore));
        }

        [Test]
        public void SetPropertyValuesWithDirtyValueWritesAndClearsDirtyFlag()
        {
            var provider = new PortableSettingsProvider { FilePathOverride = _settingsFile };
            provider.SetPropertyValues(new SettingsContext(), Collection("first", dirty: true));
            DateTime past = DateTime.UtcNow.AddHours(-1);
            File.SetLastWriteTimeUtc(_settingsFile, past);

            SettingsPropertyValueCollection changed = Collection("second", dirty: true);
            provider.SetPropertyValues(new SettingsContext(), changed);

            Assert.That(File.GetLastWriteTimeUtc(_settingsFile), Is.GreaterThan(past));
            Assert.That(File.ReadAllText(_settingsFile), Does.Contain("second"));
            Assert.That(changed["TestSetting"].IsDirty, Is.False);
        }

        [Test]
        public void SetPropertyValuesCreatesMissingFileEvenWithoutDirtyValues()
        {
            var provider = new PortableSettingsProvider { FilePathOverride = _settingsFile };

            provider.SetPropertyValues(new SettingsContext(), Collection("value", dirty: false));

            Assert.That(File.Exists(_settingsFile), Is.True);
        }

        private static SettingsPropertyValueCollection Collection(string value, bool dirty)
        {
            var property = new SettingsProperty("TestSetting")
            {
                PropertyType = typeof(string),
                SerializeAs = SettingsSerializeAs.String,
                DefaultValue = string.Empty
            };
            var propertyValue = new SettingsPropertyValue(property)
            {
                SerializedValue = value
            };
            propertyValue.IsDirty = dirty;

            return new SettingsPropertyValueCollection { propertyValue };
        }
    }
}
