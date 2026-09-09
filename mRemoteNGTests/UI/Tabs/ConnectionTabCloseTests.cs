using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using mRemoteNG.Config;
using mRemoteNG.Connection;
using mRemoteNG.Connection.Protocol;
using mRemoteNG.Properties;
using mRemoteNG.UI.Tabs;
using NUnit.Framework;

namespace mRemoteNGTests.UI.Tabs
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    [NonParallelizable]
    public class ConnectionTabCloseTests
    {
        private StubProtocol _protocol = null!;
        private ConnectionTab _connectionTab = null!;
        private InterfaceControl _interfaceControl = null!;
        private int _originalConfirmCloseConnection;
        private bool _originalKeepTabsOpenAfterDisconnect;

        [SetUp]
        public void Setup()
        {
            _originalConfirmCloseConnection = Settings.Default.ConfirmCloseConnection;
            _originalKeepTabsOpenAfterDisconnect = OptionsTabsPanelsPage.Default.KeepTabsOpenAfterDisconnect;

            // Never prompt - the fixture must stay headless.
            Settings.Default.ConfirmCloseConnection = (int)ConfirmCloseEnum.Never;
            OptionsTabsPanelsPage.Default.KeepTabsOpenAfterDisconnect = true;

            _protocol = new StubProtocol();
            _connectionTab = new ConnectionTab { TabText = "SSH2: Connection Name" };

            ConnectionInfo connectionInfo = new()
            {
                Protocol = ProtocolType.SSH2,
                Name = "Connection Name",
                Hostname = "example-host"
            };

            _interfaceControl = new InterfaceControl(_connectionTab, _protocol, connectionInfo);
            _protocol.InterfaceControl = _interfaceControl;
            _connectionTab.Tag = _interfaceControl;
        }

        [TearDown]
        public void TearDown()
        {
            Settings.Default.ConfirmCloseConnection = _originalConfirmCloseConnection;
            OptionsTabsPanelsPage.Default.KeepTabsOpenAfterDisconnect = _originalKeepTabsOpenAfterDisconnect;

            _interfaceControl?.Dispose();
            _connectionTab?.Dispose();
        }

        [Test]
        public void ClosingTheTabClosesItEvenWhenTabsAreKeptOpenAfterDisconnect()
        {
            FormClosingEventArgs closingArgs = InvokeFormClosing();

            Assert.Multiple(() =>
            {
                Assert.That(closingArgs.Cancel, Is.False,
                            "Closing the tab must close the tab - KeepTabsOpenAfterDisconnect only covers a disconnect.");
                Assert.That(_protocol.CloseCallCount, Is.EqualTo(1), "The protocol should still be disconnected.");
            });
        }

        [Test]
        public void DisconnectingFromTheTabMenuKeepsTheTabOpenWhenTabsAreKeptOpenAfterDisconnect()
        {
            _connectionTab.disconnectOnly = true;

            FormClosingEventArgs closingArgs = InvokeFormClosing();

            Assert.Multiple(() =>
            {
                Assert.That(closingArgs.Cancel, Is.True,
                            "A disconnect request should leave the tab in place so it can show the reconnect panel.");
                Assert.That(_protocol.CloseCallCount, Is.EqualTo(1), "The protocol should be disconnected.");
            });
        }

        [Test]
        public void DisconnectingFromTheTabMenuClosesTheTabWhenTabsAreNotKeptOpenAfterDisconnect()
        {
            OptionsTabsPanelsPage.Default.KeepTabsOpenAfterDisconnect = false;
            _connectionTab.disconnectOnly = true;

            FormClosingEventArgs closingArgs = InvokeFormClosing();

            Assert.That(closingArgs.Cancel, Is.False);
        }

        private FormClosingEventArgs InvokeFormClosing()
        {
            FormClosingEventArgs closingArgs = new(CloseReason.UserClosing, false);

            MethodInfo onFormClosing = typeof(ConnectionTab)
                                           .GetMethod("OnFormClosing", BindingFlags.Instance | BindingFlags.NonPublic)
                                       ?? throw new AssertionException("Failed to resolve ConnectionTab.OnFormClosing.");

            onFormClosing.Invoke(_connectionTab, [closingArgs]);

            return closingArgs;
        }

        /// <summary>
        /// Stands in for a live protocol: it reports the disconnect without starting the
        /// background close thread that <see cref="ProtocolBase.Close"/> would spawn.
        /// </summary>
        private sealed class StubProtocol : ProtocolBase
        {
            public int CloseCallCount { get; private set; }

            public override void Close()
            {
                CloseCallCount++;
            }
        }
    }
}
