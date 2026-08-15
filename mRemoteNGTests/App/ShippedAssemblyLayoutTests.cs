using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using NUnit.Framework;

namespace mRemoteNGTests.App
{
    /// <summary>
    /// Asserts that every assembly the application declares it needs at runtime is actually present
    /// in the layout that ships.
    ///
    /// This matters here more than in a normal project. mRemoteNG does not keep its dependencies
    /// beside the executable: the build moves most of them into an <c>Assemblies\</c> subdirectory
    /// and a custom <c>AssemblyResolve</c> handler finds them there. That is a arrangement where a
    /// newly added package can resolve perfectly during development — the SDK output has it — and
    /// then throw <c>FileNotFoundException</c> on a user's machine because the copy step never
    /// learned about it. #150 was exactly that shape of failure.
    ///
    /// The oracle is deps.json, which is the build's own statement of what it expects to load, so
    /// this cannot drift out of date the way a hand-written expected-file list would.
    /// </summary>
    [TestFixture]
    public class ShippedAssemblyLayoutTests
    {
        private static string? FindApplicationOutput()
        {
            // Walk up from the test binary to the repository root, then into the app's output.
            DirectoryInfo? dir = new(TestContext.CurrentContext.TestDirectory);
            while (dir != null && !Directory.Exists(Path.Combine(dir.FullName, "mRemoteNG")))
                dir = dir.Parent;

            if (dir == null)
                return null;

            string[] candidates =
            [
                Path.Combine(dir.FullName, "mRemoteNG", "bin", "x64", "Release"),
                Path.Combine(dir.FullName, "mRemoteNG", "bin", "x86", "Release"),
                Path.Combine(dir.FullName, "mRemoteNG", "bin", "Release"),
            ];

            return candidates.FirstOrDefault(
                c => File.Exists(Path.Combine(c, "mRemoteNG.deps.json")));
        }

        private static IEnumerable<string> DeclaredRuntimeAssemblies(string depsJsonPath)
        {
            using FileStream stream = File.OpenRead(depsJsonPath);
            using JsonDocument document = JsonDocument.Parse(stream);

            if (!document.RootElement.TryGetProperty("targets", out JsonElement targets))
                yield break;

            foreach (JsonProperty target in targets.EnumerateObject())
            {
                foreach (JsonProperty library in target.Value.EnumerateObject())
                {
                    if (!library.Value.TryGetProperty("runtime", out JsonElement runtime))
                        continue;

                    foreach (JsonProperty asset in runtime.EnumerateObject())
                    {
                        string file = asset.Name.Split('/').Last();
                        if (file.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                            yield return file;
                    }
                }
            }
        }

        [Test]
        public void EveryDeclaredRuntimeAssemblyIsPresentInTheShippedLayout()
        {
            string? output = FindApplicationOutput();
            if (output == null)
                Assert.Ignore("Application output not found — run build.ps1 first.");

            string depsJson = Path.Combine(output!, "mRemoteNG.deps.json");
            HashSet<string> declared = new(DeclaredRuntimeAssemblies(depsJson),
                                           StringComparer.OrdinalIgnoreCase);

            Assert.That(declared, Is.Not.Empty, "deps.json declared no runtime assemblies");

            // Both places the resolver looks: beside the executable, and in Assemblies\.
            HashSet<string> present = new(StringComparer.OrdinalIgnoreCase);
            foreach (string dir in new[] { output!, Path.Combine(output!, "Assemblies") })
            {
                if (Directory.Exists(dir))
                    present.UnionWith(Directory.GetFiles(dir, "*.dll").Select(Path.GetFileName)!);
            }

            List<string> missing = declared.Except(present, StringComparer.OrdinalIgnoreCase)
                                           .OrderBy(x => x, StringComparer.Ordinal)
                                           .ToList();

            Assert.That(missing, Is.Empty,
                        "assemblies the build says it needs are absent from the shipped layout — "
                        + "these resolve during development and throw FileNotFoundException on a "
                        + "user's machine (#150): " + string.Join(", ", missing));
        }

        [Test]
        public void TheAssembliesSubdirectoryIsPopulated()
        {
            // If the copy step silently stops running, the previous test would still pass whenever
            // everything happens to sit beside the executable. This pins the arrangement the
            // resolver is written for.
            string? output = FindApplicationOutput();
            if (output == null)
                Assert.Ignore("Application output not found — run build.ps1 first.");

            string assemblies = Path.Combine(output!, "Assemblies");
            Assert.That(Directory.Exists(assemblies), Is.True,
                        "the Assemblies subdirectory is missing — the custom AssemblyResolve "
                        + "handler has nowhere to look");
            Assert.That(Directory.GetFiles(assemblies, "*.dll"), Is.Not.Empty);
        }
    }
}
