using System.Linq;
using mRemoteNG.App;
using mRemoteNG.Connection;
using mRemoteNG.Connection.Protocol;
using mRemoteNG.Messages;
using NUnit.Framework;

namespace mRemoteNGTests.Connection
{
    [TestFixture]
    public class ConnectionInitiatorTests
    {
        private ConnectionInitiator _connectionInitiator;
        private MessageCollector _messageCollector;

        [SetUp]
        public void Setup()
        {
            _connectionInitiator = new ConnectionInitiator();
            _messageCollector = Runtime.MessageCollector;
            _messageCollector.ClearMessages();
        }

        [TearDown]
        public void Teardown()
        {
            _messageCollector?.ClearMessages();
        }

        [Test]
        public void OpenConnection_WithEmptyHostname_AddsErrorMessage()
        {
            // Arrange
            var connectionInfo = new ConnectionInfo
            {
                Name = "Test Connection",
                Hostname = "", // Empty hostname
                Protocol = ProtocolType.RDP // RDP doesn't support blank hostname
            };

            // Act
            _connectionInitiator.OpenConnection(connectionInfo);

            // Give a moment for async operations
            System.Threading.Thread.Sleep(100);

            // Assert
            var errorMessages = _messageCollector.Messages
                .Where(m => m.Class == MessageClass.ErrorMsg)
                .ToList();

            Assert.That(errorMessages, Is.Not.Empty, "Expected at least one error message");
            Assert.That(errorMessages.Any(m => m.Text.Contains("hostname")), 
                Is.True, 
                "Expected error message to mention 'hostname'");
        }

        [Test]
        public void OpenConnection_WithNullHostname_AddsErrorMessage()
        {
            // Arrange
            var connectionInfo = new ConnectionInfo
            {
                Name = "Test Connection",
                Hostname = null, // Null hostname
                Protocol = ProtocolType.SSH2 // SSH doesn't support blank hostname
            };

            // Act
            _connectionInitiator.OpenConnection(connectionInfo);

            // Give a moment for async operations
            System.Threading.Thread.Sleep(100);

            // Assert
            var errorMessages = _messageCollector.Messages
                .Where(m => m.Class == MessageClass.ErrorMsg)
                .ToList();

            Assert.That(errorMessages, Is.Not.Empty, "Expected at least one error message");
            Assert.That(errorMessages.Any(m => m.Text.Contains("hostname")), 
                Is.True, 
                "Expected error message to mention 'hostname'");
        }

        [Test]
        public void OpenConnection_WithValidHostname_DoesNotAddHostnameError()
        {
            // Arrange
            var connectionInfo = new ConnectionInfo
            {
                Name = "Test Connection",
                Hostname = "192.168.1.1", // Valid hostname
                Protocol = ProtocolType.RDP
            };

            // Act
            _connectionInitiator.OpenConnection(connectionInfo);

            // Give a moment for async operations
            System.Threading.Thread.Sleep(100);

            // Assert
            var hostnameErrors = _messageCollector.Messages
                .Where(m => m.Text.Contains("No hostname specified"))
                .ToList();

            Assert.That(hostnameErrors, Is.Empty, 
                "Should not have hostname error when hostname is provided");
        }
    }
}
