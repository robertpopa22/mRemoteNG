using System;
using System.Security.Cryptography;
using System.Text;


namespace mRemoteNG.Security.KeyDerivation
{
    public class Pkcs5S2KeyGenerator : IKeyDerivationFunction
    {
        private readonly int _iterations;
        private readonly int _keyBitSize;

        public Pkcs5S2KeyGenerator(int keyBitSize = 256, int iterations = 1000)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(iterations, 1000);
            ArgumentOutOfRangeException.ThrowIfNegative(keyBitSize);
            _keyBitSize = keyBitSize;
            _iterations = iterations;
        }

        public byte[] DeriveKey(string password, byte[] salt)
        {
            int keyLengthBytes = _keyBitSize / 8;
            if (keyLengthBytes == 0) return [];

            byte[] passwordInBytes = Encoding.UTF8.GetBytes(password);
            try
            {
                // Use .NET native PBKDF2-HMAC-SHA1 (CNG-accelerated) instead of
                // BouncyCastle's managed Pkcs5S2ParametersGenerator.
                // Output is identical (RFC 2898) but ~5x faster at high iteration counts.
                return Rfc2898DeriveBytes.Pbkdf2(passwordInBytes, salt, _iterations, HashAlgorithmName.SHA1, keyLengthBytes);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(passwordInBytes);
            }
        }
    }
}