using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using mRemoteNG.App;
using mRemoteNG.App.CrashReporting;
using mRemoteNG.App.Info;
using mRemoteNG.Resources.Language;

namespace mRemoteNG.UI.Forms
{
    [SupportedOSPlatform("windows")]
    public partial class FrmUnhandledException : Form
    {
        private readonly bool _isFatal;
        private readonly Exception? _exception;
        private bool _submitted;

        public FrmUnhandledException()
            : this(null, false)
        {
        }

        public FrmUnhandledException(Exception? exception, bool isFatal)
        {
            _isFatal = isFatal;
            _exception = exception;
            InitializeComponent();
            SetLanguage();

            if (exception == null)
                return;

            textBoxExceptionMessage.Text = DescribeException(exception, AppDomain.CurrentDomain.BaseDirectory);
            textBoxStackTrace.Text = exception.Demystify().StackTrace;
            SetEnvironmentText();
        }

        /// <summary>
        /// An assembly that ships beside mRemoteNG.exe cannot be missing unless the installation
        /// itself is incomplete - a half-extracted download, a file quarantined by antivirus, or
        /// two different downloads mixed in one folder. The raw loader message names the assembly
        /// but says none of that, so the report reads as an application bug and the user has
        /// nothing to act on (#178). Say what is actually wrong, above the original message.
        /// </summary>
        public static string DescribeException(Exception exception, string applicationFolder)
        {
            ArgumentNullException.ThrowIfNull(exception);

            string? missingAssembly = MissingAssemblyFileName(exception);
            if (missingAssembly == null)
                return exception.Message;

            return string.Format(CultureInfo.CurrentCulture, Language.InstallationIncompleteMissingAssembly,
                                 missingAssembly, applicationFolder)
                   + Environment.NewLine + Environment.NewLine
                   + exception.Message;
        }

        /// <summary>
        /// The file name of the assembly a load failure was about, or null when the failure was
        /// about something other than an assembly. FileNotFoundException also reports ordinary
        /// missing files, so an assembly is recognised by its display name carrying a version.
        /// </summary>
        private static string? MissingAssemblyFileName(Exception exception)
        {
            for (Exception? current = exception; current != null; current = current.InnerException)
            {
                string? fileName = current switch
                {
                    FileNotFoundException notFound => notFound.FileName,
                    FileLoadException loadFailed => loadFailed.FileName,
                    _ => null
                };

                if (string.IsNullOrWhiteSpace(fileName) ||
                    !fileName.Contains(", Version=", StringComparison.Ordinal))
                    continue;

                string simpleName = fileName[..fileName.IndexOf(',', StringComparison.Ordinal)].Trim();
                if (simpleName.Length > 0)
                    return simpleName + ".dll";
            }

            return null;
        }

        private void SetEnvironmentText()
        {
            textBoxEnvironment.Text = new StringBuilder()
                .AppendLine(CultureInfo.InvariantCulture, $"OS: {Environment.OSVersion}")
                .AppendLine(CultureInfo.InvariantCulture, $"{GeneralAppInfo.ProductName} Version: {GeneralAppInfo.ApplicationVersion}")
                .AppendLine("Edition: " + (Runtime.IsPortableEdition ? "Portable" : "MSI"))
                .AppendLine("Cmd line args: " + string.Join(" ", Environment.GetCommandLineArgs().Skip(1)))
                .ToString();
        }

        private void SetLanguage()
        {
            Text = Language.mRemoteNGUnhandledException;
            labelExceptionCaught.Text = Language.UnhandledExceptionOccured;

            labelExceptionIsFatalHeader.Text = _isFatal
                ? Language.ExceptionForcesmRemoteNGToClose
                : string.Empty;

            labelExceptionMessageHeader.Text = Language.ExceptionMessage;
            labelStackTraceHeader.Text = Language.StackTrace;
            labelEnvironment.Text = Language.Environment;
            buttonCreateBug.Text = Language.SubmitErrorReport;
            buttonCopyAll.Text = Language.CopyAll;
            buttonClose.Text = _isFatal
                ? Language.Exit
                : Language._Close;
        }

