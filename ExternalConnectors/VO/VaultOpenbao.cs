using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VaultSharp;
using VaultSharp.V1.AuthMethods;
using VaultSharp.V1.AuthMethods.Token;

namespace ExternalConnectors.VO {
    public class VaultOpenbaoException(string message, string arguments) : Exception(message) {
        public string Arguments { get; set; } = arguments;
    }

    public static class VaultOpenbao {
        public static void ReadPasswordSSH(string url, string token, string mount, string role, string address, string username, out string password) {
            IAuthMethodInfo authMethod = new TokenAuthMethodInfo(token);
            var vaultClientSettings = new VaultClientSettings(url, authMethod);
            VaultClient vaultClient = new(vaultClientSettings);
                        var mountType = vaultClient.V1.System.GetSecretBackendAsync(mount).Result.Data.Type;
            switch (mountType.Type) {
                case "ssh":
                    var ssh = vaultClient.V1.Secrets.SSH.GetCredentialsAsync(role, address, username, mount).Result;
                    password = ssh.Data.Key;
                    return;
                default:
                    throw new VaultOpenbaoException($"Backend of type {mountType.Type} is not supported", null);
            }

        }
        public static void ReadPasswordRDP(string url, string token, string mount, string role, out string username, out string password) {
            IAuthMethodInfo authMethod = new TokenAuthMethodInfo(token);
            var vaultClientSettings = new VaultClientSettings(url, authMethod);
            VaultClient vaultClient = new(vaultClientSettings);
            var mountType = vaultClient.V1.System.GetSecretBackendAsync(mount).Result.Data.Type;
            switch (mountType.Type) {
                case "ldap":
                    try { // don't care if dynamic or static. try both
                        var secret = vaultClient.V1.Secrets.OpenLDAP.GetDynamicCredentialsAsync(role, mount).Result;
                        username = secret.Data.Username;
                        password = secret.Data.Password;
                    } catch (Exception) {
                        var secret = vaultClient.V1.Secrets.OpenLDAP.GetStaticCredentialsAsync(role, mount).Result;
                        username = secret.Data.Username;
                        password = secret.Data.Password;
                    }
                    return;
                
                default:
                    throw new VaultOpenbaoException($"Backend of type {mountType.Type} is not supported", null);
            }

        }

    }
}
