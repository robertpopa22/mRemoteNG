using System;
using System.Data;
using System.Linq;
using mRemoteNG.Config;
using mRemoteNG.Config.Serializers.ConnectionSerializers.Sql;
using mRemoteNG.Connection;
using mRemoteNG.Security;
using mRemoteNG.Security.SymmetricEncryption;
using mRemoteNG.Tools;
using mRemoteNG.Tree;
using mRemoteNG.Tree.Root;
using NUnit.Framework;

namespace mRemoteNGTests.Config.Serializers
{
    /// <summary>
    /// Change detection decides whether a connection row is rewritten on save. It had been
    /// reporting every row as changed, so every save rewrote the whole table: on MSSQL that bumps
    /// every RowVersion, and the provider compares RowVersion for concurrency, so one user saving
    /// could hand DBConcurrencyException to everyone else on the same profile. It also made saves
    /// cost far more than the edit warranted, which is the ground #120 covered.
    ///
    /// Two independent defects caused it, and both are pinned here:
    ///   * three enum columns were written with ToString() but compared against the enum value, so
    ///     "None".Equals(ExternalAddressProvider.None) was false forever — three permanently false
    ///     links in an AND chain;
    ///   * passwords were compared by re-encrypting and matching ciphertext, which cannot match
    ///     when the provider uses a fresh nonce per call.
    ///
    /// The guards matter as much as the main assertion: change detection that never fires is a
    /// performance bug, but change detection that misses a real edit loses the user's data.
    /// </summary>
    [TestFixture]
    public class DataTableChangeDetectionTests
    {
        private const string MasterPassword = "change-detection-tests";

        private static ConnectionInfo BuildConnection() => new()
        {
            Name = "unchanged probe",
            Hostname = "host.example.invalid",
            Port = 3389,
            Panel = "General",
            Username = "user",
            // A stored password is the normal case, and the one the ciphertext comparison broke.
            Password = "p@ssw0rd",
            RDGatewayPassword = "gwp@ss",
        };

        private static ConnectionTreeModel BuildModel(ConnectionInfo connection)
        {
            RootNodeInfo root = new(RootNodeType.Connection) { PasswordString = MasterPassword };
            root.AddChild(connection);
            ConnectionTreeModel model = new();
            model.AddRootNode(root);
            return model;
        }

        private static DataTableSerializer NewSerializer() =>
            new(new SaveFilter(),
                new AeadCryptographyProvider { KeyDerivationIterations = 1000 },
                MasterPassword.ConvertToSecureString());

        /// <summary>Writes the tree once, then writes the same tree again over that result.</summary>
        private static DataTable SaveTwice(ConnectionTreeModel model)
        {
            DataTable first = NewSerializer().Serialize(model);
            first.AcceptChanges();

            DataTableSerializer second = NewSerializer();
            second.SetSourceDataTable(first);
            return second.Serialize(model);
        }

        private static DataRow[] ModifiedRows(DataTable table) =>
            table.Rows.Cast<DataRow>().Where(r => r.RowState != DataRowState.Unchanged).ToArray();

        [Test]
        public void SavingAnUnmodifiedTreeRewritesNothing()
        {
            DataTable result = SaveTwice(BuildModel(BuildConnection()));

            Assert.That(ModifiedRows(result), Is.Empty,
                        "an unmodified connection was rewritten — every save would rewrite the "
                        + "whole table, bumping RowVersion for every other user on the profile");
        }

        [Test]
        public void SavingAnUnmodifiedTreeRewritesNothingWithoutStoredPasswords()
        {
            ConnectionInfo connection = BuildConnection();
            connection.Password = "";
            connection.RDGatewayPassword = "";

            Assert.That(ModifiedRows(SaveTwice(BuildModel(connection))), Is.Empty);
        }

        [Test]
        public void AnEditedFieldIsStillDetected()
        {
            ConnectionInfo connection = BuildConnection();
            ConnectionTreeModel model = BuildModel(connection);

            DataTable first = NewSerializer().Serialize(model);
            first.AcceptChanges();

            connection.Hostname = "changed.example.invalid";

            DataTableSerializer second = NewSerializer();
            second.SetSourceDataTable(first);
            DataTable result = second.Serialize(model);

            Assert.That(ModifiedRows(result), Is.Not.Empty,
                        "a changed hostname was not detected, so the edit would never be saved");
        }

        /// <summary>
        /// The dangerous direction. Comparing decrypted plaintext instead of ciphertext is only
        /// correct if a genuinely new password still registers as a change.
        /// </summary>
        [Test]
        public void AnEditedPasswordIsStillDetected()
        {
            ConnectionInfo connection = BuildConnection();
            ConnectionTreeModel model = BuildModel(connection);

            DataTable first = NewSerializer().Serialize(model);
            first.AcceptChanges();

            connection.Password = "a completely different password";

            DataTableSerializer second = NewSerializer();
            second.SetSourceDataTable(first);
            DataTable result = second.Serialize(model);

            Assert.That(ModifiedRows(result), Is.Not.Empty,
                        "a changed password was not detected — the user's new password would be "
                        + "silently discarded on save");
        }

        [Test]
        public void ClearingAPasswordIsStillDetected()
        {
            ConnectionInfo connection = BuildConnection();
            ConnectionTreeModel model = BuildModel(connection);

            DataTable first = NewSerializer().Serialize(model);
            first.AcceptChanges();

            connection.Password = "";

            DataTableSerializer second = NewSerializer();
            second.SetSourceDataTable(first);
            DataTable result = second.Serialize(model);

            Assert.That(ModifiedRows(result), Is.Not.Empty,
                        "clearing a password was not detected, so it would stay in the database");
        }
    }
}
