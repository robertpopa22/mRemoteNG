using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace mRemoteNG.App.CrashReporting
{
    /// <summary>
    /// Files a crash report where it belongs. One defect used to produce one issue per
    /// occurrence — #175, #180 and #181 are the same missing-assembly crash from the same
    /// install, three hours apart — because the reporter never looked at what was already open.
    /// The issue title is the crash signature (exception type plus the start of its message),
    /// so an open crash report with the identical title gets the new occurrence as a comment.
    /// Closed issues are deliberately not matched: a crash that comes back after a fix is new
    /// information and deserves its own issue.
    /// </summary>
    public static class CrashReportSubmitter
    {
        public static readonly IReadOnlyList<string> Labels = new[] { "bug", "crash-report", "auto-submitted" };

        public static async Task<CrashReportOutcome> SubmitAsync(ICrashReportGateway gateway, string title, string body, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(gateway);
            ArgumentNullException.ThrowIfNull(title);
            ArgumentNullException.ThrowIfNull(body);

            CrashReportIssue? existing = await FindOpenDuplicateAsync(gateway, title, cancellationToken);
            if (existing != null)
            {
                string commentUrl = await gateway.AddCommentAsync(existing.Number, DuplicateComment(body), cancellationToken);
                return new CrashReportOutcome(existing, true, commentUrl);
            }

            CrashReportIssue created = await gateway.CreateIssueAsync(title, body, Labels, cancellationToken);
            return new CrashReportOutcome(created, false, created.HtmlUrl);
        }

        /// <summary>
        /// The lookup is best effort: if it fails, the report is filed as a new issue rather than
        /// lost, and the duplicate is cheap to merge by hand. Only an exact title match counts —
        /// a different message means a different crash, even for the same exception type.
        /// </summary>
        private static async Task<CrashReportIssue?> FindOpenDuplicateAsync(ICrashReportGateway gateway, string title, CancellationToken cancellationToken)
        {
            try
            {
                IReadOnlyList<CrashReportIssue> open = await gateway.ListOpenCrashReportsAsync(cancellationToken);
                return open.FirstOrDefault(issue => string.Equals(issue.Title, title, StringComparison.Ordinal));
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                Runtime.MessageCollector?.AddExceptionMessage("Crash report duplicate lookup failed; filing a new issue", ex);
                return null;
            }
        }

        public static string DuplicateComment(string body)
        {
            return "**Same crash, reported again** — added here instead of opening another issue."
                   + Environment.NewLine + Environment.NewLine + body;
        }
    }
}
