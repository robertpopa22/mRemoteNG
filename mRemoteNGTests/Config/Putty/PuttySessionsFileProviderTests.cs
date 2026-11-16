using System;
using System.IO;
using System.Linq;
using mRemoteNG.Config.Putty;
using mRemoteNG.Connection.Protocol;
using NUnit.Framework;

namespace mRemoteNGTests.Config.Putty
{
    [TestFixture]
    public class PuttySessionsFileProviderTests
    {
        private string _tempDir;
        private string _kittyPath;
        private string _sessionsDir;
        private string _originalPuttyPath;

        [SetUp]
        public void SetUp()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "mRemoteNG_KiTTY_Tests_" + Guid.NewGuid());
            Directory.CreateDirectory(_tempDir);
            _kittyPath = Path.Combine(_tempDir, "KiTTY.exe");
            // Create dummy exe
            File.WriteAllText(_kittyPath, "dummy");
            
            _sessionsDir = Path.Combine(_tempDir, "Sessions");
            Directory.CreateDirectory(_sessionsDir);

            _originalPuttyPath = PuttyBase.PuttyPath;
            PuttyBase.PuttyPath = _kittyPath;
        }

        [TearDown]
        public void TearDown()
        {
            PuttyBase.PuttyPath = _originalPuttyPath;
            if (Directory.Exists(_tempDir))
            {
                try { Directory.Delete(_tempDir, true); } catch { }
            }
        }

        [Test]
        public void GetSessionNames_ReturnsSessionNames()
        {
            File.WriteAllText(Path.Combine(_sessionsDir, "Session%201"), "Protocol=ssh");
            File.WriteAllText(Path.Combine(_sessionsDir, "Session2"), "Protocol=raw");

            var provider = new PuttySessionsFileProvider();
            var names = provider.GetSessionNames(false);

            Assert.That(names, Contains.Item("Session 1"));
            Assert.That(names, Contains.Item("Session2"));
        }

        [TestCase("Test%20Session", "Test Session")]
        [TestCase("%2E%2E%2FTest%5CSession%3A01.cfg", @"../Test\Session:01.cfg")]
        public void GetSession_ReturnsCorrectSessionInfo(string sessionName, string decodedName)
        {
            var filePath = Path.Combine(_sessionsDir, sessionName);
            File.WriteAllText(filePath, "Protocol=ssh" + Environment.NewLine + "HostName=example.com" + Environment.NewLine + "PortNumber=2222" + Environment.NewLine + "UserName=user");

            var provider = new PuttySessionsFileProvider();
            
            var session = provider.GetSession(sessionName);

            Assert.That(session, Is.Not.Null);
            Assert.That(session.Name, Is.EqualTo(decodedName));
            Assert.That(session.PuttySession, Is.EqualTo(decodedName));
            Assert.That(session.Hostname, Is.EqualTo("example.com"));
            Assert.That(session.Port, Is.EqualTo(2222));
            Assert.That(session.Username, Is.EqualTo("user"));
            Assert.That(session.Protocol, Is.EqualTo(ProtocolType.SSH2));
        }

        [Test]
        public void GetSession_RejectsPathsInsteadOfRawFileNames()
        {
            var outsideFile = Path.Combine(_tempDir, "OutsideSession");
            File.WriteAllText(outsideFile, "Protocol=ssh");
            var nestedDirectory = Path.Combine(_sessionsDir, "Nested");
            Directory.CreateDirectory(nestedDirectory);
            File.WriteAllText(Path.Combine(nestedDirectory, "Session"), "Protocol=ssh");

            var provider = new PuttySessionsFileProvider();

            Assert.Multiple(() =>
            {
                Assert.That(provider.GetSession(outsideFile), Is.Null);
                Assert.That(provider.GetSession(@"..\OutsideSession"), Is.Null);
                Assert.That(provider.GetSession("../OutsideSession"), Is.Null);
                Assert.That(provider.GetSession(@"Nested\Session"), Is.Null);
                Assert.That(provider.GetSession("Nested/Session"), Is.Null);
            });
        }
    }
}
