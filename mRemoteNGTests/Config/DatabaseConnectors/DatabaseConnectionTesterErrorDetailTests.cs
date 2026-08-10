using System;
using System.Runtime.Versioning;
using mRemoteNG.Config.DatabaseConnectors;
using NUnit.Framework;

namespace mRemoteNGTests.Config.DatabaseConnectors
{
    /// <summary>
    /// The options dialog used to show Language.RdpErrorUnknown for a failed SQL connection test
    /// — an RDP protocol string carrying {0}/{1} placeholders that nothing filled in, so users
    /// literally saw "An unknown error has occurred on {0} (Error {1})". The real provider error
    /// is now carried out of the tester instead. (#165)
    /// </summary>
    [TestFixture]
    [SupportedOSPlatform("windows")]
    public class DatabaseConnectionTesterErrorDetailTests
    {
        [Test]
        public void DescribeError_IncludesTheProviderMessage()
        {
            string detail = DatabaseConnectionTester.DescribeError(
                new InvalidOperationException("The server was not found or was not accessible."), null);

            Assert.That(detail, Does.Contain("The server was not found or was not accessible."));
        }

        [Test]
        public void DescribeError_AppendsTheProviderErrorNumber()
        {
            string detail = DatabaseConnectionTester.DescribeError(new InvalidOperationException("Login failed."), 18456);

            Assert.That(detail, Does.Contain("18456"));
        }

        [Test]
        public void DescribeError_OmitsAZeroOrMissingErrorNumber()
        {
            Assert.Multiple(() =>
            {
                Assert.That(DatabaseConnectionTester.DescribeError(new InvalidOperationException("boom"), 0),
                    Is.EqualTo("boom"));
                Assert.That(DatabaseConnectionTester.DescribeError(new InvalidOperationException("boom"), null),
                    Is.EqualTo("boom"));
            });
        }

        [Test]
        public void DescribeError_RedactsPasswordsThatAppearInConnectionStrings()
        {
            // Provider messages sometimes echo the connection string back.
            string detail = DatabaseConnectionTester.DescribeError(
                new InvalidOperationException("Invalid connection string: Server=db01;Uid=sa;Password=hunter2;Encrypt=no"),
                null);

            Assert.Multiple(() =>
            {
                Assert.That(detail, Does.Not.Contain("hunter2"));
                Assert.That(detail, Does.Contain("Password=***"));
                // Everything that is not a secret must survive — that is the diagnostic value.
                Assert.That(detail, Does.Contain("Server=db01"));
                Assert.That(detail, Does.Contain("Encrypt=no"));
            });
        }

        [Test]
        public void DescribeError_RedactsThePwdAliasAndIsCaseInsensitive()
        {
            string detail = DatabaseConnectionTester.DescribeError(
                new InvalidOperationException("Server=db01;pwd=s3cret;Uid=sa"), null);

            Assert.Multiple(() =>
            {
                Assert.That(detail, Does.Not.Contain("s3cret"));
                Assert.That(detail, Does.Contain("pwd=***"));
            });
        }

        [Test]
        public void DescribeError_NeverEmitsUnformattedPlaceholders()
        {
            // The regression that started #165: a message template reaching the UI with its
            // {0}/{1} placeholders intact.
            string detail = DatabaseConnectionTester.DescribeError(
                new InvalidOperationException("A network-related error occurred."), 53);

            Assert.Multiple(() =>
            {
                Assert.That(detail, Does.Not.Contain("{0}"));
                Assert.That(detail, Does.Not.Contain("{1}"));
            });
        }
    }
}
