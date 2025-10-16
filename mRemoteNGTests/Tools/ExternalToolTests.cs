using System.Diagnostics;
using System.Reflection;
using mRemoteNG.Connection;
using mRemoteNG.Tools;
using NUnit.Framework;

namespace mRemoteNGTests.Tools
{
    [TestFixture]
    public class ExternalToolTests
    {
        [Test]
        public void PasswordWithEqualsSignIsPassedCorrectly()
        {
            // Arrange
            var connectionInfo = new ConnectionInfo
            {
                Password = "Z-3=Wv99/Aq",
                Hostname = "testhost",
                Username = "testuser"
            };

            var externalTool = new ExternalTool
            {
                DisplayName = "Test Tool",
                FileName = "test.exe",
                Arguments = "-u %USERNAME% -p %PASSWORD% -h %HOSTNAME%"
            };

            // Act
            var process = new Process();
            var setProcessPropertiesMethod = typeof(ExternalTool).GetMethod(
                "SetProcessProperties",
                BindingFlags.NonPublic | BindingFlags.Instance
            );
            setProcessPropertiesMethod?.Invoke(externalTool, new object[] { process, connectionInfo });

            // Assert
            // The arguments should contain the password with the equals sign
            // It may be escaped (e.g., Z-3^=Wv99/Aq), but should not be split
            Assert.That(process.StartInfo.Arguments, Does.Contain("Z-3"));
            Assert.That(process.StartInfo.Arguments, Does.Contain("Wv99/Aq"));
            // The equals sign should be present (possibly escaped as ^=)
            Assert.That(process.StartInfo.Arguments, Does.Match("Z-3.=Wv99/Aq"));
        }

        [Test]
        public void PasswordWithSpecialCharactersIsPassedCorrectly()
        {
            // Arrange
            var connectionInfo = new ConnectionInfo
            {
                Password = "P@ss=W0rd!",
                Hostname = "testhost",
                Username = "testuser"
            };

            var externalTool = new ExternalTool
            {
                DisplayName = "Test Tool",
                FileName = "test.exe",
                Arguments = "-p %PASSWORD%"
            };

            // Act
            var process = new Process();
            var setProcessPropertiesMethod = typeof(ExternalTool).GetMethod(
                "SetProcessProperties",
                BindingFlags.NonPublic | BindingFlags.Instance
            );
            setProcessPropertiesMethod?.Invoke(externalTool, new object[] { process, connectionInfo });

            // Assert
            // The password should be present in the arguments (possibly escaped)
            Assert.That(process.StartInfo.Arguments, Does.Contain("P@ss"));
            Assert.That(process.StartInfo.Arguments, Does.Contain("W0rd"));
        }

        [Test]
        public void MultipleArgumentsAreParsedCorrectly()
        {
            // Arrange
            var connectionInfo = new ConnectionInfo
            {
                Password = "TestPass=123",
                Hostname = "myhost.com",
                Username = "admin",
                Port = 8080
            };

            var externalTool = new ExternalTool
            {
                DisplayName = "Test Tool",
                FileName = "app.exe",
                Arguments = "--host %HOSTNAME% --port %PORT% --user %USERNAME% --pass %PASSWORD%"
            };

            // Act
            var process = new Process();
            var setProcessPropertiesMethod = typeof(ExternalTool).GetMethod(
                "SetProcessProperties",
                BindingFlags.NonPublic | BindingFlags.Instance
            );
            setProcessPropertiesMethod?.Invoke(externalTool, new object[] { process, connectionInfo });

            // Assert
            var arguments = process.StartInfo.Arguments;
            Assert.That(arguments, Does.Contain("myhost.com"));
            Assert.That(arguments, Does.Contain("8080"));
            Assert.That(arguments, Does.Contain("admin"));
            Assert.That(arguments, Does.Contain("TestPass"));
            Assert.That(arguments, Does.Contain("123"));
        }
    }
}