        private void buttonCopyAll_Click(object sender, EventArgs e)
        {
            string text = new StringBuilder()
               .AppendLine("```")
               .AppendLine(labelExceptionMessageHeader.Text)
               .AppendLine("\"" + textBoxExceptionMessage.Text + "\"")
               .AppendLine()
               .AppendLine(labelStackTraceHeader.Text)
               .AppendLine(textBoxStackTrace.Text)
               .AppendLine()
               .AppendLine(labelEnvironment.Text)
               .AppendLine(textBoxEnvironment.Text)
               .AppendLine("```")
               .ToString();

            Clipboard.SetText(text);
        }

        private void buttonClose_Click(object sender, EventArgs e)
        {
            if (_isFatal)
                Shutdown.Quit();

            Close();
        }

        /// <summary>
        /// Where the report goes. Null means GitHub with the token compiled into this build;
        /// tests set a stand-in so the form can be driven without a network or a real tracker.
        /// </summary>
        public ICrashReportGateway? Gateway { get; set; }

        /// <summary>The result of the last successful submission, for the caller and the tests.</summary>
        public CrashReportOutcome? LastOutcome { get; private set; }

        /// <summary>The submit button's state, for the tests that drive the real form.</summary>
        public string SubmitButtonText => buttonCreateBug.Text;

        public bool SubmitButtonEnabled => buttonCreateBug.Enabled;

        private async void buttonCreateBug_Click(object sender, EventArgs e)
        {
            if (_submitted)
                return;

            if (Gateway != null)
            {
                await SubmitAndReportAsync(Gateway);
                return;
            }

            string? token = GetCrashReportToken();
            if (string.IsNullOrEmpty(token))
            {
                OpenPreFilledIssueUrl();
                return;
            }

            using GitHubCrashReportGateway github = new(GeneralAppInfo.CrashReportOwner, GeneralAppInfo.CrashReportRepo, token,
                                                       $"{GeneralAppInfo.ProductName}/{GeneralAppInfo.ApplicationVersion}");
            await SubmitAndReportAsync(github);
        }

        private async Task SubmitAndReportAsync(ICrashReportGateway gateway)
        {
            CrashReportOutcome? outcome = await SubmitAsync(gateway);
            if (outcome == null)
            {
                // API call or network failed — fall back to the browser so the report is not lost.
                OpenPreFilledIssueUrl();
                return;
            }

            // Yes/No rather than a bare OK: the person who hit the crash is the one who wants to
            // know when it is fixed, and the issue page is where they can subscribe to that. Asked
            // for by the reporter of #181, who had just been sent to the issue by hand.
            DialogResult open = MessageBox.Show(this, BuildSubmittedMessage(outcome), GeneralAppInfo.ProductName,
                                                MessageBoxButtons.YesNo, MessageBoxIcon.Information);
            if (open == DialogResult.Yes)
                Process.Start(new ProcessStartInfo { FileName = outcome.Url, UseShellExecute = true });
        }

        /// <summary>The text of the prompt shown after a submission, for the tests.</summary>
        public static string BuildSubmittedMessage(CrashReportOutcome outcome)
        {
            ArgumentNullException.ThrowIfNull(outcome);

            string filed = outcome.AddedToExistingIssue
                ? FormattableString.Invariant($"This crash is already tracked as #{outcome.Issue.Number}. Your report was added there.")
                : "Error report submitted successfully.";
            return filed + "\n\n" + outcome.Url + "\n\n"
                   + "Open it in your browser? On the issue page you can subscribe to be notified when it is resolved.";
        }

