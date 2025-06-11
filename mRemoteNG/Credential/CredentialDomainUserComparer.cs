using System.Collections;
using System.Collections.Generic;
using System.Runtime.Versioning;


namespace mRemoteNG.Credential
{
    [SupportedOSPlatform("windows")]
    public class CredentialDomainUserComparer : IComparer<ICredentialRecord>, IEqualityComparer<ICredentialRecord>
    {
        public int Compare(ICredentialRecord? x, ICredentialRecord? y)
        {
            if (x == null && y == null) return 0;
            if (x == null) return -1;
            if (y == null) return 1;

            CaseInsensitiveComparer comparer = new();
            return comparer.Compare($"{x.Domain}\\{x.Username}", $"{y.Domain}\\{y.Username}");
        }

        public bool Equals(ICredentialRecord? x, ICredentialRecord? y)
        {
            return Compare(x, y) == 0;
        }

        public int GetHashCode(ICredentialRecord obj)
        {
            return obj.Domain.GetHashCode() * 17 + obj.Username.GetHashCode();
        }
    }
}