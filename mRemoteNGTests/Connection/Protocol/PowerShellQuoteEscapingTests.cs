using mRemoteNG.Connection.Protocol.PowerShell;
using NUnit.Framework;

namespace mRemoteNGTests.Connection.Protocol
{
    /// <summary>
    /// The PowerShell protocol puts the host name, user name and password inside single-quoted
    /// literals on the command line. PowerShell (5.1 and 7, checked 2026-11-10) also ends such a
    /// literal at the typographic single quotes, so each of them has to be doubled like the ASCII
    /// apostrophe, or a password containing ’ runs the rest of itself as code.
    /// </summary>
    [TestFixture]
    public class PowerShellQuoteEscapingTests
    {
        [TestCase("plain", "plain")]
        [TestCase("it's", "it''s")]
        [TestCase("p‘x", "p‘‘x")]
        [TestCase("p’; Write-Host owned; ’", "p’’; Write-Host owned; ’’")]
        [TestCase("p‚x", "p‚‚x")]
        [TestCase("p‛x", "p‛‛x")]
        [TestCase("", "")]
        public void EverySingleQuoteKindIsDoubled(string value, string expected)
        {
            Assert.That(ProtocolPowerShell.EscapePsString(value), Is.EqualTo(expected));
        }

        [Test]
        public void DoubleQuotesAndOtherCharactersAreLeftAlone()
        {
            Assert.That(ProtocolPowerShell.EscapePsString("a\"b`c$d“e"), Is.EqualTo("a\"b`c$d“e"));
        }
    }
}
