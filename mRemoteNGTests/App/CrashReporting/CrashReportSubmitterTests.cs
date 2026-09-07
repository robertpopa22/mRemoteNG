using System;
using System.Net.Http;
using System.Threading.Tasks;
using mRemoteNG.App.CrashReporting;
using NUnit.Framework;

namespace mRemoteNGTests.App.CrashReporting
{
    /// <summary>
    /// #175, #180 and #181 were one missing-assembly crash filed three times from one install,
    /// because every occurrence opened a new issue. The title is the signature; an open issue
    /// with the same title receives the occurrence as a comment instead.
    /// </summary>
    public class CrashReportSubmitterTests
    {
        private const string Title = "[Crash] System.IO.FileNotFoundException: Could not load file or assembly 'ExternalConnectors, Version=1.0.0.0, Cu...";
        private const string Body = "## Crash Report\n\nstack and environment";

        [Test]
        public async Task ACrashAlreadyTrackedByAnOpenIssueBecomesACommentThere()
        {
            FakeCrashReportGateway gateway = new();
            gateway.OpenCrashReports.Add(new CrashReportIssue(175, Title, "https://github.com/example/repo/issues/175"));
            gateway.OpenCrashReports.Add(new CrashReportIssue(178, "[Crash] Something else", "https://github.com/example/repo/issues/178"));

            CrashReportOutcome outcome = await CrashReportSubmitter.SubmitAsync(gateway, Title, Body);

            Assert.Multiple(() =>
            {
                Assert.That(outcome.AddedToExistingIssue, Is.True);
                Assert.That(outcome.Issue.Number, Is.EqualTo(175));
                Assert.That(outcome.Url, Does.Contain("issues/175#issuecomment"));
                Assert.That(gateway.CreatedIssues, Is.Empty, "a tracked crash must not open another issue");
                Assert.That(gateway.Comments, Has.Count.EqualTo(1));
                Assert.That(gateway.Comments[0].IssueNumber, Is.EqualTo(175));
                Assert.That(gateway.Comments[0].Body, Does.Contain(Body), "the occurrence keeps its own stack and environment");
                Assert.That(gateway.Comments[0].Body, Does.StartWith("**Same crash, reported again**"));
            });
        }

        [Test]
        public async Task ANewCrashOpensAnIssueWithTheCrashReportLabels()
        {
            FakeCrashReportGateway gateway = new();
            gateway.OpenCrashReports.Add(new CrashReportIssue(178, "[Crash] Something else", "https://github.com/example/repo/issues/178"));

            CrashReportOutcome outcome = await CrashReportSubmitter.SubmitAsync(gateway, Title, Body);

            Assert.Multiple(() =>
            {
                Assert.That(outcome.AddedToExistingIssue, Is.False);
                Assert.That(outcome.Url, Is.EqualTo(outcome.Issue.HtmlUrl));
                Assert.That(gateway.Comments, Is.Empty);
                Assert.That(gateway.CreatedIssues, Has.Count.EqualTo(1));
                Assert.That(gateway.CreatedIssues[0].Title, Is.EqualTo(Title));
                Assert.That(gateway.CreatedIssues[0].Body, Is.EqualTo(Body));
                Assert.That(gateway.CreatedIssues[0].Labels, Is.EquivalentTo(new[] { "bug", "crash-report", "auto-submitted" }));
            });
        }

        [Test]
        public async Task OnlyAnIdenticalTitleCountsAsTheSameCrash()
        {
            // Same exception type, different message: a different crash. A prefix or
            // case-insensitive match would fold unrelated FileNotFoundExceptions together.
            FakeCrashReportGateway gateway = new();
            gateway.OpenCrashReports.Add(new CrashReportIssue(175, Title.Replace("ExternalConnectors", "ObjectListView", StringComparison.Ordinal), "u"));
            gateway.OpenCrashReports.Add(new CrashReportIssue(176, Title.ToUpperInvariant(), "u"));
            gateway.OpenCrashReports.Add(new CrashReportIssue(177, Title[..40], "u"));

            CrashReportOutcome outcome = await CrashReportSubmitter.SubmitAsync(gateway, Title, Body);

            Assert.Multiple(() =>
            {
                Assert.That(outcome.AddedToExistingIssue, Is.False);
                Assert.That(gateway.CreatedIssues, Has.Count.EqualTo(1));
                Assert.That(gateway.Comments, Is.Empty);
            });
        }

        [Test]
        public async Task AFailedLookupStillFilesTheReportAsANewIssue()
        {
            // Losing a crash report to a lookup failure would be worse than a duplicate.
            FakeCrashReportGateway gateway = new() { ListFailure = new HttpRequestException("403 rate limited") };
            gateway.OpenCrashReports.Add(new CrashReportIssue(175, Title, "u"));

            CrashReportOutcome outcome = await CrashReportSubmitter.SubmitAsync(gateway, Title, Body);

            Assert.Multiple(() =>
            {
                Assert.That(outcome.AddedToExistingIssue, Is.False);
                Assert.That(gateway.CreatedIssues, Has.Count.EqualTo(1));
                Assert.That(gateway.Comments, Is.Empty);
            });
        }

        [Test]
        public void AFailedCreateIsNotSwallowed()
        {
            // The form falls back to the browser on failure; it can only do that if it hears about it.
            ThrowingGateway gateway = new();

            Assert.ThrowsAsync<HttpRequestException>(() => CrashReportSubmitter.SubmitAsync(gateway, Title, Body));
        }

        private sealed class ThrowingGateway : ICrashReportGateway
        {
            public Task<System.Collections.Generic.IReadOnlyList<CrashReportIssue>> ListOpenCrashReportsAsync(System.Threading.CancellationToken cancellationToken)
                => Task.FromResult<System.Collections.Generic.IReadOnlyList<CrashReportIssue>>(Array.Empty<CrashReportIssue>());

            public Task<CrashReportIssue> CreateIssueAsync(string title, string body, System.Collections.Generic.IReadOnlyList<string> labels, System.Threading.CancellationToken cancellationToken)
                => throw new HttpRequestException("500");

            public Task<string> AddCommentAsync(int issueNumber, string body, System.Threading.CancellationToken cancellationToken)
                => throw new HttpRequestException("500");
        }
    }
}
