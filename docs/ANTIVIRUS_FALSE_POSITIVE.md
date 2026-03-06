# Antivirus False Positive — mRemoteNG

## Why does my antivirus flag mRemoteNG?

mRemoteNG is a **multi-protocol remote connection manager** that legitimately uses
Windows APIs and patterns that overlap with malware heuristic signatures. These are
all **false positives** — the APIs are required for mRemoteNG's core functionality.

### APIs that trigger heuristics

| API / Pattern | Why mRemoteNG uses it | Why AV flags it |
|---|---|---|
| `SendInput` (keyboard) | Releases stuck modifier keys (Ctrl/Alt/Shift) after RDP sessions | Input simulation is used by keyloggers |
| `CryptProtectData` (DPAPI) | Encrypts saved connection credentials using Windows Data Protection | Credential theft tools use the same API |
| `Assembly.LoadFrom()` | Loads satellite assemblies (translations) and NuGet packages from app subfolder | Malware loaders use dynamic assembly loading |
| COM Interop (`MSTSCLib`) | Controls the Microsoft RDP ActiveX component | COM automation is used in exploitation |
| Port scanning | Network discovery feature for finding hosts | Network scanners are dual-use tools |
| Multiple P/Invoke declarations | Window management, clipboard, focus handling | Bulk P/Invoke is a heuristic red flag |

### Why you can trust this build

1. **Open source** — Full source code at [github.com/mRemoteNG/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG)
2. **Code-signed** — Release builds are signed via [SignPath Foundation](https://signpath.org/) (free for open source). See [`CODE_SIGNING_POLICY.md`](CODE_SIGNING_POLICY.md) for details
3. **CI-verified** — Every build runs through automated security scanning (CodeQL + SonarCloud)
4. **VirusTotal scanned** — Nightly builds are automatically scanned via VirusTotal in CI
5. **Reproducible builds** — Build from source using `build.ps1` and compare

> **Note:** Nightly builds may not yet be signed. See [`CODE_SIGNING_POLICY.md`](CODE_SIGNING_POLICY.md)
> for current signing status. Signed builds dramatically reduce false positive detections.

## How to verify authenticity

### Check the digital signature (signed releases)

1. Right-click `mRemoteNG.exe` → Properties → Digital Signatures tab
2. Verify the signer is "SignPath Foundation" or "mRemoteNG"
3. Click Details → View Certificate to inspect the certificate chain

### Check on VirusTotal

Upload the file to [virustotal.com](https://www.virustotal.com/) and compare with known results.
A few detections (1-3 out of 70+ engines) on legitimate remote management tools is normal.

### Build from source

```powershell
git clone https://github.com/mRemoteNG/mRemoteNG.git
cd mRemoteNG
pwsh -File build.ps1
```

## How to whitelist mRemoteNG

### Bitdefender

1. Open Bitdefender → Protection → Antivirus → Settings
2. Go to **Exclusions** → **Add**
3. Add the mRemoteNG installation folder (e.g., `C:\Program Files\mRemoteNG\`)
4. Check both **On-access scanning** and **On-demand scanning**

### Windows Defender

```powershell
# Run as Administrator
Add-MpPreference -ExclusionPath "C:\Program Files\mRemoteNG\"
```

Or via GUI: Windows Security → Virus & threat protection → Manage settings →
Exclusions → Add an exclusion → Folder

### Other antivirus products

Add the mRemoteNG installation folder to your AV's exclusion/whitelist.
The specific steps vary by product — consult your AV's documentation.

## Reporting false positives

If your AV blocks mRemoteNG, please report it as a false positive:

- **Bitdefender**: [False positive report form](https://www.bitdefender.com/consumer/support/answer/29358/) or email virus_submission@bitdefender.com
- **Windows Defender**: [WDSI file submission](https://www.microsoft.com/en-us/wdsi/filesubmission) — select "Software developer" → "False positive"
- **ESET**: [support.eset.com/en/kb141](https://support.eset.com/en/kb141/)
- **Kaspersky**: [opentip.kaspersky.com](https://opentip.kaspersky.com/)
- **VirusTotal**: Upload at [virustotal.com](https://www.virustotal.com/) — results are shared with all engines

When reporting, include:
- The specific file flagged (e.g., `mRemoteNG.dll`)
- The detection name (e.g., `IL:Trojan.MSILZilla.212190`)
- Link to this GitHub repository
- The SHA256 hash of the file (`certutil -hashfile mRemoteNG.dll SHA256`)

For detailed submission templates and step-by-step instructions, see
[`docs/AV_FALSE_POSITIVE_SUBMISSION.md`](AV_FALSE_POSITIVE_SUBMISSION.md).
