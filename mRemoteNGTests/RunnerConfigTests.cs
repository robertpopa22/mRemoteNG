using System.IO;
using System.Linq;
using System.Text.Json;
using NUnit.Framework;

namespace mRemoteNGTests;

/// <summary>
/// test-config.json drives run-tests-core.sh. Its groups are meant to partition the test DLL. Until
/// 2026-10-21 two of them negated with "\!~": vstest's discovery then matched nothing, the NUnit
/// adapter ran the whole DLL instead, and "Remaining" and "Config.Other" re-ran almost every test,
/// so a full run executed about 8000 tests where 4037 exist.
/// </summary>
public class RunnerConfigTests
{
    private static JsonDocument LoadConfig()
    {
        DirectoryInfo? dir = new(TestContext.CurrentContext.TestDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "test-config.json")))
            dir = dir.Parent;

        Assert.That(dir, Is.Not.Null, "test-config.json not found above " + TestContext.CurrentContext.TestDirectory);
        return JsonDocument.Parse(File.ReadAllText(Path.Combine(dir!.FullName, "test-config.json")));
    }

    [Test]
    public void NoFilterEscapesItsNegation()
    {
        using JsonDocument config = LoadConfig();
        JsonElement runner = config.RootElement.GetProperty("runner");
        string[] filters = runner.GetProperty("groups").EnumerateArray()
                                 .Concat(runner.GetProperty("isolated").EnumerateArray())
                                 .Select(g => g.GetProperty("filter").GetString() ?? "")
                                 .ToArray();

        Assert.That(filters, Has.None.Contains("\\"), "negate with !~ or !=; a backslash makes the group run the whole DLL");
    }

    [Test]
    public void TheGroupsAndIsolatedChecksAddUpToTheExpectedTotal()
    {
        using JsonDocument config = LoadConfig();
        JsonElement runner = config.RootElement.GetProperty("runner");
        int groups = runner.GetProperty("groups").EnumerateArray().Sum(g => g.GetProperty("expected").GetInt32());
        int isolated = runner.GetProperty("isolated").GetArrayLength();
        JsonElement metadata = config.RootElement.GetProperty("metadata");

        Assert.Multiple(() =>
        {
            Assert.That(groups + isolated, Is.EqualTo(metadata.GetProperty("total_expected").GetInt32()),
                        "each test belongs to exactly one group, so the totals add up");
            Assert.That(metadata.GetProperty("min_total_threshold").GetInt32(),
                        Is.LessThan(metadata.GetProperty("total_expected").GetInt32()));
        });
    }
}
