using System;
using System.Linq;
using log4net.Appender;
using mRemoteNG.App;
using mRemoteNG.App.Diagnostics;
using NUnit.Framework;

namespace mRemoteNGTests.App;

[TestFixture]
[NonParallelizable]
public class OptionsLoadSpanTests
{
    private MemoryAppender _capture;
    private log4net.Repository.Hierarchy.Logger _logger;

    [SetUp]
    public void CaptureDiagnostics()
    {
        _logger = (log4net.Repository.Hierarchy.Logger)Logger.Instance.Log.Logger;
        _capture = new MemoryAppender();
        _logger.AddAppender(_capture);
    }

    [TearDown]
    public void RemoveCapture() => _logger.RemoveAppender(_capture);

    private string[] Lines() => _capture.GetEvents().Select(e => e.RenderedMessage)
        .Where(line => line.Contains("event=options_load", StringComparison.Ordinal)).ToArray();

    [Test]
    public void AStartedOperationIsRecordedBeforeItCanBlock()
    {
        using (new OptionsLoadSpan(OptionsLoadStage.PageSettings, "AdvancedPage"))
        {
            Assert.That(Lines(), Has.Length.EqualTo(1));
            Assert.That(Lines()[0], Does.Contain("phase=begin"));
        }
        Assert.That(Lines()[1], Does.Contain("phase=leave"));
    }

    [Test]
    public void NestedFailurePreservesTheExceptionAndRestoresTheParent()
    {
        int previous = OptionsLoadSpan.CurrentId;
        var original = new InvalidOperationException("synthetic operation failure");
        using (new OptionsLoadSpan(OptionsLoadStage.Open))
        {
            int parent = OptionsLoadSpan.CurrentId;
            var observed = Assert.Throws<InvalidOperationException>(() =>
            {
                using var child = new OptionsLoadSpan(OptionsLoadStage.PageSettings, "AdvancedPage");
                throw original;
            });
            Assert.That(observed, Is.SameAs(original));
            Assert.That(OptionsLoadSpan.CurrentId, Is.EqualTo(parent));
            Assert.That(Lines()[1], Does.Contain($"parent={parent} "));
            Assert.That(Lines()[2], Does.Contain("phase=leave"));
        }
        Assert.That(OptionsLoadSpan.CurrentId, Is.EqualTo(previous));
        Assert.That(Lines(), Has.Length.EqualTo(4));
    }

    [Test]
    public void UnknownPageTextCannotEnterTheDiagnostic()
    {
        const string privateText = "synthetic-private-path-and-caption";
        using (new OptionsLoadSpan(OptionsLoadStage.PageSettings, privateText)) { }
        Assert.That(Lines(), Has.All.Contains("page=other"));
        Assert.That(string.Join("\n", Lines()), Does.Not.Contain(privateText));
    }
}
