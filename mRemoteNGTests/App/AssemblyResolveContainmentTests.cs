using System;
using System.IO;
using System.Runtime.InteropServices;
using NUnit.Framework;
using mRemoteNG.App;

namespace mRemoteNGTests.App
{
    /// <summary>
    /// The assembly resolver only loads files from inside the application directory. That check
    /// must hold whether the application was started through a long path or an 8.3 short one.
    /// </summary>
    [TestFixture]
    public class AssemblyResolveContainmentTests
    {
        private string _root = null!;

        [SetUp]
        public void SetUp()
        {
            _root = Path.Combine(Path.GetTempPath(), "mRemoteNG resolve containment " + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path.Combine(_root, "Assemblies"));
            File.WriteAllText(Path.Combine(_root, "Assemblies", "Probe.dll"), "x");
        }

        [TearDown]
        public void TearDown()
        {
            try { Directory.Delete(_root, recursive: true); } catch { /* best effort */ }
        }

        [Test]
        public void AFileInsideTheDirectoryIsAccepted()
        {
            Assert.That(ProgramRoot.IsUnderDirectory(Path.Combine(_root, "Assemblies", "Probe.dll"), _root + Path.DirectorySeparatorChar), Is.True);
        }

        [Test]
        public void TheDirectoryWithoutATrailingSeparatorStillWorks()
        {
            Assert.That(ProgramRoot.IsUnderDirectory(Path.Combine(_root, "Assemblies", "Probe.dll"), _root), Is.True);
        }

        [Test]
        public void ASiblingSharingThePrefixIsRejected()
        {
            Assert.That(ProgramRoot.IsUnderDirectory(_root + "2" + Path.DirectorySeparatorChar + "Probe.dll", _root), Is.False);
        }

        [Test]
        public void ParentTraversalIsRejected()
        {
            string escaped = Path.Combine(_root, "Assemblies", "..", "..", "Evil.dll");

            Assert.That(ProgramRoot.IsUnderDirectory(escaped, _root), Is.False);
        }

        [Test]
        public void AShortNameBaseAcceptsItsOwnFiles()
        {
            // The real failure: the base directory in 8.3 form, the candidate built from it.
            // Where the volume does not generate short names, the short form equals the long one
            // and this degenerates to the plain case, which must still pass.
            string shortRoot = ToShortPath(_root);
            string candidate = Path.Combine(shortRoot, "Assemblies", "Probe.dll");

            Assert.That(ProgramRoot.IsUnderDirectory(candidate, shortRoot + Path.DirectorySeparatorChar), Is.True);
            Assert.That(ProgramRoot.IsUnderDirectory(Path.Combine(_root, "Assemblies", "Probe.dll"), shortRoot), Is.True);
        }

        [Test]
        public void AShortNameBaseStillRejectsASibling()
        {
            string shortRoot = ToShortPath(_root);
            string sibling = Path.Combine(Path.GetDirectoryName(_root)!, "elsewhere", "Probe.dll");

            Assert.That(ProgramRoot.IsUnderDirectory(sibling, shortRoot), Is.False);
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern uint GetShortPathName(string longPath, [Out] char[] shortPath, uint bufferLength);

        private static string ToShortPath(string path)
        {
            char[] buffer = new char[1024];
            uint length = GetShortPathName(path, buffer, (uint)buffer.Length);
            return length == 0 || length >= buffer.Length ? path : new string(buffer, 0, (int)length);
        }
    }
}
