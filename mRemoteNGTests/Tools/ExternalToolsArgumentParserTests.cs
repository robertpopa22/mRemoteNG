using System;
using System.Collections;
using mRemoteNG.Connection;
using mRemoteNG.Connection.Protocol;
using mRemoteNG.Security;
using mRemoteNG.Tools;
using NUnit.Framework;


namespace mRemoteNGTests.Tools
{
    public class ExternalToolsArgumentParserTests
    {
        private ExternalToolArgumentParser _argumentParser;
        private const string TestString = @"()%!^abc123*<>&|""'\";
        private const string StringAfterMetacharacterEscaping = @"^(^)^%^!^^abc123*^<^>^&^|^""'\";
        private const string StringAfterAllEscaping = @"^(^)^%^!^^abc123*^<^>^&^|\^""'\";
        private const string StringAfterNoEscaping = TestString;
        private const int Port = 9933;
        private const string PortAsString = "9933";
        private const string ProtocolAsString = "RDP";
        private const string SampleCommandString = @"/k echo ()%!^abc123*<>&|""'\";


        [OneTimeSetUp]
        public void Setup()
        {
            var connectionInfo = new ConnectionInfo
            {
                Name = TestString,
                Hostname = TestString,
                Port = Port,
                Protocol = ProtocolType.RDP,
                Username = TestString,
                //Password = TestString.ConvertToSecureString(),
                Password = TestString,
                Domain = TestString,
                Description = TestString,
                MacAddress = TestString,
                UserField = TestString,
                EnvironmentTags = TestString,
                SSHOptions = TestString,
                PuttySession = TestString
            };
            _argumentParser = new ExternalToolArgumentParser(connectionInfo);
        }

        [OneTimeTearDown]
        public void Teardown()
        {
            _argumentParser = null;
        }

        [TestCaseSource(typeof(ParserTestsDataSource), nameof(ParserTestsDataSource.TestCases))]
        public string ParserTests(string argumentString)
        {
            return _argumentParser.ParseArguments(argumentString);
        }

        [Test]
        public void NullConnectionInfoResultsInEmptyVariables()
        {
            var parser = new ExternalToolArgumentParser(null);
            var parsedText = parser.ParseArguments("test %USERNAME% test");
            Assert.That(parsedText, Is.EqualTo("test  test"));
        }



        private class ParserTestsDataSource
        {
            public static IEnumerable TestCases
            {
                get
                {
                    yield return new TestCaseData("%NAME%").Returns(StringAfterAllEscaping);
                    yield return new TestCaseData("%-NAME%").Returns(StringAfterMetacharacterEscaping);
                    yield return new TestCaseData("%!NAME%").Returns(StringAfterNoEscaping);
                    yield return new TestCaseData("%HOSTNAME%").Returns(StringAfterAllEscaping);
                    yield return new TestCaseData("%-HOSTNAME%").Returns(StringAfterMetacharacterEscaping);
                    yield return new TestCaseData("%!HOSTNAME%").Returns(StringAfterNoEscaping);
                    yield return new TestCaseData("%PORT%").Returns(PortAsString);
                    yield return new TestCaseData("%-PORT%").Returns(PortAsString);
                    yield return new TestCaseData("%!PORT%").Returns(PortAsString);
                    yield return new TestCaseData("%USERNAME%").Returns(StringAfterAllEscaping);
                    yield return new TestCaseData("%-USERNAME%").Returns(StringAfterMetacharacterEscaping);
                    yield return new TestCaseData("%!USERNAME%").Returns(StringAfterNoEscaping);
                    yield return new TestCaseData("%PASSWORD%").Returns(StringAfterAllEscaping);
                    yield return new TestCaseData("%-PASSWORD%").Returns(StringAfterMetacharacterEscaping);
                    yield return new TestCaseData("%!PASSWORD%").Returns(StringAfterNoEscaping);
                    yield return new TestCaseData("%DOMAIN%").Returns(StringAfterAllEscaping);
                    yield return new TestCaseData("%-DOMAIN%").Returns(StringAfterMetacharacterEscaping);
                    yield return new TestCaseData("%!DOMAIN%").Returns(StringAfterNoEscaping);
                    yield return new TestCaseData("%DESCRIPTION%").Returns(StringAfterAllEscaping);
                    yield return new TestCaseData("%-DESCRIPTION%").Returns(StringAfterMetacharacterEscaping);
                    yield return new TestCaseData("%!DESCRIPTION%").Returns(StringAfterNoEscaping);
                    yield return new TestCaseData("%MACADDRESS%").Returns(StringAfterAllEscaping);
                    yield return new TestCaseData("%-MACADDRESS%").Returns(StringAfterMetacharacterEscaping);
                    yield return new TestCaseData("%!MACADDRESS%").Returns(StringAfterNoEscaping);
                    yield return new TestCaseData("%USERFIELD%").Returns(StringAfterAllEscaping);
                    yield return new TestCaseData("%-USERFIELD%").Returns(StringAfterMetacharacterEscaping);
                    yield return new TestCaseData("%!USERFIELD%").Returns(StringAfterNoEscaping);
                    yield return new TestCaseData("%PROTOCOL%").Returns(ProtocolAsString);
                    yield return new TestCaseData("%-PROTOCOL%").Returns(ProtocolAsString);
                    yield return new TestCaseData("%!PROTOCOL%").Returns(ProtocolAsString);
                    yield return new TestCaseData("%ENVIRONMENTTAGS%").Returns(StringAfterAllEscaping);
                    yield return new TestCaseData("%-ENVIRONMENTTAGS%").Returns(StringAfterMetacharacterEscaping);
                    yield return new TestCaseData("%!ENVIRONMENTTAGS%").Returns(StringAfterNoEscaping);
                    yield return new TestCaseData("%SSHOPTIONS%").Returns(StringAfterAllEscaping);
                    yield return new TestCaseData("%-SSHOPTIONS%").Returns(StringAfterMetacharacterEscaping);
                    yield return new TestCaseData("%!SSHOPTIONS%").Returns(StringAfterNoEscaping);
                    yield return new TestCaseData("%PUTTYSESSION%").Returns(StringAfterAllEscaping);
                    yield return new TestCaseData("%-PUTTYSESSION%").Returns(StringAfterMetacharacterEscaping);
                    yield return new TestCaseData("%!PUTTYSESSION%").Returns(StringAfterNoEscaping);
                    yield return new TestCaseData("%%") {TestName = "EmptyVariableTagsNotParsed" }.Returns("%%");
                    yield return new TestCaseData("/k echo %!USERNAME%") { TestName = "ParsingWorksWhenVariableIsNotInFirstPosition" }.Returns(SampleCommandString);
                    yield return new TestCaseData("%COMSPEC%") { TestName = "EnvironmentVariablesParsed" }.Returns(Environment.GetEnvironmentVariable("comspec"));
                    yield return new TestCaseData("%UNSUPPORTEDPARAMETER%") { TestName = "UnsupportedParametersNotParsed" }.Returns("%UNSUPPORTEDPARAMETER%");
                    yield return new TestCaseData(@"\%COMSPEC\%") { TestName = "BackslashEscapedEnvironmentVariablesParsed" }.Returns(Environment.GetEnvironmentVariable("comspec"));
                    yield return new TestCaseData(@"^%COMSPEC^%") { TestName = "ChevronEscapedEnvironmentVariablesNotParsed" }.Returns("%COMSPEC%");
                }
            }
        }

