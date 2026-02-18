using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using mRemoteNG.Container;
using mRemoteNG.Connection;
using System.Threading;

namespace mRemoteNGTests.Container
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public class DynamicFolderManagerTests
    {
        private string _tempFile;
        private DynamicFolderManager _manager;

        [SetUp]
        public void Setup()
        {
            _manager = new DynamicFolderManager();
            _tempFile = Path.GetTempFileName();
        }

        [TearDown]
        public void TearDown()
        {
            if (File.Exists(_tempFile))
            {
                File.Delete(_tempFile);
            }
        }

        [Test]
        public void RefreshFolder_FileSource_PopulatesChildren()
        {
            // Arrange
            string xml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<mRemoteNG ConfVersion=""1.0"">
    <Node Name=""TestConnection"" Type=""Connection"" Descr=""Test Description"" Protocol=""RDP"" Hostname=""localhost"" Port=""3389"" />
</mRemoteNG>";
            File.WriteAllText(_tempFile, xml);

            var container = new ContainerInfo
            {
                Name = "DynamicFolder",
                DynamicSource = DynamicSourceType.File,
                DynamicSourceValue = _tempFile
            };

            // Act
            _manager.RefreshFolder(container);

            // Assert
            Assert.That(container.Children.Count, Is.EqualTo(1));
            Assert.That(container.Children[0].Name, Is.EqualTo("TestConnection"));
        }

        [Test]
        public void RefreshFolder_ScriptSource_PopulatesChildren()
        {
            // Arrange
            string scriptPath = Path.ChangeExtension(_tempFile, ".ps1");
            string xml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<mRemoteNG ConfVersion=""1.0"">
    <Node Name=""ScriptConnection"" Type=""Connection"" Descr=""From Script"" Protocol=""SSH"" Hostname=""127.0.0.1"" Port=""22"" />
</mRemoteNG>";
            // Escaping quotes for PowerShell string
            string psXml = xml.Replace("\"", "`\""); 
            File.WriteAllText(scriptPath, $"Write-Output \"{psXml}\"");

            var container = new ContainerInfo
            {
                Name = "DynamicScriptFolder",
                DynamicSource = DynamicSourceType.Script,
                DynamicSourceValue = scriptPath
            };

            try
            {
                // Act
                _manager.RefreshFolder(container);

                // Assert
                Assert.That(container.Children.Count, Is.EqualTo(1));
                Assert.That(container.Children[0].Name, Is.EqualTo("ScriptConnection"));
            }
            finally
            {
                if (File.Exists(scriptPath)) File.Delete(scriptPath);
            }
        }
    }
}