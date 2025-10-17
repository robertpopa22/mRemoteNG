using Microsoft.Win32;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VaultSharp;
using VaultSharp.V1.AuthMethods;
using VaultSharp.V1.AuthMethods.Token;
using VaultSharp.V1.SecretsEngines;

namespace ExternalConnectors.VO {
    public class VaultOpenbaoException(string message, string arguments) : Exception(message) {
        public string Arguments { get; set; } = arguments;
    }

    public static class VaultOpenbao {
        private static RegistryKey baseKey = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\mRemoteVaultOpenbao");
        private static string token = "";
        private static VaultClient GetClient() {
            string url = (string)baseKey.GetValue("URL", "");
            if (string.IsNullOrEmpty(url) || string.IsNullOrEmpty(token)) {
                using VaultOpenbaoConnectionForm voForm = new();
                voForm.tbUrl.Text = url;
                _ = voForm.ShowDialog();
                if (voForm.DialogResult != DialogResult.OK)
                    throw new VaultOpenbaoException($"No credential provided", null);
                url = voForm.tbUrl.Text;
                token = voForm.tbToken.Text;
            }
            IAuthMethodInfo authMethod = new TokenAuthMethodInfo(token);
            var vaultClientSettings = new VaultClientSettings(url, authMethod);
            VaultClient client = new(vaultClientSettings);
            var sysInfo = client.V1.System.GetInitStatusAsync().Result;
            if (!sysInfo) {
                MessageBox.Show("Test connection failed", "Vault Openbao", MessageBoxButtons.OK, MessageBoxIcon.Error);
                throw new VaultOpenbaoException($"Url not working", null);
            }
            baseKey.SetValue("URL", url);
            return client;
        }
        private static void TestMountType(VaultClient vaultClient, string mount, int VaultOpenbaoSecretEngine) {
            switch (vaultClient.V1.System.GetSecretBackendAsync(mount).Result.Data.Type.Type) {
                case "kv" when VaultOpenbaoSecretEngine != 0:
                    throw new VaultOpenbaoException($"Backend of type kv does not match expected type {VaultOpenbaoSecretEngine}", null);
                case "ldap" when VaultOpenbaoSecretEngine != 1 && VaultOpenbaoSecretEngine != 2:
                    throw new VaultOpenbaoException($"Backend of type ldap does not match expected type {VaultOpenbaoSecretEngine}", null);
            }
        }
        public static void ReadPasswordSSH(int secretEngine, string mount, string role, string username, out string password) {
            VaultClient vaultClient = GetClient();
            TestMountType(vaultClient, mount, secretEngine);
            switch (secretEngine) {
                case 0:
                    var kv = vaultClient.V1.Secrets.KeyValue.V2.ReadSecretAsync(role, mountPoint: mount).Result;
                    password = kv.Data.Data[username].ToString();
                    return;
                //case "ssh": // TODO: does not work with Keyboard-Interactive yet
                //    var ssh = vaultClient.V1.Secrets.SSH.GetCredentialsAsync(role, address, username, mount).Result;
                //    password = ssh.Data.Key;
                //    return;
                default:
                    throw new VaultOpenbaoException($"Backend of type {secretEngine} is not supported", null);
            }

        }
        public static void ReadPasswordRDP(int secretEngine, string mount, string role, ref string username, out string password) {
            VaultClient vaultClient = GetClient();
            TestMountType(vaultClient, mount, secretEngine);
            switch (secretEngine) {
                case 0:
                    var kv = vaultClient.V1.Secrets.KeyValue.V2.ReadSecretAsync(role, mountPoint: mount).Result;
                    password = kv.Data.Data[username].ToString();
                    return;
                case 1:
                    var ldapd = vaultClient.V1.Secrets.OpenLDAP.GetDynamicCredentialsAsync(role, mount).Result;
                    username = ldapd.Data.Username;
                    password = ldapd.Data.Password;
                    return;
                case 2:
                    var ldaps = vaultClient.V1.Secrets.OpenLDAP.GetStaticCredentialsAsync(role, mount).Result;
                    username = ldaps.Data.Username;
                    password = ldaps.Data.Password;
                    return;

                default:
                    throw new VaultOpenbaoException($"Backend of type {secretEngine} is not supported", null);
            }

        }

    }
}
