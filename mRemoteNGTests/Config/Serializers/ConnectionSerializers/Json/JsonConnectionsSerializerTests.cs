using System;
using System.Linq;
using mRemoteNG.Config.Serializers.ConnectionSerializers.Json;
using mRemoteNG.Connection;
using mRemoteNG.Connection.Protocol;
using mRemoteNG.Container;
using mRemoteNG.Security;
using mRemoteNGTests.TestHelpers;
using NUnit.Framework;

namespace mRemoteNGTests.Config.Serializers.ConnectionSerializers.Json
{
    [TestFixture]
    public class JsonConnectionsSerializerTests
    {
        private JsonConnectionsSerializer _serializer;
        private SaveFilter _saveFilter;

        [SetUp]
        public void Setup()
        {
            _saveFilter = new SaveFilter();
            _serializer = new JsonConnectionsSerializer(_saveFilter);
        }

        [Test]
        public void Serialize_SingleConnection_IncludesBasicProperties()
        {
            // Arrange
            var connection = new ConnectionInfo
            {
                Name = "Test Connection",
                Hostname = "test-host",
                Protocol = ProtocolType.RDP,
                Port = 3389,
                Description = "A test description"
            };

            // Act
            string json = _serializer.Serialize(connection);

            // Assert
            Assert.That(json, Does.Contain("\"Name\": \"Test Connection\""));
            Assert.That(json, Does.Contain("\"Hostname\": \"test-host\""));
            Assert.That(json, Does.Contain("\"Protocol\": \"RDP\""));
            Assert.That(json, Does.Contain("\"Port\": 3389"));
            Assert.That(json, Does.Contain("\"Description\": \"A test description\""));
        }

        [Test]
        public void Serialize_WithCredentialsEnabled_IncludesUsernameAndPassword()
        {
            // Arrange
            _saveFilter.SaveUsername = true;
            _saveFilter.SavePassword = true;
            var connection = new ConnectionInfo
            {
                Username = "test-user",
                Password = "test-password"
            };

            // Act
            string json = _serializer.Serialize(connection);

            // Assert
            Assert.That(json, Does.Contain("\"Username\": \"test-user\""));
            Assert.That(json, Does.Contain("\"Password\": \"test-password\""));
        }

        [Test]
        public void Serialize_WithCredentialsDisabled_ExcludesUsernameAndPassword()
        {
            // Arrange
            _saveFilter.SaveUsername = false;
            _saveFilter.SavePassword = false;
            var connection = new ConnectionInfo
            {
                Username = "test-user",
                Password = "test-password"
            };

            // Act
            string json = _serializer.Serialize(connection);

            // Assert
            Assert.That(json, Does.Not.Contain("\"Username\""));
            Assert.That(json, Does.Not.Contain("\"Password\""));
        }

        [Test]
        public void Serialize_Container_IncludesChildren()
        {
            // Arrange
            var container = new ContainerInfo("cont-id") { Name = "My Folder" };
            var child = new ConnectionInfo { Name = "Child Connection" };
            container.Children.Add(child);

            // Act
            string json = _serializer.Serialize(container);

            // Assert
            Assert.That(json, Does.Contain("\"Name\": \"My Folder\""));
            Assert.That(json, Does.Contain("\"Children\":"));
            Assert.That(json, Does.Contain("\"Name\": \"Child Connection\""));
        }

        [Test]
        public void Serialize_NodeId_IsIncluded()
        {
            // Arrange
            var connection = new ConnectionInfo();
            string expectedId = connection.ConstantID;

            // Act
            string json = _serializer.Serialize(connection);

            // Assert
            Assert.That(json, Does.Contain("\"Id\": \"" + expectedId + "\""));
        }
    }
}