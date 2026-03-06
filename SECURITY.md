# Security Policy

## Supported Versions

| Version | Supported |
|---------|-----------|
| 1.82.x (nightly) | :white_check_mark: Active development |
| 1.81.x | :white_check_mark: Latest stable release |
| 1.76.x and earlier | :x: End of life |

## Reporting a Vulnerability

**Please do NOT report security vulnerabilities through public GitHub issues.**

Instead, use one of these channels:

1. **GitHub Security Advisories (preferred):**
   Go to [Security → Advisories → New draft advisory](https://github.com/robertpopa22/mRemoteNG/security/advisories/new) to report privately.

2. **Email:** Create a private advisory on GitHub (no public email for security reports).

### What to include

- Description of the vulnerability
- Steps to reproduce
- Affected version(s)
- Impact assessment (if known)

### Response timeline

- **Acknowledgment:** within 48 hours
- **Initial assessment:** within 7 days
- **Fix or mitigation:** depends on severity (critical: ASAP, high: 14 days, medium: 30 days)

### What to expect

- You will receive an acknowledgment with a tracking reference
- We will keep you informed of progress toward a fix
- We will credit you in the release notes (unless you prefer anonymity)
- We will NOT take legal action against researchers who follow responsible disclosure

## Code Signing

All release builds are signed via [SignPath Foundation](https://signpath.org/) (Authenticode).
See [`CODE_SIGNING_POLICY.md`](docs/CODE_SIGNING_POLICY.md) for details.

## Security Measures

- **DPAPI encryption** for stored credentials (Windows Data Protection API)
- **No plaintext secrets** in configuration files
- **CI security scanning** via CodeQL (weekly) and SonarCloud (per push)
- **Dependency scanning** via GitHub Dependabot
- **Code review** required for all pull requests

## Antivirus False Positives

mRemoteNG uses legitimate Windows APIs (SendInput, DPAPI, COM Interop) that may trigger
heuristic antivirus detections. These are **false positives**. See
[`docs/ANTIVIRUS_FALSE_POSITIVE.md`](docs/ANTIVIRUS_FALSE_POSITIVE.md) for details.
