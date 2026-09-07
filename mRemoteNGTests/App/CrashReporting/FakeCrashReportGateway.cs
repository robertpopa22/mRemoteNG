using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using mRemoteNG.App.CrashReporting;

namespace mRemoteNGTests.App.CrashReporting
{
    /// <summary>Answers as the tracker would, and remembers what it was asked to do.</summary>
    internal sealed class FakeCrashReportGateway : ICrashReportGateway
    {
        public List<CrashReportIssue> OpenCrashReports { get; } = [];
        public Exception? ListFailure { get; set; }
        public List<(string Title, string Body, IReadOnlyList<string> Labels)> CreatedIssues { get; } = [];
        public List<(int IssueNumber, string Body)> Comments { get; } = [];
        public int NextIssueNumber { get; set; } = 900;

        public Task<IReadOnlyList<CrashReportIssue>> ListOpenCrashReportsAsync(CancellationToken cancellationToken)
        {
            if (ListFailure != null)
                throw ListFailure;

            return Task.FromResult<IReadOnlyList<CrashReportIssue>>(OpenCrashReports.AsReadOnly());
        }

        public Task<CrashReportIssue> CreateIssueAsync(string title, string body, IReadOnlyList<string> labels, CancellationToken cancellationToken)
        {
            CreatedIssues.Add((title, body, labels));
            int number = NextIssueNumber++;
            return Task.FromResult(new CrashReportIssue(number, title, $"https://github.com/example/repo/issues/{number}"));
        }

        public Task<string> AddCommentAsync(int issueNumber, string body, CancellationToken cancellationToken)
        {
            Comments.Add((issueNumber, body));
            return Task.FromResult($"https://github.com/example/repo/issues/{issueNumber}#issuecomment-{Comments.Count}");
        }
    }
}
