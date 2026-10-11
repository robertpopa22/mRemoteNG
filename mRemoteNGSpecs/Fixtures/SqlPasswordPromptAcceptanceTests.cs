using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using FlaUI.Core.Capturing;
using FlaUI.Core.AutomationElements;
using FlaUI.UIA3;
using mRemoteNG.Config.Connections;
using mRemoteNG.Config.DatabaseConnectors;
using mRemoteNG.Config.DataProviders;
using mRemoteNG.Config.Serializers;
using mRemoteNG.Config.Serializers.ConnectionSerializers.Sql;
using mRemoteNG.Config.Serializers.Versioning;
using mRemoteNG.Security.SymmetricEncryption;
using mRemoteNG.Security;
using mRemoteNG.Tools;
using mRemoteNG.Tree;
using mRemoteNG.Tree.Root;
using mRemoteNGSpecs.Support;
using NUnit.Framework;

namespace mRemoteNGSpecs.Fixtures;

/// <summary>
/// Real SQL loader, legacy crypto and password dialog; metadata/table providers are
/// in-memory so the negative canary cannot read or mutate a real database.
/// Run on the lab's interactive desktop, never on the operator's desktop.
/// </summary>
[TestFixture]
[NonParallelizable]
[SupportedOSPlatform("windows")]
public class SqlPasswordPromptAcceptanceTests
{
    [TestCase(false)]
    [TestCase(true)]
    public void ALegacyPaddingCollisionStillShowsThePasswordDialog(bool accept)
    {
        using var automation = new UIA3Automation();
        using var connector = new OdbcDatabaseConnector("DSN=synthetic-not-connected", "", "", "");
        var data = new SyntheticDatabase();
        var loader = new SqlConnectionsLoader(data, data, connector, data, data, data,
            new LegacyRijndaelCryptographyProvider());
        var result = new TaskCompletionSource<ConnectionTreeModel>();
        var worker = new Thread(() =>
        {
            try { result.SetResult(loader.Load()); }
            catch (Exception ex) { result.SetException(ex); }
        });
        worker.IsBackground = true;
        worker.SetApartmentState(ApartmentState.STA);
        worker.Start();
        FlaUI.Core.AutomationElements.AutomationElement? dialog = null;
        try
        {
            UiWait.Until(() =>
            {
                dialog = automation.GetDesktop().FindAllChildren(cf => cf.ByProcessId(Environment.ProcessId))
                    .FirstOrDefault(window => window.FindFirstDescendant(cf => cf.ByAutomationId("txtPassword")) is not null);
                return dialog is not null || result.Task.IsCompleted;
            }, "SQL password prompt or load result", TimeSpan.FromSeconds(15));
            Assert.That(dialog, Is.Not.Null, "CBC padding alone must not skip the real password prompt");
            AutomationElement prompt = dialog!;
            string evidence = Path.Combine(AppContext.BaseDirectory, "_uiscenarios", "_evidence",
                "SqlPasswordPrompt-" + (accept ? "accept" : "cancel"));
            Directory.CreateDirectory(evidence);
            Capture.Element(prompt).ToFile(Path.Combine(evidence, "password-prompt.png"));
            if (accept)
            {
                UiWait.FindRequired(prompt, cf => cf.ByAutomationId("txtPassword"), "master password field").AsTextBox().Text = "sqlpass";
                UiWait.FindRequired(prompt, cf => cf.ByAutomationId("btnOK"), "OK button").AsButton().Invoke();
            }
            else
                UiWait.FindRequired(prompt, cf => cf.ByAutomationId("btnCancel"), "Cancel button").AsButton().Invoke();

            Assert.That(worker.Join(TimeSpan.FromSeconds(15)), Is.True, "SQL load must finish after the dialog answer");
            if (accept)
            {
                Assert.That(result.Task.GetAwaiter().GetResult().RootNodes[0].Children[0].Password,
                    Is.EqualTo("synthetic-secret"));
                Assert.That(data.TableReads, Is.EqualTo(1));
            }
            else
            {
                Assert.That(() => result.Task.GetAwaiter().GetResult(),
                    Throws.TypeOf<InvalidOperationException>().With.Message.EqualTo("Could not load SQL connections"));
                Assert.That(data.TableReads, Is.Zero);
            }
            File.WriteAllText(Path.Combine(evidence, "observation.txt"),
                $"Prompt visible; answer={(accept ? "correct password" : "cancel")}; table reads={data.TableReads}");
        }
        finally
        {
            if (worker.IsAlive && dialog is not null)
            {
                dialog.FindFirstDescendant(cf => cf.ByAutomationId("btnCancel"))?.AsButton().Invoke();
                worker.Join(TimeSpan.FromSeconds(5));
            }
        }
    }

    private sealed class SyntheticDatabase : IDataProvider<string>, IDataProvider<DataTable>,
        IDeserializer<string, IEnumerable<LocalConnectionPropertiesModel>>, ISqlDatabaseMetaDataRetriever,
        ISqlDatabaseVersionVerifier
    {
        public int TableReads { get; private set; }
        public SqlConnectionListMetaData GetDatabaseMetaData(IDatabaseConnector connector) => new()
        {
            Protected = "prFr5uheaqzrPbIp+bOjhAiTvlNPrrOGWY/9NVDPx+Q=", ConfVersion = new Version(3, 0)
        };
        public void WriteDatabaseMetaData(RootNodeInfo root, IDatabaseConnector connector) => throw new NotSupportedException();
        public void WriteDatabaseMetaData(RootNodeInfo root, IDatabaseConnector connector, DbTransaction? transaction) => throw new NotSupportedException();
        public bool VerifyDatabaseVersion(Version version) => true;
        string IDataProvider<string>.Load() => "";
        public void Save(string contents) => throw new NotSupportedException();
        public IEnumerable<LocalConnectionPropertiesModel> Deserialize(string contents) => [];
        public void Save(DataTable contents) => throw new NotSupportedException();
        DataTable IDataProvider<DataTable>.Load()
        {
            TableReads++;
            using SecureString password = "sqlpass".ConvertToSecureString();
            var root = new RootNodeInfo(RootNodeType.Connection) { PasswordString = "sqlpass" };
            root.AddChild(new mRemoteNG.Connection.ConnectionInfo { Name = "Synthetic", Password = "synthetic-secret" });
            var tree = new ConnectionTreeModel();
            tree.AddRootNode(root);
            return new DataTableSerializer(new SaveFilter(), new LegacyRijndaelCryptographyProvider(), password).Serialize(tree);
        }
    }
}