        /// <summary>
        /// Files the report through <paramref name="gateway"/> and reflects the result on the
        /// button: a new issue, or "Added to #N" when an open issue already tracks this crash.
        /// Returns null when the submission failed; the button is re-armed for another attempt.
        /// No dialog is shown here, so a test can drive the real form.
        /// </summary>
        public async Task<CrashReportOutcome?> SubmitAsync(ICrashReportGateway gateway)
        {
            ArgumentNullException.ThrowIfNull(gateway);

            buttonCreateBug.Enabled = false;
            buttonCreateBug.Text = "Submitting...";

            try
            {
                CrashReportOutcome outcome = await CrashReportSubmitter.SubmitAsync(gateway, BuildIssueTitle(), BuildIssueBody());
                _submitted = true;
                LastOutcome = outcome;
                buttonCreateBug.Text = outcome.AddedToExistingIssue
                    ? FormattableString.Invariant($"Added to #{outcome.Issue.Number}")
                    : "Submitted!";
                return outcome;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                Runtime.MessageCollector?.AddExceptionMessage("Crash report submission failed", ex);
                buttonCreateBug.Text = Language.SubmitErrorReport;
                buttonCreateBug.Enabled = true;
                return null;
            }
        }

        private static string? GetCrashReportToken()
        {
            return Assembly.GetExecutingAssembly()
                .GetCustomAttributes<AssemblyMetadataAttribute>()
                .FirstOrDefault(a => string.Equals(a.Key, "CrashReportToken", StringComparison.Ordinal))?.Value;
        }

        private void OpenPreFilledIssueUrl()
        {
            string url = BuildPreFilledIssueUrl();
            Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
        }

        private string BuildIssueTitle()
        {
            string exceptionType = _exception?.GetType().FullName ?? "Unknown";
            string exceptionMessage = _exception?.Message ?? "No message";
            return $"[Crash] {exceptionType}: {Truncate(exceptionMessage, 80)}";
        }

        private string BuildIssueBody()
        {
            string exceptionType = _exception?.GetType().FullName ?? "Unknown";
            string exceptionMessage = _exception?.Message ?? "No message";
            string stackTrace = textBoxStackTrace.Text ?? "";
            string environment = textBoxEnvironment.Text ?? "";

            var body = new StringBuilder();
            body.AppendLine("## Crash Report");
            body.AppendLine();
            body.AppendLine("### Exception");
            body.AppendLine(CultureInfo.InvariantCulture, $"**Type:** `{exceptionType}`");
            body.AppendLine(CultureInfo.InvariantCulture, $"**Message:** {exceptionMessage}");
            body.AppendLine();
            body.AppendLine("### Stack Trace");
            body.AppendLine("```");
            body.AppendLine(Truncate(stackTrace, 4000));
            body.AppendLine("```");
            body.AppendLine();
            body.AppendLine("### Environment");
            body.AppendLine("```");
            body.AppendLine(environment);
            body.AppendLine("```");
            body.AppendLine();
            body.AppendLine(_isFatal ? "> **Fatal:** This exception forced the application to close." : "> **Non-fatal:** The application continued running.");
            body.AppendLine();
            body.AppendLine("---");
            body.AppendLine("*Auto-generated crash report from mRemoteNG*");
            body.AppendLine();
            body.AppendLine("*This report feeds an automated fix pipeline (see #167). If a fix ships for this crash, " +
                            "retesting on the nightly and replying here is what actually verifies it — the automated " +
                            "tests cannot reproduce your environment. Any extra detail about what you were doing " +
                            "when this happened helps debugging enormously.*");

            return body.ToString();
        }

        private string BuildPreFilledIssueUrl()
        {
            string title = BuildIssueTitle();
            string issueBody = BuildIssueBody();

            string encodedTitle = Uri.EscapeDataString(title);
            string encodedBody = Uri.EscapeDataString(issueBody);
            string encodedLabels = Uri.EscapeDataString("bug,crash-report");

            string url = $"{GeneralAppInfo.UrlBugs}?title={encodedTitle}&body={encodedBody}&labels={encodedLabels}";
            if (url.Length > 8000)
            {
                int excess = url.Length - 8000;
                if (issueBody.Length > excess + 100)
                    issueBody = issueBody[..^(excess + 100)] + "\n\n*(truncated due to URL length limit)*";
                encodedBody = Uri.EscapeDataString(issueBody);
                url = $"{GeneralAppInfo.UrlBugs}?title={encodedTitle}&body={encodedBody}&labels={encodedLabels}";
            }

            return url;
        }

        private static string Truncate(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value)) return value ?? "";
            return value.Length <= maxLength ? value : value[..maxLength] + "...";
        }
    }
}
