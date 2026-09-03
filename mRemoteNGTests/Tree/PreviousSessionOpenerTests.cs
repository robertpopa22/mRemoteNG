using System;
using System.Linq;
using mRemoteNG.Connection;
using mRemoteNG.Tree;
using mRemoteNG.Tree.Root;
using mRemoteNG.UI.Controls.ConnectionTree;
using NSubstitute;
using NUnit.Framework;

namespace mRemoteNGTests.Tree
{
    public class PreviousSessionOpenerTests
    {
        private PreviousSessionOpener _previousSessionOpener = null!;
        private IConnectionInitiator _connectionInitiator = null!;
        private IConnectionTree _connectionTree = null!;

        [SetUp]
        public void Setup()
        {
            _connectionInitiator = Substitute.For<IConnectionInitiator>();
            _connectionInitiator.ActiveConnections.Returns(Array.Empty<string>());
            _previousSessionOpener = new PreviousSessionOpener(_connectionInitiator, () => Array.Empty<ConnectionInfo>());
            _connectionTree = Substitute.For<IConnectionTree>();
            _connectionTree.GetRootConnectionNode().Returns(BuildTree());
        }

        [Test]
        public void AllRequestedSessionsAreReopened()
        {
            _previousSessionOpener.Execute(_connectionTree);
            _connectionInitiator.ReceivedWithAnyArgs(2).OpenConnection(new ConnectionInfo());
        }

        [Test]
        public void PreviouslyOpenedQuickConnectSessionsAreReopened()
        {
            ConnectionInfo quickConnect = new() { IsQuickConnect = true, PleaseConnect = true };
            _previousSessionOpener = new PreviousSessionOpener(_connectionInitiator, () => [quickConnect]);

            _previousSessionOpener.Execute(_connectionTree);

            _connectionInitiator.ReceivedWithAnyArgs().OpenConnection(quickConnect);
            _connectionInitiator.ReceivedWithAnyArgs(3).OpenConnection(new ConnectionInfo());
        }

        [Test]
        public void ActiveQuickConnectSessionsAreNotReopened()
        {
            ConnectionInfo quickConnect = new() { IsQuickConnect = true, PleaseConnect = true };
            _connectionInitiator.ActiveConnections.Returns([quickConnect.ConstantID]);
            _previousSessionOpener = new PreviousSessionOpener(_connectionInitiator, () => [quickConnect]);

            _previousSessionOpener.Execute(_connectionTree);

            _connectionInitiator.ReceivedWithAnyArgs(2).OpenConnection(new ConnectionInfo());
        }

        [Test]
        public void ConnectionsTheSavedLayoutAlreadyOpenedAreNotReopened()
        {
            RootNodeInfo root = BuildTree();
            _connectionTree.GetRootConnectionNode().Returns(root);
            string openedByLayout = root.Children.First(child => child.PleaseConnect).ConstantID;
            _previousSessionOpener = new PreviousSessionOpener(
                _connectionInitiator,
                () => Array.Empty<ConnectionInfo>(),
                () => [openedByLayout]);

            _previousSessionOpener.Execute(_connectionTree);

            _connectionInitiator.ReceivedWithAnyArgs(1).OpenConnection(new ConnectionInfo());
        }

        /// <summary>
        /// The first fix for #172 skipped whatever the layout INTENDED to open. When the layout then
        /// opened nothing — a disposed panel, ids the connections file no longer holds — both paths
        /// stood aside and the reporter got no tabs at all. Reporting nothing as opened must mean
        /// this path reopens everything.
        /// </summary>
        [Test]
        public void EverythingIsReopenedWhenTheLayoutOpenedNothing()
        {
            RootNodeInfo root = BuildTree();
            _connectionTree.GetRootConnectionNode().Returns(root);
            _previousSessionOpener = new PreviousSessionOpener(
                _connectionInitiator,
                () => Array.Empty<ConnectionInfo>(),
                Array.Empty<string>);

            _previousSessionOpener.Execute(_connectionTree);

            _connectionInitiator.ReceivedWithAnyArgs(2).OpenConnection(new ConnectionInfo());
        }

        [Test]
        public void ExceptionThrownWhenConstructorGivenNullArg()
        {
            // ReSharper disable once ObjectCreationAsStatement
            Assert.Throws<ArgumentNullException>(() => new PreviousSessionOpener(null));
        }

        private static RootNodeInfo BuildTree()
        {
            var root = new RootNodeInfo(RootNodeType.Connection);
            root.AddChild(new ConnectionInfo { PleaseConnect = true });
            root.AddChild(new ConnectionInfo());
            root.AddChild(new ConnectionInfo { PleaseConnect = true });
            return root;
        }
    }
}
