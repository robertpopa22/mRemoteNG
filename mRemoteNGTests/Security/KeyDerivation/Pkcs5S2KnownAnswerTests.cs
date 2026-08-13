using System;
using System.Text;
using mRemoteNG.Security.KeyDerivation;
using NUnit.Framework;

namespace mRemoteNGTests.Security.KeyDerivation
{
    /// <summary>
    /// Pins the derived key against a published PBKDF2-HMAC-SHA1 test vector.
    ///
    /// The other tests in this folder only assert self-consistency -- same input, same key --
    /// so they pass no matter which algorithm is underneath. That is not enough for this class:
    /// its output keys every connection file mRemoteNG has ever encrypted, so any change to the
    /// digest, the iteration handling or the password encoding silently makes existing files
    /// undecryptable. A known-answer test is what makes that impossible to do by accident.
    ///
    /// Vector from RFC 6070 section 2: P = "password", S = "salt", c = 4096, dkLen = 20.
    /// </summary>
    public class Pkcs5S2KnownAnswerTests
    {
        [Test]
        public void DerivedKeyMatchesTheRfc6070Vector()
        {
            // 160 bits = the 20 byte dkLen the vector specifies.
            Pkcs5S2KeyGenerator generator = new(keyBitSize: 160, iterations: 4096);

            byte[] key = generator.DeriveKey("password", Encoding.UTF8.GetBytes("salt"));

            Assert.That(Convert.ToHexString(key).ToLowerInvariant(),
                        Is.EqualTo("4b007901b765489abead49d926f721d065a429c1"));
        }

        [Test]
        public void TheDefaultKeySizeStillProducesA256BitKey()
        {
            Pkcs5S2KeyGenerator generator = new();

            byte[] key = generator.DeriveKey("password", Encoding.UTF8.GetBytes("salt"));

            Assert.That(key, Has.Length.EqualTo(32));
        }

        [Test]
        public void PasswordsAreEncodedAsUtf8()
        {
            // A non-ASCII password only derives the same key on both sides of a change if the
            // encoding is preserved, which byte-identical output requires.
            Pkcs5S2KeyGenerator generator = new(keyBitSize: 160, iterations: 4096);

            byte[] viaString = generator.DeriveKey("pässwörd", Encoding.UTF8.GetBytes("salt"));
            byte[] expected = System.Security.Cryptography.Rfc2898DeriveBytes.Pbkdf2(
                Encoding.UTF8.GetBytes("pässwörd"),
                Encoding.UTF8.GetBytes("salt"),
                4096,
                System.Security.Cryptography.HashAlgorithmName.SHA1,
                20);

            Assert.That(viaString, Is.EqualTo(expected));
        }
    }
}
