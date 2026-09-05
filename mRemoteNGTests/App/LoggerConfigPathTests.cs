using System;
using System.IO;
using mRemoteNG.App;
using NUnit.Framework;

namespace mRemoteNGTests.App;

public class LoggerConfigPathTests
{
    [Test]
    public void TheLog4NetConfigIsLookedForBesideTheExecutableNotInTheWorkingDirectory()
    {
        // The daily-driver install produced no log at all for a run started with a foreign working
        // directory: log4net.config was looked up relative to that directory, log4net does not
        // throw when its config is missing, and a repository with no appenders discards every
        // line. A shortcut with an empty "Start in" or a scheduled task does the same.
        string path = Logger.ConfigFilePath;

        Assert.Multiple(() =>
        {
            Assert.That(Path.IsPathRooted(path), Is.True, "the config path must not depend on the working directory");
            Assert.That(Path.GetDirectoryName(path), Is.EqualTo(Path.TrimEndingDirectorySeparator(AppContext.BaseDirectory)));
            Assert.That(Path.GetFileName(path), Is.EqualTo("log4net.config"));
        });
    }

    [Test]
    public void TheConfigPathDoesNotFollowACurrentDirectoryChange()
    {
        string original = Environment.CurrentDirectory;
        string before = Logger.ConfigFilePath;
        try
        {
            Environment.CurrentDirectory = Path.GetTempPath();
            Assert.That(Logger.ConfigFilePath, Is.EqualTo(before));
        }
        finally
        {
            Environment.CurrentDirectory = original;
        }
    }
}
