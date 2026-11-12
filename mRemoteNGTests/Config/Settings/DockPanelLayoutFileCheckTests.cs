using System.IO;
using mRemoteNG.Config.Settings;
using NUnit.Framework;

namespace mRemoteNGTests.Config.Settings
{
    /// <summary>
    /// View menu, load layout from a file: a file that is not a dock layout is recognised before
    /// any panel is closed (#230).
    /// </summary>
    [TestFixture]
    public class DockPanelLayoutFileCheckTests
    {
        private string _file = null!;

        [SetUp]
        public void Setup() => _file = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".xml");

        [TearDown]
        public void Teardown()
        {
            if (File.Exists(_file)) File.Delete(_file);
        }

        [Test]
        public void ADockLayoutIsAccepted()
        {
            File.WriteAllText(_file, "<?xml version=\"1.0\" encoding=\"utf-16\"?>\n<!--DockPanel configuration file.-->\n<DockPanel FormatVersion=\"1.0\" DockLeftPortion=\"0.25\"><Contents Count=\"0\" /></DockPanel>", System.Text.Encoding.Unicode);

            Assert.That(DockPanelLayoutLoader.IsDockLayoutFile(_file, out string reason), Is.True, reason);
        }

        [Test]
        public void AConnectionsFileIsRefusedAndTheReasonNamesItsRoot()
        {
            File.WriteAllText(_file, "<?xml version=\"1.0\" encoding=\"utf-8\"?><mrng:Connections xmlns:mrng=\"http://mremoteng.org\" Name=\"Connections\" />");

            Assert.That(DockPanelLayoutLoader.IsDockLayoutFile(_file, out string reason), Is.False);
            Assert.That(reason, Does.Contain("mrng:Connections"));
        }

        [Test]
        public void AFileThatIsNotXmlIsRefused()
        {
            File.WriteAllText(_file, "this is not xml");

            Assert.That(DockPanelLayoutLoader.IsDockLayoutFile(_file, out string reason), Is.False);
            Assert.That(reason, Is.Not.Empty);
        }

        [Test]
        public void AMissingFileIsRefused()
        {
            Assert.That(DockPanelLayoutLoader.IsDockLayoutFile(_file, out string reason), Is.False);
            Assert.That(reason, Is.Not.Empty);
        }

        [Test]
        public void AFileWithADocumentTypeIsRefused()
        {
            File.WriteAllText(_file, "<!DOCTYPE DockPanel [<!ENTITY x \"y\">]><DockPanel FormatVersion=\"1.0\" />");

            Assert.That(DockPanelLayoutLoader.IsDockLayoutFile(_file, out string _), Is.False);
        }
    }
}
