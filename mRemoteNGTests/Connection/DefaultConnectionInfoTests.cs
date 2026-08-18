using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Versioning;
using mRemoteNG.Connection;
using mRemoteNG.Connection.Protocol.RDP;
using mRemoteNGTests.TestHelpers;
using NUnit.Framework;


namespace mRemoteNGTests.Connection
{
    [SupportedOSPlatform("windows")]
	public class DefaultConnectionInfoTests
	{
		private ConnectionInfo _randomizedConnectionInfo;
        private TestScope? _scope;

        [SetUp]
        public void Setup()
        {
            _scope = TestScope.Begin();
	        _randomizedConnectionInfo = ConnectionInfoHelpers.GetRandomizedConnectionInfo();
        }

        [TearDown]
        public void TearDown()
        {
            _scope?.Dispose();
        }

        [TestCaseSource(nameof(GetConnectionInfoProperties))]
        public void LoadingDefaultInfoUpdatesAllProperties(PropertyInfo property)
        {
            DefaultConnectionInfo.Instance.LoadFrom(_randomizedConnectionInfo);
	        var valueInDestination = property.GetValue(DefaultConnectionInfo.Instance);
	        var valueInSource = property.GetValue(_randomizedConnectionInfo);
            Assert.That(valueInDestination, Is.EqualTo(valueInSource));
        }

		[TestCaseSource(nameof(GetConnectionInfoProperties))]
		public void SavingDefaultConnectionInfoExportsAllProperties(PropertyInfo property)
        {
            var saveTarget = new ConnectionInfo();
	        var randomizedValue = property.GetValue(_randomizedConnectionInfo);
			property.SetValue(DefaultConnectionInfo.Instance, randomizedValue);
            DefaultConnectionInfo.Instance.SaveTo(saveTarget);
	        var valueInDestination = property.GetValue(saveTarget);
	        var valueInSource = property.GetValue(DefaultConnectionInfo.Instance);
			Assert.That(valueInDestination, Is.EqualTo(valueInSource));
        }

		[TestCaseSource(nameof(GetConnectionInfoProperties))]
        public void CanSaveDefaultConnectionToModelWithAllStringProperties(PropertyInfo property)
		{
            var saveTarget = new SerializableConnectionInfoAllPropertiesOfType<string>();

			// randomize default connection values to ensure we don't get false passing tests
			var randomizedValue = property.GetValue(_randomizedConnectionInfo);
			property.SetValue(DefaultConnectionInfo.Instance, randomizedValue);

            DefaultConnectionInfo.Instance.SaveTo(saveTarget);

			var valueInSource = property.GetValue(DefaultConnectionInfo.Instance)?.ToString();
			var valueInDestination = saveTarget.GetType().GetProperty(property.Name)?.GetValue(saveTarget)?.ToString();
            try
            {
                Assert.That(valueInDestination, Is.EqualTo(valueInSource));
            }
            catch (AssertionException)
            {
                Console.WriteLine($"Assertion Failed: Parameter {property.Name}");                
            }
            Assert.That(valueInDestination, Is.EqualTo(valueInSource));
        }

        [TestCase("False", RDPDiskDrives.None)]
        [TestCase("True", RDPDiskDrives.All)]
        public void LegacyBooleanRedirectDiskDrivesSettingIsMigrated(string legacyValue, RDPDiskDrives expected)
        {
            DefaultConnectionInfo.Instance.LoadFrom(new LegacyRedirectDrivesSettings
            {
                RedirectDiskDrives = legacyValue
            });

            Assert.That(DefaultConnectionInfo.Instance.RedirectDiskDrives, Is.EqualTo(expected));
        }

		private static IEnumerable<PropertyInfo> GetConnectionInfoProperties()
	    {
			return new ConnectionInfo().GetSerializableProperties();
	    }

        private sealed class LegacyRedirectDrivesSettings
        {
            public string RedirectDiskDrives { get; init; } = string.Empty;
        }
    }
}
