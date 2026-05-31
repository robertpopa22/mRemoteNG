using System;
using System.Text;
using Org.BouncyCastle.Security;

namespace mRemoteNG.Security
{
    public static class RandomGenerator
    {
        public static string RandomString(int length)
        {
            if (length < 0)
                throw new ArgumentException($"{nameof(length)} must be a positive integer", nameof(length));

            SecureRandom randomGen = new();
            StringBuilder stringBuilder = new();
            const string availableChars =
                @"abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789`~!@#$%^&*()-_=+|[]{};:',./<>?";
            for (int x = 0; x < length; x++)
            {
                // Next(maxValue) has an exclusive upper bound, so pass Length (not Length - 1) to
                // allow the final alphabet character to be selected.
                int randomIndex = randomGen.Next(availableChars.Length);
                stringBuilder.Append(availableChars[randomIndex]);
            }

            return stringBuilder.ToString();
        }
    }
}