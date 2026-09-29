using mRemoteNG.Tools;
using NUnit.Framework;

namespace mRemoteNGTests.Tools
{
    [TestFixture]
    public class ScpPathQuotingTests
    {
        [TestCase("/tmp/plain.txt", "'/tmp/plain.txt'")]
        [TestCase("/tmp/two words.txt", "'/tmp/two words.txt'")]
        [TestCase("/tmp/$(id).txt", "'/tmp/$(id).txt'")]
        [TestCase("/tmp/`id`.txt", "'/tmp/`id`.txt'")]
        [TestCase("/tmp/\"quoted\".txt", "'/tmp/\"quoted\".txt'")]
        [TestCase("/tmp/it's.txt", "'/tmp/it'\"'\"'s.txt'")]
        public void ProductClientTreatsShellSyntaxAsLiteralPath(string path, string expected)
        {
            using var transfer = new SecureTransfer("localhost", "test", "", 22, SecureTransfer.SSHTransferProtocol.SCP);
            using var client = transfer.CreateScpClient();
            Assert.That(client.RemotePathTransformation.Transform(path), Is.EqualTo(expected));
        }
    }
}
