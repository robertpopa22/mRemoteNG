using System;
using System.Linq;
using System.Security;
using mRemoteNG.Tools;

namespace mRemoteNG.Security.Authentication
{
    public class PasswordAuthenticator : IAuthenticator
    {
        private readonly ICryptographyProvider _cryptographyProvider;
        private readonly string _cipherText;
        private readonly Func<Optional<SecureString>> _authenticationRequestor;
        private readonly Func<string, bool>? _plaintextValidator;

        public PasswordAuthenticator(ICryptographyProvider cryptographyProvider,
                                     string cipherText,
                                     Func<Optional<SecureString>> authenticationRequestor,
                                     Func<string, bool>? plaintextValidator = null)
        {
            ArgumentNullException.ThrowIfNull(cryptographyProvider);
            ArgumentNullException.ThrowIfNull(authenticationRequestor);
            if (string.IsNullOrEmpty(cipherText))
                throw new ArgumentException("Value cannot be null or empty.", nameof(cipherText));
            _cryptographyProvider = cryptographyProvider;
            _cipherText = cipherText;
            _authenticationRequestor = authenticationRequestor;
            _plaintextValidator = plaintextValidator;
        }

        public int MaxAttempts { get; set; } = 3;
        public SecureString? LastAuthenticatedPassword { get; private set; }

        public bool Authenticate(SecureString password)
        {
            LastAuthenticatedPassword = null;
            int attempts = 0;
            while (attempts < MaxAttempts)
            {
                try
                {
                    string plaintext = _cryptographyProvider.Decrypt(_cipherText, password);
                    if (_plaintextValidator is null || _plaintextValidator(plaintext))
                    {
                        LastAuthenticatedPassword = password;
                        return true;
                    }
                }
                catch
                {
                    // A failed decryption and a rejected plaintext use the same retry path.
                }

                Optional<SecureString> providedPassword = _authenticationRequestor();
                if (!providedPassword.Any())
                    return false;

                password = providedPassword.First();
                if (password == null || password.Length == 0) break;

                attempts++;
            }

            return false;
        }
    }
}
