using System;
using System.Threading;
using System.Windows.Forms;
using mRemoteNG.Connection;
using mRemoteNG.Connection.Protocol;
using mRemoteNG.UI.Tabs;
using NUnit.Framework;

namespace mRemoteNGTests.Connection.Protocol
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public class PuttyBaseConnectTests
    {
        private PuttyBase _puttyProtocol;
        private ConnectionTab _connectionTab;
        private InterfaceControl _interfaceControl;
        private string _originalPuttyPath;

        [SetUp]
        public void Setup()
        {
            _originalPuttyPath = PuttyBase.PuttyPath;
            _puttyProtocol = new PuttyBase();
            _connectionTab = new ConnectionTab();
            ConnectionInfo connectionInfo = new ConnectionInfo
            {
                Protocol = ProtocolType.SSH2,
                Name = "Test Connection",
                Hostname = "localhost"
            };
            _interfaceControl = new InterfaceControl(_connectionTab, _puttyProtocol, connectionInfo);
            _puttyProtocol.InterfaceControl = _interfaceControl;
            
            // Set PuttyPath to cmd.exe to simulate a process starting
            PuttyBase.PuttyPath = "cmd.exe"; 
        }

        [TearDown]
        public void TearDown()
        {
            // Dispose the control tree BEFORE Close(). ProtocolBase.Close is asynchronous: it
            // hands teardown to a background STA thread (CloseBG) that relies on Control.Invoke
            // to marshal back — but this test thread has no message pump and the controls never
            // created a handle, so InvokeRequired reports false and CloseBG disposes the same
            // control tree on its own thread. With Close() first, that background dispose raced
            // these two Dispose calls inside Control.ControlCollection and TearDown died with
            // ArgumentOutOfRangeException('index') — once, on a loaded CI runner (nightly run
            // 31970484140), never locally. Disposing first is deterministic: CloseBG sees
            // IsDisposed and leaves the tree alone. PuttyBase.Close itself is safe on disposed
            // controls — its synchronous half only unhooks an event and kills the process.
            _interfaceControl?.Dispose();
            _connectionTab?.Dispose();
            _puttyProtocol?.Close();
            PuttyBase.PuttyPath = _originalPuttyPath;
        }

        [Test]
        public void Connect_ReturnsTrueImmediately()
        {
            // This test verifies that Connect() returns true without waiting for the window
            bool result = _puttyProtocol.Connect();
            Assert.That(result, Is.True, "Connect should return true immediately");
        }
    }
}
