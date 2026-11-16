using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Loader;
using System.Threading;
using NUnit.Framework;
using mRemoteNG.App;
using mRemoteNG.Messages;

namespace mRemoteNGTests.App
{
    [TestFixture]
    [NonParallelizable]
    public class StartupLoadTraceTests
    {
        [TestCase(FileAttributes.Normal, false)]
        [TestCase(FileAttributes.Archive | FileAttributes.ReadOnly, false)]
        [TestCase(FileAttributes.Archive | FileAttributes.ReparsePoint, false)]
        [TestCase(FileAttributes.Offline, true)]
        [TestCase((FileAttributes)0x00040000, true)] // RECALL_ON_OPEN
        [TestCase((FileAttributes)0x00400000 | FileAttributes.Archive, true)] // RECALL_ON_DATA_ACCESS: OneDrive online-only
        [TestCase((FileAttributes)0x00080000 | FileAttributes.Archive, false)] // PINNED: always kept on this device
        public void IsNotLocal_ReadsTheCloudPlaceholderBits(FileAttributes attributes, bool expected)
        {
            Assert.That(StartupLoadTrace.IsNotLocal(attributes), Is.EqualTo(expected));
        }

        [Test]
        public void ElapsedMs_ConvertsStopwatchTicks()
        {
            Assert.That(StartupLoadTrace.ElapsedMs(0, Stopwatch.Frequency * 3), Is.EqualTo(3000));
        }

        [Test]
        public void StartupLogsTheFileScanAndAnAssemblyLoadAfterASlowGap() =>
            RunWithFreshTrace(nameof(CheckStartup));

        [Test]
        public void StartupContinuesWhenTheProgramFolderCannotBeScanned() =>
            RunWithFreshTrace(nameof(CheckScanFailure));

        private static void CheckStartup()
        {
            DirectoryInfo programDirectory = Directory.CreateTempSubdirectory();
            var messages = new MessageCollector();
            try
            {
                DirectoryInfo assemblies = programDirectory.CreateSubdirectory("Assemblies");
                File.WriteAllText(Path.Combine(programDirectory.FullName, "local.dll"), string.Empty);
                File.WriteAllText(Path.Combine(programDirectory.FullName, "ignore.txt"), string.Empty);
                string offline = Path.Combine(assemblies.FullName, "offline.EXE");
                File.WriteAllText(offline, string.Empty);
                File.SetAttributes(offline, FileAttributes.Offline);

                StartupLoadTrace.LogProgramFiles(messages);
                StartupLoadTrace.Finish(messages);
                Assert.That(messages.Messages, Is.Empty);

                var probe = new AssemblyName("DelayedStartupProbe");
                _ = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("WarmupStartupProbe"), AssemblyBuilderAccess.RunAndCollect);
                TimeSpan delay = TimeSpan.FromMilliseconds(StartupLoadTrace.SlowGapMs + 50);
                StartIn(programDirectory.FullName);
                StartIn(Path.Combine(programDirectory.FullName, "missing")); // A second Start must keep the first scan.
                Thread.Sleep(delay);
                _ = AssemblyBuilder.DefineDynamicAssembly(probe, AssemblyBuilderAccess.RunAndCollect);
                StartupLoadTrace.LogProgramFiles(messages);
                StartupLoadTrace.Finish(messages);

                IMessage[] logged = messages.Messages.ToArray();
                Assert.That(logged[0].Text, Does.Match(@"^\[Startup\]   ProgramFiles: 2 dll/exe scanned in \d+ms, 1 not on the local disk \(cloud placeholders\): offline\.EXE$"));
                Assert.That(logged.Select(message => message.Text), Has.Some.StartsWith("[Startup]   AssemblyLoad: DelayedStartupProbe at +"));
                Assert.That(logged[^1].Text, Does.Match(@"^\[Startup\]   AssemblyLoads: [1-9]\d* during startup$"));
                Assert.That(logged, Has.All.Matches<IMessage>(message => message.OnlyLog && message.Class == MessageClass.InformationMsg));

                StartupLoadTrace.Finish(messages);
                Assert.That(messages.Messages, Is.EqualTo(logged));
            }
            finally
            {
                StartupLoadTrace.Finish(messages);
                programDirectory.Delete(recursive: true);
            }
        }

        private static void CheckScanFailure()
        {
            DirectoryInfo programDirectory = Directory.CreateTempSubdirectory();
            var messages = new MessageCollector();
            try
            {
                StartIn(Path.Combine(programDirectory.FullName, "missing"));
                StartupLoadTrace.LogProgramFiles(messages);
                StartupLoadTrace.Finish(messages);

                string[] logged = messages.Messages.Select(message => message.Text).ToArray();
                Assert.That(logged, Has.Length.EqualTo(2));
                Assert.That(logged[0], Does.EndWith(", scan failed: DirectoryNotFoundException"));
                Assert.That(logged[1], Does.StartWith("[Startup]   AssemblyLoads: "));
            }
            finally
            {
                StartupLoadTrace.Finish(messages);
                programDirectory.Delete();
            }
        }

        private static void StartIn(string baseDirectory)
        {
            const string baseDirectoryKey = "APP_CONTEXT_BASE_DIRECTORY";
            object? original = AppContext.GetData(baseDirectoryKey);
            try
            {
                AppContext.SetData(baseDirectoryKey, baseDirectory);
                StartupLoadTrace.Start();
            }
            finally
            {
                AppContext.SetData(baseDirectoryKey, original);
            }
        }

        private static void RunWithFreshTrace(string testMethod)
        {
            // The production trace is one-shot and belongs to the app's assembly. Give each
            // scenario fresh statics without resetting the app's copy or sharing state with other tests.
            var context = new StartupTraceLoadContext();
            try
            {
                Type testType = typeof(StartupLoadTraceTests);
                Type isolatedTestType = context.LoadFromAssemblyPath(testType.Assembly.Location).GetType(testType.FullName!)!;
                isolatedTestType.GetMethod(testMethod, BindingFlags.Static | BindingFlags.NonPublic)!.CreateDelegate<Action>()();
            }
            finally
            {
                context.Unload();
            }
        }

        private sealed class StartupTraceLoadContext() : AssemblyLoadContext(isCollectible: true)
        {
            protected override Assembly? Load(AssemblyName assemblyName)
            {
                Assembly app = typeof(StartupLoadTrace).Assembly;
                return assemblyName.Name == app.GetName().Name ? LoadFromAssemblyPath(app.Location) : null;
            }
        }
    }
}
