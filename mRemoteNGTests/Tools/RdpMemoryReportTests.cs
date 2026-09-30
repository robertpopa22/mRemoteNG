using System;
using System.Diagnostics;
using System.IO;
using NUnit.Framework;

namespace mRemoteNGTests.Tools;

[TestFixture]
public class RdpMemoryReportTests
{
    [Test]
    public void InteractiveReportRejectsTheHistoricalFalseReassurance()
    {
        ProcessStartInfo start = new("pwsh") { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true };
        start.ArgumentList.Add("-NoProfile");
        start.ArgumentList.Add("-File");
        start.ArgumentList.Add(Path.Combine(AppContext.BaseDirectory, "scripts", "test-rdp-memory-summary.ps1"));
        using Process process = Process.Start(start)!;
        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();
        bool exited = process.WaitForExit(30_000);
        if (!exited) process.Kill(entireProcessTree: true);
        Assert.That(exited, Is.True, "memory report regression test timed out");
        Assert.That(process.ExitCode, Is.Zero, stdout.GetAwaiter().GetResult() + stderr.GetAwaiter().GetResult());
    }
}
