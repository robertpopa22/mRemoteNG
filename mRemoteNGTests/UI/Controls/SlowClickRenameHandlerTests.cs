using System;
using mRemoteNG.Connection;
using mRemoteNG.Tree.Root;
using mRemoteNG.UI.Controls.ConnectionTree;
using NSubstitute;
using NUnit.Framework;

namespace mRemoteNGTests.UI.Controls;

[TestFixture]
public class SlowClickRenameHandlerTests
{
    private ISlowClickRenameTimer _timer;
    private int _renameCount;
    private ConnectionInfo _selectedNode;
    private SlowClickRenameHandler _handler;

    [SetUp]
    public void Setup()
    {
        _timer = Substitute.For<ISlowClickRenameTimer>();
        _renameCount = 0;
        _selectedNode = null;
        _handler = new SlowClickRenameHandler(_timer, () => _renameCount++, () => _selectedNode);
    }

    [TearDown]
    public void Teardown()
    {
        _handler.Dispose();
    }

    private void FireTick()
    {
        _timer.Tick += Raise.EventWith(_timer, EventArgs.Empty);
    }

    [Test]
    public void FirstClickDoesNotStartTheTimer()
    {
        _handler.Execute(new ConnectionInfo());

        _timer.DidNotReceive().Start();
    }

    [Test]
    public void SecondClickOnTheSameNodeStartsTheTimer()
    {
        var node = new ConnectionInfo();

        _handler.Execute(node);
        _handler.Execute(node);

        _timer.Received(1).Start();
    }

    [Test]
    public void TimerTickTriggersRenameWhenTheClickedNodeIsStillSelected()
    {
        var node = new ConnectionInfo();
        _selectedNode = node;

        _handler.Execute(node);
        _handler.Execute(node);
        FireTick();

        Assert.That(_renameCount, Is.EqualTo(1));
    }

    [Test]
    public void TimerTickDoesNotRenameWhenSelectionMovedElsewhere()
    {
        var node = new ConnectionInfo();
        _selectedNode = new ConnectionInfo();

        _handler.Execute(node);
        _handler.Execute(node);
        FireTick();

        Assert.That(_renameCount, Is.Zero);
    }

    [Test]
    public void ClickingADifferentNodeDoesNotStartTheTimer()
    {
        _handler.Execute(new ConnectionInfo());
        _handler.Execute(new ConnectionInfo());

        _timer.DidNotReceive().Start();
    }

    [Test]
    public void CancelPreventsThePendingRename()
    {
        var node = new ConnectionInfo();
        _selectedNode = node;

        _handler.Execute(node);
        _handler.Execute(node);
        _handler.Cancel();
        FireTick();

        Assert.That(_renameCount, Is.Zero);
    }

    [Test]
    public void CancelIfDifferentNodeKeepsPendingRenameForTheSameNode()
    {
        var node = new ConnectionInfo();
        _selectedNode = node;

        _handler.Execute(node);
        _handler.CancelIfDifferentNode(node);
        _handler.Execute(node);

        _timer.Received(1).Start();
    }

    [Test]
    public void CancelIfDifferentNodeCancelsWhenSelectionMoved()
    {
        var node = new ConnectionInfo();

        _handler.Execute(node);
        _handler.CancelIfDifferentNode(new ConnectionInfo());
        _handler.Execute(node);

        // The pending node was cleared, so the second Execute is a "first click" again.
        _timer.DidNotReceive().Start();
    }

    [Test]
    public void RootNodesAreNotRenameEligible()
    {
        var root = new RootNodeInfo(RootNodeType.Connection);

        _handler.Execute(root);
        _handler.Execute(root);

        _timer.DidNotReceive().Start();
    }

    [Test]
    public void DisposeUnsubscribesAndDisposesTheTimer()
    {
        _handler.Dispose();

        _timer.Received(1).Dispose();
    }
}
