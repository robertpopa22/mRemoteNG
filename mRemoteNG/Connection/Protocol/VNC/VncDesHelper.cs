using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;

namespace mRemoteNG.Connection.Protocol.VNC
{
    /// <summary>
    /// Performs VNC DES-ECB challenge encryption using Windows BCrypt.
    /// .NET 10 rejects DES "weak keys" (e.g. all-zeros after bit-reversal),
    /// but the VNC RFB protocol requires them. Windows BCrypt has no such
    /// restriction, so we P/Invoke directly (#54).
    /// </summary>
    [SupportedOSPlatform("windows")]
    internal static class VncDesHelper
    {
        /// <summary>
        /// Encrypts a 16-byte VNC challenge with the password using DES-ECB,
        /// following the RFB protocol's bit-reversal convention.
        /// </summary>
        public static byte[] EncryptChallenge(string password, byte[] challenge)
        {
            byte[] key = new byte[8];
            System.Text.Encoding.ASCII.GetBytes(password, 0, Math.Min(password.Length, 8), key, 0);
            for (int i = 0; i < 8; i++)
                key[i] = ReverseBits(key[i]);

            byte[] input1 = new byte[8], input2 = new byte[8];
            Buffer.BlockCopy(challenge, 0, input1, 0, 8);
            Buffer.BlockCopy(challenge, 8, input2, 0, 8);

            byte[] response = new byte[16];
            byte[] enc1 = DesEcbEncryptBlock(key, input1);
            byte[] enc2 = DesEcbEncryptBlock(key, input2);
            Buffer.BlockCopy(enc1, 0, response, 0, 8);
            Buffer.BlockCopy(enc2, 0, response, 8, 8);
            return response;
        }

        private static byte ReverseBits(byte b)
        {
            return (byte)(
                ((b & 0x01) << 7) | ((b & 0x02) << 5) | ((b & 0x04) << 3) | ((b & 0x08) << 1) |
                ((b & 0x10) >> 1) | ((b & 0x20) >> 3) | ((b & 0x40) >> 5) | ((b & 0x80) >> 7));
        }

        private static byte[] DesEcbEncryptBlock(byte[] key, byte[] input)
        {
            nint hAlg = 0, hKey = 0;
            try
            {
                int status = BCryptOpenAlgorithmProvider(out hAlg, "DES", null, 0);
                if (status != 0) throw new InvalidOperationException($"BCryptOpenAlgorithmProvider failed: 0x{status:X8}");

                status = BCryptSetProperty(hAlg, "ChainingMode", "ChainingModeECB",
                    System.Text.Encoding.Unicode.GetByteCount("ChainingModeECB") + 2, 0);
                if (status != 0) throw new InvalidOperationException($"BCryptSetProperty failed: 0x{status:X8}");

                status = BCryptGenerateSymmetricKey(hAlg, out hKey, null, 0, key, key.Length, 0);
                if (status != 0) throw new InvalidOperationException($"BCryptGenerateSymmetricKey failed: 0x{status:X8}");

                byte[] output = new byte[8];
                status = BCryptEncrypt(hKey, input, 8, IntPtr.Zero, null, 0, output, 8, out _, 0);
                if (status != 0) throw new InvalidOperationException($"BCryptEncrypt failed: 0x{status:X8}");

                return output;
            }
            finally
            {
                // Cleanup status is deliberately discarded: this runs in a finally, there is no
                // recovery from a failed handle release, and throwing here would mask the real
                // outcome of the operation.
                if (hKey != 0) _ = BCryptDestroyKey(hKey);
                if (hAlg != 0) _ = BCryptCloseAlgorithmProvider(hAlg, 0);
            }
        }

        [DllImport("bcrypt.dll", CharSet = CharSet.Unicode)]
        private static extern int BCryptOpenAlgorithmProvider(out nint phAlgorithm, string pszAlgId, string? pszImplementation, uint dwFlags);

        [DllImport("bcrypt.dll")]
        private static extern int BCryptCloseAlgorithmProvider(nint hAlgorithm, uint dwFlags);

        [DllImport("bcrypt.dll", CharSet = CharSet.Unicode)]
        private static extern int BCryptSetProperty(nint hObject, string pszProperty, string pbInput, int cbInput, uint dwFlags);

        [DllImport("bcrypt.dll")]
        private static extern int BCryptGenerateSymmetricKey(nint hAlgorithm, out nint phKey, byte[]? pbKeyObject, int cbKeyObject, byte[] pbSecret, int cbSecret, uint dwFlags);

        [DllImport("bcrypt.dll")]
        private static extern int BCryptEncrypt(nint hKey, byte[] pbInput, int cbInput, IntPtr pPaddingInfo, byte[]? pbIV, int cbIV, byte[] pbOutput, int cbOutput, out int pcbResult, uint dwFlags);

        [DllImport("bcrypt.dll")]
        private static extern int BCryptDestroyKey(nint hKey);
    }
}
