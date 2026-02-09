using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using mRemoteNG.App;
using mRemoteNG.Connection;
using mRemoteNG.Connection.Protocol;
using mRemoteNG.Connection.Protocol.VNC;
using mRemoteNG.Messages;
using NSubstitute;
using NUnit.Framework;

namespace mRemoteNGTests.Connection.Protocol.VNC
{
    [TestFixture]
    [Apartment(ApartmentState.STA)] // VNCSharpCore relies on STA
    public class ProtocolVNCTests
    {
        private ProtocolVNC _vncProtocol;
        private ConnectionInfo _connectionInfo;

        [SetUp]
        public void Setup()
        {
            // Initialize real ConnectionInfo
            _connectionInfo = new ConnectionInfo
            {
                Hostname = "test.host.com",
                Port = 5900,
                Protocol = ProtocolType.VNC,
                Password = "testpassword",
                VNCSmartSizeMode = ProtocolVNC.SmartSizeMode.SmartSNo
            };

            // Initialize ProtocolVNC
            _vncProtocol = new ProtocolVNC();
            
            // We need to satisfy the InterfaceControl dependency for Initialize()
            // InterfaceControl needs a Control (can be null for some tests) and ConnectionInfo
            _vncProtocol.InterfaceControl = new InterfaceControl(null, _vncProtocol, _connectionInfo);
            
            _vncProtocol.Initialize();
            
            // Clear real MessageCollector
            Runtime.MessageCollector.ClearMessages();
        }

        [TearDown]
        public void Teardown()
        {
            try
            {
                _vncProtocol?.Disconnect();
            }
            catch (Exception)
            {
                // Ignore exceptions during disconnect in teardown
            }
        }

        [Test]
        public void StartChat_AddsInformationMessage()
        {
            // Act
            _vncProtocol.StartChat();

            // Assert
            var lastMessage = Runtime.MessageCollector.Messages.LastOrDefault();
            Assert.That(lastMessage, Is.Not.Null);
            Assert.That(lastMessage.Class, Is.EqualTo(MessageClass.InformationMsg));
            Assert.That(lastMessage.Text, Does.Contain("VNC chat is not supported"));
        }

        [Test]
        public void StartFileTransfer_AddsInformationMessage()
        {
            // Act
            _vncProtocol.StartFileTransfer();

            // Assert
            var lastMessage = Runtime.MessageCollector.Messages.LastOrDefault();
            Assert.That(lastMessage, Is.Not.Null);
            Assert.That(lastMessage.Class, Is.EqualTo(MessageClass.InformationMsg));
            Assert.That(lastMessage.Text, Does.Contain("VNC file transfer is not supported"));
        }
    }
}