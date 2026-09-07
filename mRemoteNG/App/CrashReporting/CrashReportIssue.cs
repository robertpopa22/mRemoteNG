namespace mRemoteNG.App.CrashReporting
{
    /// <summary>An issue in the crash-report repository, as much of it as the reporter needs.</summary>
    public sealed record CrashReportIssue(int Number, string Title, string HtmlUrl);
}
