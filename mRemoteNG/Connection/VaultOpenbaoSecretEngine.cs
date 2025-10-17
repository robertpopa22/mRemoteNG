using mRemoteNG.Resources.Language;
using mRemoteNG.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mRemoteNG.Connection {
    public enum VaultOpenbaoSecretEngine {
        [LocalizedAttributes.LocalizedDescription(nameof(Language.VaultOpenbaoSecretEngineKeyValue))]
        Kv = 0,

        [LocalizedAttributes.LocalizedDescription(nameof(Language.VaultOpenbaoSecretEngineLDAPDynamic))]
        LdapDynamic = 1,

        [LocalizedAttributes.LocalizedDescription(nameof(Language.VaultOpenbaoSecretEngineLDAPStatic))]
        LdapStatic = 2,
    }
}
