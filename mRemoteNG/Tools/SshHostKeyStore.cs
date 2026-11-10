using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace mRemoteNG.Tools
{
    internal enum SshHostKeyStatus
    {
        Trusted,
        Unknown,
        Changed
    }

    /// <summary>
    /// A host key the server offered, with the verdict against the trusted keys.
    /// </summary>
    internal sealed record SshHostKeyOffer(
        string Host,
        int Port,
        string KeyType,
        string KeyBase64,
        string FingerprintSha256,
        SshHostKeyStatus Status,
        string? TrustedKeyType,
        string? TrustedFingerprintSha256);

    /// <summary>
    /// The SSH host keys the user has trusted for SSH File Transfer, kept in an OpenSSH
    /// known_hosts file ("host keytype base64", "[host]:port keytype base64" for other ports) so
    /// the entries can be read, compared and removed with ordinary tools.
    ///
    /// Only plain entries are understood. Hashed host names, markers such as @cert-authority and
    /// malformed lines are kept in the file as they are and never grant trust.
    /// </summary>
    internal sealed class SshHostKeyStore
    {
        internal const string FileName = "ssh_known_hosts";

        private readonly string _filePath;

        public SshHostKeyStore(string filePath)
        {
            _filePath = filePath;
        }

        public string FilePath => _filePath;

        public static string Fingerprint(byte[] key) =>
            Convert.ToBase64String(SHA256.HashData(key)).TrimEnd('=');

        /// <summary>
        /// The key type named inside the public-key blob (its first SSH string, e.g. "ssh-rsa"),
        /// or null when the blob does not start with one. The negotiated signature algorithm is not
        /// used: an RSA key is offered as rsa-sha2-256 or rsa-sha2-512 depending on negotiation,
        /// while OpenSSH and this file name it ssh-rsa.
        /// </summary>
        public static string? KeyTypeOf(byte[] key)
        {
            if (key is null || key.Length < 4)
                return null;
            int length = (key[0] << 24) | (key[1] << 16) | (key[2] << 8) | key[3];
            if (length <= 0 || length > 64 || length > key.Length - 4)
                return null;
            string type = Encoding.ASCII.GetString(key, 4, length);
            return type.All(c => c > ' ' && c < 127) ? type : null;
        }

        internal static string HostToken(string host, int port)
        {
            string h = host.Trim().ToLowerInvariant();
            return port == 22 ? h : $"[{h}]:{port}";
        }

        public SshHostKeyOffer Check(string host, int port, string keyType, byte[] key)
        {
            ArgumentNullException.ThrowIfNull(key);
            string token = HostToken(host, port);
            string offered = Convert.ToBase64String(key);
            string fingerprint = Fingerprint(key);

            List<(string Type, string Key)> known = ReadEntries()
                .Where(e => e.Token == token)
                .Select(e => (e.Type, e.Key))
                .ToList();

            if (known.Any(k => k.Type == keyType && k.Key == offered))
                return new SshHostKeyOffer(host, port, keyType, offered, fingerprint, SshHostKeyStatus.Trusted, keyType, fingerprint);

            if (known.Count == 0)
                return new SshHostKeyOffer(host, port, keyType, offered, fingerprint, SshHostKeyStatus.Unknown, null, null);

            (string type, string trustedKey) = known[0];
            return new SshHostKeyOffer(host, port, keyType, offered, fingerprint, SshHostKeyStatus.Changed,
                                       type, FingerprintOfBase64(trustedKey));
        }

        /// <summary>
        /// Trusts the offered key for its host and port, replacing any key trusted for them before.
        /// </summary>
        public void Trust(SshHostKeyOffer offer)
        {
            ArgumentNullException.ThrowIfNull(offer);
            string token = HostToken(offer.Host, offer.Port);

            List<string> lines = File.Exists(_filePath)
                ? File.ReadAllLines(_filePath, Encoding.UTF8).ToList()
                : ["# SSH host keys trusted by mRemoteNG SSH File Transfer (OpenSSH known_hosts format)"];

            lines.RemoveAll(line => TryParse(line, out Entry e) && e.Token == token);
            lines.Add($"{token} {offer.KeyType} {offer.KeyBase64}");

            string? directory = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            string temp = _filePath + ".tmp";
            File.WriteAllLines(temp, lines, new UTF8Encoding(false));
            File.Move(temp, _filePath, overwrite: true);
        }

        private static string? FingerprintOfBase64(string base64)
        {
            try { return Fingerprint(Convert.FromBase64String(base64)); }
            catch (FormatException) { return null; }
        }

        private readonly record struct Entry(string Token, string Type, string Key);

        private IEnumerable<Entry> ReadEntries()
        {
            if (!File.Exists(_filePath))
                yield break;

            foreach (string line in File.ReadAllLines(_filePath, Encoding.UTF8))
            {
                if (TryParse(line, out Entry entry))
                    yield return entry;
            }
        }

        private static bool TryParse(string line, out Entry entry)
        {
            entry = default;
            string trimmed = line.Trim();
            if (trimmed.Length == 0 || trimmed[0] == '#' || trimmed[0] == '@' || trimmed[0] == '|')
                return false;

            string[] parts = trimmed.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 3)
                return false;

            // One line may list several names for the same key: "host1,host2 keytype base64".
            string[] names = parts[0].Split(',', StringSplitOptions.RemoveEmptyEntries);
            if (names.Length != 1)
                return false;

            entry = new Entry(names[0].ToLowerInvariant(), parts[1], parts[2]);
            return true;
        }
    }
}
