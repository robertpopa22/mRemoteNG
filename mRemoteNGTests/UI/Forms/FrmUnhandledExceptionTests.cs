using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using mRemoteNG.App.CrashReporting;
using mRemoteNG.Resources.Language;
using mRemoteNG.UI.Forms;
using mRemoteNGTests.App.CrashReporting;
using NUnit.Framework;

namespace mRemoteNGTests.UI.Forms;

[Apartment(ApartmentState.STA)]
public class FrmUnhandledExceptionTests
{
    private const string Folder = @"C:\Portable\mRemoteNG";

    [Test]
    public void AMissingShippedAssemblyIsReportedAsAnIncompleteInstallation()
    {
        // #178 arrived as a crash report: the connection tree could not be created because
        // ObjectListView.dll was not in the folder. The loader message names the assembly and
        // explains nothing, so it reads as an application bug.
        FileNotFoundException exception = new(
            "Could not load file or assembly 'ObjectListView, Version=2.9.3.0, Culture=neutral, PublicKeyToken=null'.",
            "ObjectListView, Version=2.9.3.0, Culture=neutral, PublicKeyToken=null");

        string description = FrmUnhandledException.DescribeException(exception, Folder);

        Assert.Multiple(() =>
        {
            Assert.That(description, Does.Contain("ObjectListView.dll"));
            Assert.That(description, Does.Contain(Folder));
            Assert.That(description, Does.Contain(exception.Message),
                        "the original loader message must still be there for the crash report");
        });
    }

    [Test]
    public void AnAssemblyMissingBehindAnotherExceptionIsStillRecognised()
    {
        FileNotFoundException inner = new(
            "Could not load file or assembly 'WeifenLuo.WinFormsUI.Docking, Version=3.1.0.0'.",
            "WeifenLuo.WinFormsUI.Docking, Version=3.1.0.0, Culture=neutral, PublicKeyToken=null");
        TypeInitializationException exception = new("mRemoteNG.UI.Window.ConnectionTreeWindow", inner);

        Assert.That(FrmUnhandledException.DescribeException(exception, Folder),
                    Does.Contain("WeifenLuo.WinFormsUI.Docking.dll"));
    }

    [Test]
    public void AnOrdinaryMissingFileIsNotCalledAnIncompleteInstallation()
    {
        // FileNotFoundException also reports plain data files. Telling someone to reinstall
        // because their connections file moved would be worse than saying nothing.
        FileNotFoundException exception = new("Could not find file 'confCons.xml'.",
                                              @"C:\Portable\mRemoteNG\Settings\confCons.xml");

        Assert.That(FrmUnhandledException.DescribeException(exception, Folder),
                    Is.EqualTo(exception.Message));
    }

    [Test]
    public void AnUnrelatedExceptionIsLeftExactlyAsItIs()
    {
        InvalidOperationException exception = new("Cross-thread operation not valid.");

        Assert.That(FrmUnhandledException.DescribeException(exception, Folder),
                    Is.EqualTo(exception.Message));
    }

    // The real form, its real title/body builders and its real button, driven through the
    // gateway seam. #175, #180 and #181 were this exact crash filed three times; the form
    // must send the third occurrence to the issue that already tracks it.

    private static FileNotFoundException TheExternalConnectorsCrash()
    {
        return new FileNotFoundException(
            "Could not load file or assembly 'ExternalConnectors, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null'. The system cannot find the file specified.",
            "ExternalConnectors, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null");
    }

    private static string TitleFor(Exception exception)
    {
        // The form's own title rule: type, colon, the first 80 characters of the message.
        string message = exception.Message.Length <= 80 ? exception.Message : exception.Message[..80] + "...";
        return $"[Crash] {exception.GetType().FullName}: {message}";
    }

