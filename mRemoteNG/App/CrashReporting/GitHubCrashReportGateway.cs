using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace mRemoteNG.App.CrashReporting
{
    /// <summary>GitHub's REST issues API, with the crash-report token.</summary>
    public sealed class GitHubCrashReportGateway : ICrashReportGateway, IDisposable
    {
        private readonly HttpClient _client;
        private readonly string _issuesUrl;

        public GitHubCrashReportGateway(string owner, string repo, string token, string userAgent)
            : this(owner, repo, token, userAgent, new HttpClientHandler())
        {
        }

        /// <summary>The handler is the seam the tests use to answer as GitHub would.</summary>
        public GitHubCrashReportGateway(string owner, string repo, string token, string userAgent, HttpMessageHandler handler)
        {
            ArgumentNullException.ThrowIfNull(owner);
            ArgumentNullException.ThrowIfNull(repo);
            ArgumentNullException.ThrowIfNull(token);
            ArgumentNullException.ThrowIfNull(userAgent);
            ArgumentNullException.ThrowIfNull(handler);

            _client = new HttpClient(handler);
            _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            _client.DefaultRequestHeaders.UserAgent.ParseAdd(userAgent);
            _issuesUrl = $"https://api.github.com/repos/{owner}/{repo}/issues";
        }

        public async Task<IReadOnlyList<CrashReportIssue>> ListOpenCrashReportsAsync(CancellationToken cancellationToken)
        {
            using HttpResponseMessage response = await _client.GetAsync($"{_issuesUrl}?state=open&labels=crash-report&per_page=100", cancellationToken);
            await ThrowIfFailedAsync(response, cancellationToken);

            using JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
            List<CrashReportIssue> issues = new();
            foreach (JsonElement element in document.RootElement.EnumerateArray())
            {
                // The issues endpoint lists pull requests too; a PR cannot track a crash.
                if (element.TryGetProperty("pull_request", out _))
                    continue;

                issues.Add(ReadIssue(element));
            }

            return issues;
        }

        public async Task<CrashReportIssue> CreateIssueAsync(string title, string body, IReadOnlyList<string> labels, CancellationToken cancellationToken)
        {
            using HttpResponseMessage response = await PostAsync(_issuesUrl, new { title, body, labels }, cancellationToken);
            using JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
            return ReadIssue(document.RootElement);
        }

        public async Task<string> AddCommentAsync(int issueNumber, string body, CancellationToken cancellationToken)
        {
            string url = FormattableString.Invariant($"{_issuesUrl}/{issueNumber}/comments");
            using HttpResponseMessage response = await PostAsync(url, new { body }, cancellationToken);
            using JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
            return document.RootElement.GetProperty("html_url").GetString() ?? string.Empty;
        }

        private async Task<HttpResponseMessage> PostAsync(string url, object payload, CancellationToken cancellationToken)
        {
            using StringContent content = new(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            HttpResponseMessage response = await _client.PostAsync(url, content, cancellationToken);
            try
            {
                await ThrowIfFailedAsync(response, cancellationToken);
                return response;
            }
            catch
            {
                response.Dispose();
                throw;
            }
        }

        private static async Task ThrowIfFailedAsync(HttpResponseMessage response, CancellationToken cancellationToken)
        {
            if (response.IsSuccessStatusCode)
                return;

            string detail = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException(
                FormattableString.Invariant($"GitHub answered {(int)response.StatusCode} {response.ReasonPhrase} to {response.RequestMessage?.Method} {response.RequestMessage?.RequestUri}: {Truncate(detail, 300)}"),
                null,
                response.StatusCode);
        }

        private static CrashReportIssue ReadIssue(JsonElement element)
        {
            return new CrashReportIssue(
                element.GetProperty("number").GetInt32(),
                element.GetProperty("title").GetString() ?? string.Empty,
                element.GetProperty("html_url").GetString() ?? string.Empty);
        }

        private static string Truncate(string value, int maxLength)
        {
            return value.Length <= maxLength ? value : value[..maxLength] + "...";
        }

        public void Dispose()
        {
            _client.Dispose();
        }
    }
}
