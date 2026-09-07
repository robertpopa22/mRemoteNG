using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace mRemoteNG.App.CrashReporting
{
    /// <summary>
    /// The three things the crash reporter does against the issue tracker. Separated from the
    /// HTTP so the decision logic and the form can be exercised without GitHub.
    /// </summary>
    public interface ICrashReportGateway
    {
        /// <summary>Open issues carrying the crash-report label, pull requests excluded.</summary>
        Task<IReadOnlyList<CrashReportIssue>> ListOpenCrashReportsAsync(CancellationToken cancellationToken);

        Task<CrashReportIssue> CreateIssueAsync(string title, string body, IReadOnlyList<string> labels, CancellationToken cancellationToken);

        /// <summary>Adds a comment and returns its URL.</summary>
        Task<string> AddCommentAsync(int issueNumber, string body, CancellationToken cancellationToken);
    }
}
