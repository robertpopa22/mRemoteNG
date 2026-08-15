using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using mRemoteNG.Credential;
using mRemoteNG.Security;
using NSubstitute;
using mRemoteNG.Config.Serializers.ConnectionSerializers.Csv;
using mRemoteNG.Connection;
using mRemoteNG.Container;
using mRemoteNG.Tree;
using mRemoteNG.Tree.Root;
using NUnit.Framework;

namespace mRemoteNGTests.Config.Serializers.ConnectionSerializers.Csv
{
    /// <summary>
    /// #141: the CSV export declared its inheritance headers in one order and emitted the values in
    /// another, so seven columns carried the wrong flag — an export that looked perfectly well
    /// formed and silently told the truth about the wrong properties.
    ///
    /// A header/value misalignment is invisible to any test that only checks "the file has the
    /// right number of columns" or round-trips through our own importer, because our importer reads
    /// positionally too: both sides agree on the same wrong answer. The only oracle that catches it
    /// is checking, per column, that the value under the header named InheritX really is X's flag.
    ///
    /// This walks every inheritance column in the header and proves that, by setting exactly one
    /// flag at a time and asserting it appears under its own name and nowhere else.
    /// </summary>
    [TestFixture]
    public class CsvInheritanceColumnAlignmentTests
    {
        private const char Separator = ';';

        private static (string[] Headers, string[] Values) ExportSingleConnection(
            Action<ConnectionInfoInheritance> configureInheritance)
        {
            RootNodeInfo root = new(RootNodeType.Connection);
            ConnectionInfo connection = new() { Name = "csv-alignment-probe" };
            configureInheritance(connection.Inheritance);
            root.AddChild(connection);

            ConnectionTreeModel model = new();
            model.AddRootNode(root);

            CsvConnectionsSerializerMremotengFormat serializer =
                new(new SaveFilter(), Substitute.For<ICredentialRepositoryList>());
            string csv = serializer.Serialize(model);

            string[] lines = csv.Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries);
            Assert.That(lines, Has.Length.GreaterThanOrEqualTo(2), "export produced no data row");

            return (lines[0].Split(Separator), lines[1].Split(Separator));
        }

        /// <summary>Inheritance flags that the export actually carries, taken from the header.</summary>
        private static IEnumerable<string> InheritanceHeaderNames()
        {
            (string[] headers, _) = ExportSingleConnection(_ => { });
            return headers.Where(h => h.StartsWith("Inherit", StringComparison.Ordinal)).Distinct();
        }

        [Test]
        public void TheHeaderAndTheValueRowHaveTheSameNumberOfColumns()
        {
            (string[] headers, string[] values) = ExportSingleConnection(_ => { });

            // A count mismatch is the coarse symptom; the per-column test below is the real oracle,
            // but this one localises the failure when a column is added on only one side.
            Assert.That(values, Has.Length.EqualTo(headers.Length),
                        $"header declares {headers.Length} columns, the row emits {values.Length}");
        }

        [Test]
        public void EveryInheritanceFlagIsWrittenUnderItsOwnHeader()
        {
            List<string> misaligned = [];

            foreach (string header in InheritanceHeaderNames())
            {
                string propertyName = header["Inherit".Length..];
                PropertyInfo? property = typeof(ConnectionInfoInheritance)
                    .GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);

                // Headers without a matching property are a separate problem; the alignment test
                // cannot speak to them, so they are skipped rather than silently passed.
                if (property == null || property.PropertyType != typeof(bool) || !property.CanWrite)
                    continue;

                // Exactly one flag on: if the writer emits values in a different order than the
                // header declares them, the "True" shows up under some other column's name.
                (string[] headers, string[] values) = ExportSingleConnection(inheritance =>
                {
                    property.SetValue(inheritance, true);
                });

                int index = Array.IndexOf(headers, header);
                if (index < 0 || index >= values.Length)
                {
                    misaligned.Add($"{header}: header index {index} outside the value row");
                    continue;
                }

                if (!string.Equals(values[index], "True", StringComparison.OrdinalIgnoreCase))
                {
                    string actuallyAt = string.Join(", ",
                        Enumerable.Range(0, Math.Min(headers.Length, values.Length))
                                  .Where(i => string.Equals(values[i], "True", StringComparison.OrdinalIgnoreCase))
                                  .Select(i => headers[i]));

                    misaligned.Add($"{header} (index {index}) held '{values[index]}'; "
                                   + $"the flag landed under: {(actuallyAt.Length == 0 ? "no column" : actuallyAt)}");
                }
            }

            Assert.That(misaligned, Is.Empty,
                        "inheritance values are written under the wrong headers (#141):"
                        + Environment.NewLine + string.Join(Environment.NewLine, misaligned));
        }
    }
}
