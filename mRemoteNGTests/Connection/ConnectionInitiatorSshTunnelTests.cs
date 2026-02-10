using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using mRemoteNG.App;
using mRemoteNG.Connection;
using mRemoteNG.Connection.Protocol;
using mRemoteNG.Connection.Protocol.SSH;
using mRemoteNG.Container;
using mRemoteNG.Messages;
using mRemoteNG.Resources.Language;
using mRemoteNG.Tree;
using mRemoteNG.Tree.Root;
using NSubstitute;
using NUnit.Framework;

namespace mRemoteNGTests.Connection
{
    [TestFixture]
    public class ConnectionInitiatorSshTunnelTests
    {
        private ConnectionInitiator _connectionInitiator;
        private IProtocolFactory _mockProtocolFactory;
        private ITunnelPortValidator _mockTunnelPortValidator;
        private ConnectionInfo _targetConnection;
        private ConnectionInfo _tunnelConnection;
        private string _tempFilePath;

        [SetUp]
        public void Setup()
        {
            _mockProtocolFactory = Substitute.For<IProtocolFactory>();
            _mockTunnelPortValidator = Substitute.For<ITunnelPortValidator>();
            _connectionInitiator = new ConnectionInitiator(_mockProtocolFactory, _mockTunnelPortValidator);

            _tunnelConnection = new ConnectionInfo
            {
                Name = "MyTunnel",
                Hostname = "tunnel.host.com",
                Protocol = ProtocolType.SSH2,
                Panel = "General"
            };

            _targetConnection = new ConnectionInfo
            {
                Name = "TargetViaTunnel",
                Hostname = "target.internal.com",
                Port = 80,
                Protocol = ProtocolType.HTTP,
                SSHTunnelConnectionName = "MyTunnel",
                Panel = "General"
            };

            // Setup a real ConnectionTreeModel via ConnectionsService
            _tempFilePath = Path.Combine(Path.GetTempPath(), "test_connections.mxs");
            
            // Create a minimal tree model
            var treeModel = new ConnectionTreeModel();
            var rootNode = new RootNodeInfo(RootNodeType.Connection);
            var container = new ContainerInfo { Name = "Root" };
            rootNode.Children.Add(container);
            container.Children.Add(_tunnelConnection);
            container.Children.Add(_targetConnection);
            treeModel.AddRootNode(rootNode);

            // Use reflection to set the model if LoadConnections is too heavy
            var connectionsService = Runtime.ConnectionsService;
            var treeModelProperty = typeof(ConnectionsService).GetProperty(nameof(ConnectionsService.ConnectionTreeModel));
            var backingField = typeof(ConnectionsService).GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
                .FirstOrDefault(f => f.Name.Contains("<ConnectionTreeModel>k__BackingField"));
            
            if (backingField != null)
            {
                backingField.SetValue(connectionsService, treeModel);
            }
            else
            {
                // Fallback to property setter if it exists (it's private set, so we need reflection)
                treeModelProperty.GetSetMethod(true).Invoke(connectionsService, new object[] { treeModel });
            }

            Runtime.MessageCollector.ClearMessages();
        }

        [TearDown]
        public void Cleanup()
        {
            if (File.Exists(_tempFilePath))
            {
                File.Delete(_tempFilePath);
            }
        }

        [Test]
        public async Task OpenConnection_RetriesSshTunnel_OnFailure()
        {
            // Arrange
            var mockTunnelProtocol = Substitute.For<ProtocolSSH2>();
            var mockTargetProtocol = Substitute.For<ProtocolBase>();
            
            mockTunnelProtocol.InitializeAsync().Returns(Task.FromResult(true));
            mockTunnelProtocol.Connect().Returns(true);
            mockTunnelProtocol.isRunning().Returns(true);
            mockTunnelProtocol.InterfaceControl = new InterfaceControl(null, mockTunnelProtocol, _tunnelConnection);

            int validationCallCount = 0;
            _mockTunnelPortValidator.ValidatePortAsync(Arg.Any<int>()).Returns(_ =>
            {
                validationCallCount++;
                return Task.FromResult(validationCallCount > 2);
            });

            _mockProtocolFactory.CreateProtocol(Arg.Is<ConnectionInfo>(c => c.Name == "MyTunnel"))
                               .Returns(mockTunnelProtocol);
            
            _mockProtocolFactory.CreateProtocol(Arg.Is<ConnectionInfo>(c => c.Hostname == "localhost"))
                               .Returns(mockTargetProtocol);
            
            mockTargetProtocol.InitializeAsync().Returns(Task.FromResult(true));
            mockTargetProtocol.Connect().Returns(true);
            mockTargetProtocol.InterfaceControl = new InterfaceControl(null, mockTargetProtocol, _targetConnection);

            // Act
            _connectionInitiator.OpenConnection(_targetConnection);

            // Assert
            await Task.Delay(1000); 

            await _mockTunnelPortValidator.Received(3).ValidatePortAsync(Arg.Any<int>());
            _mockProtocolFactory.Received(1).CreateProtocol(Arg.Is<ConnectionInfo>(c => c.Hostname == "localhost"));
        }
    }
}