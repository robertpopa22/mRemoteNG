Triage update from fork validation:

- LDAP path/query input is sanitized before `DirectoryEntry` use in `ActiveDirectoryDeserializer` (constructor uses `LdapPathSanitizer.SanitizeLdapPath(...)`).
- Sanitization utility exists centrally in `mRemoteNG/Security/LdapPathSanitizer.cs` and is used by AD-related flows.
- Security tests exist in `mRemoteNGTests/Security/LdapPathSanitizerTests.cs` covering common LDAP injection patterns and malformed query/fragment payloads.

Given current code state, this issue looks mitigated in code. If there is still a reproducible vector, please attach a concrete payload + execution path on latest branch so it can be fixed/verified quickly; otherwise this can be closed as addressed.