    [Test]
    public async Task ACrashAlreadyOpenAsAnIssueIsAddedThereAndTheButtonSaysSo()
    {
        FileNotFoundException crash = TheExternalConnectorsCrash();
        FakeCrashReportGateway gateway = new();
        gateway.OpenCrashReports.Add(new CrashReportIssue(175, TitleFor(crash), "https://github.com/example/repo/issues/175"));
        using FrmUnhandledException form = new(crash, false) { Gateway = gateway };

        CrashReportOutcome? outcome = await form.SubmitAsync(gateway);

        Assert.Multiple(() =>
        {
            Assert.That(outcome, Is.Not.Null);
            Assert.That(outcome!.AddedToExistingIssue, Is.True);
            Assert.That(form.LastOutcome, Is.SameAs(outcome));
            Assert.That(gateway.CreatedIssues, Is.Empty);
            Assert.That(gateway.Comments, Has.Count.EqualTo(1));
            Assert.That(gateway.Comments[0].IssueNumber, Is.EqualTo(175));
            Assert.That(gateway.Comments[0].Body, Does.Contain("### Stack Trace").And.Contain("### Environment"),
                        "the occurrence carries its own stack and environment, like a fresh report would");
            Assert.That(form.SubmitButtonText, Is.EqualTo("Added to #175"));
            Assert.That(form.SubmitButtonEnabled, Is.False, "one report per dialog");
        });
    }

    [Test]
    public async Task ACrashNobodyHasReportedOpensANewIssue()
    {
        FileNotFoundException crash = TheExternalConnectorsCrash();
        FakeCrashReportGateway gateway = new();
        gateway.OpenCrashReports.Add(new CrashReportIssue(149, "[Crash] System.ArgumentOutOfRangeException: startIndex ('427') must be less than '41'.", "u"));
        using FrmUnhandledException form = new(crash, false) { Gateway = gateway };

        CrashReportOutcome? outcome = await form.SubmitAsync(gateway);

        Assert.Multiple(() =>
        {
            Assert.That(outcome!.AddedToExistingIssue, Is.False);
            Assert.That(gateway.Comments, Is.Empty);
            Assert.That(gateway.CreatedIssues, Has.Count.EqualTo(1));
            Assert.That(gateway.CreatedIssues[0].Title, Is.EqualTo(TitleFor(crash)));
            Assert.That(gateway.CreatedIssues[0].Body, Does.Contain("ExternalConnectors").And.Contain("Non-fatal"));
            Assert.That(form.SubmitButtonText, Is.EqualTo("Submitted!"));
        });
    }

    [Test]
    public async Task AFailedSubmissionReArmsTheButtonAndReportsNothing()
    {
        FailingGateway gateway = new();
        using FrmUnhandledException form = new(TheExternalConnectorsCrash(), false) { Gateway = gateway };

        CrashReportOutcome? outcome = await form.SubmitAsync(gateway);

        Assert.Multiple(() =>
        {
            Assert.That(outcome, Is.Null);
            Assert.That(form.LastOutcome, Is.Null);
            Assert.That(form.SubmitButtonEnabled, Is.True, "the user must be able to try again or fall back to the browser");
            Assert.That(form.SubmitButtonText, Is.EqualTo(Language.SubmitErrorReport));
        });
    }

    private sealed class FailingGateway : ICrashReportGateway
    {
        public Task<System.Collections.Generic.IReadOnlyList<CrashReportIssue>> ListOpenCrashReportsAsync(CancellationToken cancellationToken)
            => Task.FromResult<System.Collections.Generic.IReadOnlyList<CrashReportIssue>>(Array.Empty<CrashReportIssue>());

        public Task<CrashReportIssue> CreateIssueAsync(string title, string body, System.Collections.Generic.IReadOnlyList<string> labels, CancellationToken cancellationToken)
            => throw new HttpRequestException("network down");

        public Task<string> AddCommentAsync(int issueNumber, string body, CancellationToken cancellationToken)
            => throw new HttpRequestException("network down");
    }
}
