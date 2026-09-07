namespace mRemoteNG.App.CrashReporting
{
    /// <summary>
    /// Where a crash report ended up: a new issue, or a comment on the open issue that already
    /// tracks the same crash. <see cref="Url"/> is the thing to show the user — the new issue,
    /// or the comment that was added.
    /// </summary>
    public sealed record CrashReportOutcome(CrashReportIssue Issue, bool AddedToExistingIssue, string Url);
}
