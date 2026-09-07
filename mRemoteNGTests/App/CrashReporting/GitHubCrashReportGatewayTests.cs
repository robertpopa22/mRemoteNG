using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using mRemoteNG.App.CrashReporting;
using NUnit.Framework;

namespace mRemoteNGTests.App.CrashReporting
{
    /// <summary>The HTTP layer, answered by a scripted handler instead of GitHub.</summary>
    public class GitHubCrashReportGatewayTests
    {
        private const string IssuesUrl = "https://api.github.com/repos/owner/repo/issues";

        [Test]
        public async Task ListingAsksForOpenCrashReportsOnlyAndDropsPullRequests()
        {
            ScriptedHandler handler = new((request, _) =>
            {
                Assert.That(request.Method, Is.EqualTo(HttpMethod.Get));
                Assert.That(request.RequestUri!.ToString(), Does.StartWith(IssuesUrl + "?"));
                Assert.That(request.RequestUri.Query, Does.Contain("state=open").And.Contain("labels=crash-report"));
                Assert.That(request.Headers.Authorization!.Parameter, Is.EqualTo("secret"));
                return Json(HttpStatusCode.OK,
                    "[{\"number\":175,\"title\":\"[Crash] A\",\"html_url\":\"https://github.com/owner/repo/issues/175\"},"
                    + "{\"number\":176,\"title\":\"[Crash] B\",\"html_url\":\"https://github.com/owner/repo/pull/176\",\"pull_request\":{}}]");
            });
            using GitHubCrashReportGateway gateway = new("owner", "repo", "secret", "mRemoteNG/1.0", handler);

            IReadOnlyList<CrashReportIssue> issues = await gateway.ListOpenCrashReportsAsync(CancellationToken.None);

            Assert.Multiple(() =>
            {
                Assert.That(issues, Has.Count.EqualTo(1));
                Assert.That(issues[0].Number, Is.EqualTo(175));
                Assert.That(issues[0].Title, Is.EqualTo("[Crash] A"));
            });
        }

        [Test]
        public async Task CreatingPostsTitleBodyAndLabelsToTheIssuesEndpoint()
        {
            string? sentJson = null;
            ScriptedHandler handler = new((request, content) =>
            {
                Assert.That(request.Method, Is.EqualTo(HttpMethod.Post));
                Assert.That(request.RequestUri!.ToString(), Is.EqualTo(IssuesUrl));
                sentJson = content;
                return Json(HttpStatusCode.Created, "{\"number\":900,\"title\":\"[Crash] New\",\"html_url\":\"https://github.com/owner/repo/issues/900\"}");
            });
            using GitHubCrashReportGateway gateway = new("owner", "repo", "secret", "mRemoteNG/1.0", handler);

            CrashReportIssue issue = await gateway.CreateIssueAsync("[Crash] New", "body text", new[] { "bug", "crash-report" }, CancellationToken.None);

            using JsonDocument sent = JsonDocument.Parse(sentJson!);
            Assert.Multiple(() =>
            {
                Assert.That(issue.Number, Is.EqualTo(900));
                Assert.That(issue.HtmlUrl, Does.EndWith("/issues/900"));
                Assert.That(sent.RootElement.GetProperty("title").GetString(), Is.EqualTo("[Crash] New"));
                Assert.That(sent.RootElement.GetProperty("body").GetString(), Is.EqualTo("body text"));
                Assert.That(sent.RootElement.GetProperty("labels").GetArrayLength(), Is.EqualTo(2));
            });
        }

        [Test]
        public async Task CommentingPostsToTheIssuesCommentsEndpoint()
        {
            string? sentJson = null;
            ScriptedHandler handler = new((request, content) =>
            {
                Assert.That(request.Method, Is.EqualTo(HttpMethod.Post));
                Assert.That(request.RequestUri!.ToString(), Is.EqualTo(IssuesUrl + "/175/comments"));
                sentJson = content;
                return Json(HttpStatusCode.Created, "{\"html_url\":\"https://github.com/owner/repo/issues/175#issuecomment-1\"}");
            });
            using GitHubCrashReportGateway gateway = new("owner", "repo", "secret", "mRemoteNG/1.0", handler);

            string url = await gateway.AddCommentAsync(175, "seen again", CancellationToken.None);

            using JsonDocument sent = JsonDocument.Parse(sentJson!);
            Assert.Multiple(() =>
            {
                Assert.That(url, Does.EndWith("#issuecomment-1"));
                Assert.That(sent.RootElement.GetProperty("body").GetString(), Is.EqualTo("seen again"));
            });
        }

        [Test]
        public void ARejectedRequestThrowsWithTheStatusInTheMessage()
        {
            ScriptedHandler handler = new((_, _) => Json(HttpStatusCode.Forbidden, "{\"message\":\"rate limited\"}"));
            using GitHubCrashReportGateway gateway = new("owner", "repo", "secret", "mRemoteNG/1.0", handler);

            HttpRequestException? ex = Assert.ThrowsAsync<HttpRequestException>(
                () => gateway.AddCommentAsync(175, "seen again", CancellationToken.None));

            Assert.That(ex!.Message, Does.Contain("403").And.Contain("rate limited"));
        }

        private static HttpResponseMessage Json(HttpStatusCode status, string json)
        {
            return new HttpResponseMessage(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
        }

        private sealed class ScriptedHandler : HttpMessageHandler
        {
            private readonly Func<HttpRequestMessage, string?, HttpResponseMessage> _answer;

            public ScriptedHandler(Func<HttpRequestMessage, string?, HttpResponseMessage> answer)
            {
                _answer = answer;
            }

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                string? content = request.Content == null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
                HttpResponseMessage response = _answer(request, content);
                response.RequestMessage = request;
                return response;
            }
        }
    }
}