        [Test]
        public void PasswordWithCommaIsNotCaretEscaped()
        {
            // Commas are cmd.exe weak delimiters — caret escaping never protected them.
            // They are now left unescaped; callers must use double-quoting instead.
            var connectionInfo = new ConnectionInfo
            {
                Password = "1234,56789"
            };
            var parser = new ExternalToolArgumentParser(connectionInfo);
            var result = parser.ParseArguments("%PASSWORD%");
            Assert.That(result, Is.EqualTo("1234,56789"));
        }

        [Test]
        public void PasswordWithSemicolonIsNotCaretEscaped()
        {
            // Semicolons are cmd.exe weak delimiters — caret escaping never protected them.
            var connectionInfo = new ConnectionInfo
            {
                Password = "1234;56789"
            };
            var parser = new ExternalToolArgumentParser(connectionInfo);
            var result = parser.ParseArguments("%PASSWORD%");
            Assert.That(result, Is.EqualTo("1234;56789"));
        }

        [Test]
        public void PasswordWithMultipleSpecialCharsIsEscaped()
        {
            // Only & is still caret-escaped (strong shell metacharacter).
            // Comma and semicolon are left raw — protected by quoting at the caller level.
            var connectionInfo = new ConnectionInfo
            {
                Password = "pass,word;test&more"
            };
            var parser = new ExternalToolArgumentParser(connectionInfo);
            var result = parser.ParseArguments("%PASSWORD%");
            Assert.That(result, Is.EqualTo("pass,word;test^&more"));
        }

        [TestCase(ProtocolType.SSH2, "SSH2")]
        [TestCase(ProtocolType.VNC, "VNC")]
        [TestCase(ProtocolType.Telnet, "Telnet")]
        [TestCase(ProtocolType.HTTP, "HTTP")]
        [TestCase(ProtocolType.HTTPS, "HTTPS")]
        [TestCase(ProtocolType.SSH1, "SSH1")]
        [TestCase(ProtocolType.Rlogin, "Rlogin")]
        [TestCase(ProtocolType.RAW, "RAW")]
        [TestCase(ProtocolType.IntApp, "IntApp")]
        [TestCase(ProtocolType.ARD, "ARD")]
        [TestCase(ProtocolType.AnyDesk, "AnyDesk")]
        public void ProtocolTokenReturnsCorrectValueForEachProtocol(ProtocolType protocol, string expected)
        {
            var connectionInfo = new ConnectionInfo { Protocol = protocol };
            var parser = new ExternalToolArgumentParser(connectionInfo);
            var result = parser.ParseArguments("%PROTOCOL%");
            Assert.That(result, Is.EqualTo(expected));
        }
    }
}
