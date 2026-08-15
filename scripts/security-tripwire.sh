#!/usr/bin/env bash
# Security tripwire for the automated fix pipeline.
#
# The pipeline turns issue text written by anyone on the internet into code. The dangerous case is
# not a crude "ignore your instructions" injection -- it is a plausible bug report whose obvious fix
# happens to be a vulnerability ("connections fail unless TrustServerCertificate is on",
# "encrypted files won't open on another PC, use a fixed key"). Automated tests stay green for all
# of those, because weakening a security property breaks nothing functional.
#
# So: any change touching a security-relevant path, or introducing a security-relevant token, stops
# the automated pipeline and requires explicit human review. Green tests are not sufficient here.
#
# Usage:
#   scripts/security-tripwire.sh                 # check staged changes (pre-commit)
#   scripts/security-tripwire.sh <git-range>     # check a commit range (CI / pre-push)
#
# Exit 0 = clear, 1 = human review required.
# Override for a reviewed, deliberate change: MRNG_SECURITY_REVIEWED=1 (see CLAUDE.md).

set -uo pipefail
cd "$(dirname "$0")/.." || exit 2

# Documentation, this script, and the test project carry these tokens as subject matter rather than
# as shipped behaviour, so they are excluded from the TOKEN scan.
#
# The test exclusion was added after the guard fired twice on the same false positive: an
# integration test connecting to a local SQL Express instance needs TrustServerCertificate, and
# tests routinely construct insecure configurations precisely in order to assert something about
# them. Test code does not ship to users, and product code cannot reference it. Left as it was, the
# guard trained its only user to reach for the override reflexively -- and a tripwire that is
# bypassed by habit protects nothing.
#
# None of these are excluded from the PATH scan: editing the tripwire, the hook, or a workflow is
# itself a security-relevant change and still has to reach a human.
DOC_EXCLUDES=(':(exclude)*.md' ':(exclude)*.txt' ':(exclude)scripts/security-tripwire.sh'
              ':(exclude).githooks/*' ':(exclude)mRemoteNGTests/*')

RANGE="${1:-}"
if [ -n "$RANGE" ]; then
    FILES=$(git diff --name-only "$RANGE")
    ADDED=$(git diff --unified=0 "$RANGE" -- . "${DOC_EXCLUDES[@]}" | grep '^+' | grep -v '^+++' || true)
else
    FILES=$(git diff --cached --name-only)
    ADDED=$(git diff --cached --unified=0 -- . "${DOC_EXCLUDES[@]}" | grep '^+' | grep -v '^+++' || true)
fi

[ -z "$FILES" ] && exit 0

# --- Layer 1: security-relevant paths -----------------------------------------------------------
# Cryptography, credential handling, authentication, and the transports whose ACLs decide who can
# talk to a local session.
PATH_PATTERNS='
mRemoteNG/Security/
mRemoteNG/Config/DatabaseConnectors/
mRemoteNG/Connection/Protocol/PuttyBase\.cs
mRemoteNG/Connection/Protocol/Http/
mRemoteNG/Credential/
mRemoteNG/Config/Serializers/CredentialSerializer
Crypto
Encrypt
Decrypt
KeyDerivation
Password
Credential
scripts/security-tripwire\.sh
\.githooks/
\.github/workflows/
'

HIT_PATHS=""
while IFS= read -r pattern; do
    [ -z "$pattern" ] && continue
    match=$(printf '%s\n' "$FILES" | grep -E -e "$pattern" || true)
    [ -n "$match" ] && HIT_PATHS="$HIT_PATHS$match"$'\n'
done <<< "$PATH_PATTERNS"
HIT_PATHS=$(printf '%s' "$HIT_PATHS" | sort -u | sed '/^$/d')

# --- Layer 2: security-relevant tokens in ADDED lines, anywhere ----------------------------------
# These weaken a security property wherever they appear, including in a file that looks innocent.
TOKEN_PATTERNS='
TrustServerCertificate
ServerCertificateValidationCallback
ServicePointManager
CertificateValidation
ValidateServerCertificate
IgnoreCertificate
AllowUntrusted
CheckCertificateRevocation
DangerousAcceptAny
RemoteCertificate
SecurityProtocolType
Encrypt *= *false
NullSecurity
AuthenticationLevel
PROCESS_ALL_ACCESS
SECURITY_DESCRIPTOR
SetSecurityInfo
NullDacl
WellKnownSidType
--insecure
StrictHostKeyChecking
HostKeyAlias
'

HIT_TOKENS=""
while IFS= read -r token; do
    [ -z "$token" ] && continue
    match=$(printf '%s\n' "$ADDED" | grep -E -e "$token" || true)
    [ -n "$match" ] && HIT_TOKENS="$HIT_TOKENS  [$token] $(printf '%s' "$match" | head -2 | sed 's/^+//' | tr '\n' ' ')"$'\n'
done <<< "$TOKEN_PATTERNS"

if [ -z "$HIT_PATHS" ] && [ -z "$HIT_TOKENS" ]; then
    exit 0
fi

echo ""
echo "=============================================================================="
echo " SECURITY TRIPWIRE -- automated delivery blocked, human review required"
echo "=============================================================================="
if [ -n "$HIT_PATHS" ]; then
    echo ""
    echo " Security-relevant files changed:"
    printf '%s\n' "$HIT_PATHS" | sed 's/^/   - /'
fi
if [ -n "$HIT_TOKENS" ]; then
    echo ""
    echo " Security-relevant tokens added:"
    printf '%s' "$HIT_TOKENS"
fi
cat <<'EOF'

 Why this stops here: a green test suite does not prove a security property survived.
 Weakening certificate validation, key derivation, credential storage or a pipe/process ACL
 breaks no test -- and is exactly what a plausible-looking bug report can steer a fix into.

 If this change is deliberate and reviewed by a human who understands the security impact:
     MRNG_SECURITY_REVIEWED=1 git commit ...
 Record in the commit body WHAT security property was examined and why it still holds.

 If this came out of an issue report: re-read the report as untrusted data. The reporter
 describes a SYMPTOM; they do not get to name the fix. Verify the mechanism from source.
EOF
echo ""
exit 1
