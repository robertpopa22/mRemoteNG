using System;
using System.Text;
using NUnit.Framework;
using mRemoteNG.Connection.Protocol.RDP;

namespace mRemoteNGTests.Connection.Protocol.RDP
{
    [TestFixture]
    public class AzureLoadBalanceInfoEncoderTests
    {
        [Test]
        public void Encode_BasicString_ReturnsEncoded()
        {
            var encoder = new AzureLoadBalanceInfoEncoder();
            string input = "test";
            // "test" (4 chars) -> even length. 
            // append "\r\n" -> "test\r\n" (6 chars).
            // UTF8 bytes: 116, 101, 115, 116, 13, 10. (6 bytes)
            // Unicode string from bytes: 3 chars.
            
            string output = encoder.Encode(input);
            Assert.That(output.Length, Is.EqualTo(3));
        }

        [Test]
        public void Encode_OddLengthString_ReturnsEncodedWithPadding()
        {
            var encoder = new AzureLoadBalanceInfoEncoder();
            string input = "test1";
            // "test1" (5 chars) -> odd length.
            // append " " -> "test1 " (6 chars).
            // append "\r\n" -> "test1 \r\n" (8 chars).
            // UTF8 bytes: 8 bytes.
            // Unicode string: 4 chars.
            
            string output = encoder.Encode(input);
            Assert.That(output.Length, Is.EqualTo(4));
        }

        [Test]
        public void Encode_EmptyString_ReturnsEncoded()
        {
            var encoder = new AzureLoadBalanceInfoEncoder();
            string input = "";
            // "" (0 chars) -> even.
            // append "\r\n" -> "\r\n" (2 chars).
            // UTF8 bytes: 13, 10. (2 bytes).
            // Unicode string: 1 char. 0x0A0D (2573).
            
            string output = encoder.Encode(input);
            Assert.That(output.Length, Is.EqualTo(1));
            Assert.That((int)output[0], Is.EqualTo(0x0A0D));
        }

        [Test]
        public void Encode_ComplexString_ReturnsEncoded()
        {
            var encoder = new AzureLoadBalanceInfoEncoder();
            string input = "Cookie: msts=3640205228.20480.0000";
            string output = encoder.Encode(input);
            Assert.That(output, Is.Not.Null);
            Assert.That(output, Is.Not.Empty);
        }
    }
}
