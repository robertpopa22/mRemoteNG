# AV False Positive Submission — Templates & Materials

## Current Status (2026-03-05)

**Nightly x64 (20260304):** 9/66 detections on VirusTotal
**VT Analysis:** `https://www.virustotal.com/gui/file-analysis/MGUyMjBlNmE5MTFkMTRkMDg5OWRkMmIxMDk2NDA2Y2M6MTc3MjY5ODM2MA==`
**ZIP SHA256:** `02817ffbbd2f8995095a44ba2ef2a16f7c03a9b9e84205e50510f83e46d5b62d`

### Key Insight: BitDefender Engine Family

All 9 detections use **BitDefender's engine** (signature `IL:Trojan.MSILZilla`). Fixing the
detection at BitDefender will cascade to 6+ resellers who license their engine:

| Vendor | Detection Name | Engine |
|--------|---------------|--------|
| **BitDefender** | IL:Trojan.MSILZilla.212190 | **BitDefender (primary)** |
| ALYac | IL:Trojan.MSILZilla.212190 | BitDefender OEM (ESTsecurity) |
| Arcabit | IL:Trojan.MSILZilla.D33CDE | BitDefender OEM (Polish AV) |
| Emsisoft | IL:Trojan.MSILZilla.212190 (B) | BitDefender OEM |
| GData | IL:Trojan.MSILZilla.212190 | BitDefender OEM |
| MicroWorld-eScan | IL:Trojan.MSILZilla.212190 | BitDefender OEM (Indian AV) |
| VIPRE | IL:Trojan.MSILZilla.212190 | BitDefender OEM |
| CTX | zip.trojan.msilzilla | SaintSecurity (Korean, BD-derived) |
| Xcitium | Application.Win32.DomaIQ.KKM@57at0e | **Independent engine** (formerly Comodo) |

**Priority:** Submit to BitDefender first (fixes 7/9), then Xcitium (independent), then CTX.

---

## Quick Reference — All Vendors

| Vendor | Submission | Contact | Priority |
|--------|-----------|---------|----------|
| **BitDefender** | [Web form](https://www.bitdefender.com/en-us/business/submit) | virus_submission@bitdefender.com | **P1 — fixes 7 vendors** |
| **Xcitium** | [Comodo form](https://www.comodo.com/home/internet-security/submit.php) | threatlabs@xcitium.com | **P2 — independent engine** |
| **CTX** | Email only | root@malwares.com | P3 — may auto-fix via BD |
| ALYac | [Web portal](https://en.estsecurity.com/support/report) | esrc@estsecurity.com | P4 — BD OEM, wait for BD fix |
| Arcabit | Email only | vt.fp@arcabit.pl | P4 — BD OEM |
| Emsisoft | Email | fp@emsisoft.com | P4 — BD OEM |
| GData | [Web form](https://submit.gdatasoftware.com/privacy) | (form only) | P4 — BD OEM |
| MicroWorld-eScan | [Web form](https://www.escanav.com/en/index.asp) | samples@escanav.com | P4 — BD OEM |
| VIPRE | [Web form](https://www.vipre.com/support/submit-false-positive/) | (form only) | P4 — BD OEM |

---

## Email Template (BitDefender) — PRIMARY

**To:** virus_submission@bitdefender.com
**Subject:** False Positive Report — mRemoteNG (IL:Trojan.MSILZilla.212190)

---

Dear BitDefender Virus Lab,

I am reporting a **false positive detection** on mRemoteNG, an open-source multi-protocol remote connections manager for Windows with **10,633 GitHub stars and 9.5M+ downloads**.

**Detection details:**
- **Detection name:** `IL:Trojan.MSILZilla.212190`
- **File:** `mRemoteNG.dll` (main application assembly, 2.5 MB)
- **Product version:** 1.82.0-beta.1
- **SHA256:** `02817ffbbd2f8995095a44ba2ef2a16f7c03a9b9e84205e50510f83e46d5b62d`
- **VirusTotal:** https://www.virustotal.com/gui/file-analysis/MGUyMjBlNmE5MTFkMTRkMDg5OWRkMmIxMDk2NDA2Y2M6MTc3MjY5ODM2MA==

**Impact:** Your detection cascades to 6 OEM vendors (ALYac, Arcabit, Emsisoft, GData, MicroWorld-eScan, VIPRE) — a single fix at BitDefender resolves 7 of 9 total detections.

**Why this is a false positive:**

mRemoteNG is one of the most widely-used open-source sysadmin tools for Windows (since 2007). The APIs that trigger heuristic detection are **required for core functionality**:

| API | Legitimate Use in mRemoteNG |
|-----|----------------------------|
| `SendInput` (user32.dll) | Releases stuck modifier keys (Ctrl/Alt/Shift) after RDP session disconnect |
| `CryptProtectData` (DPAPI) | Encrypts saved connection credentials using Windows Data Protection API |
| COM Interop (`MSTSCLib`) | Controls the Microsoft RDP ActiveX component (built into Windows) |
| `Assembly.LoadFrom()` | Loads satellite assemblies for localization and NuGet packages from app directory |
| Multiple P/Invoke | Window management, clipboard operations, focus handling — standard WinForms patterns |

**Project references:**
- **Source code:** https://github.com/mRemoteNG/mRemoteNG (10,633 stars, GPL v2)
- **Chocolatey:** https://community.chocolatey.org/packages/mRemoteNG (300K+ downloads)
- **Microsoft winget:** Official package `mRemoteNG.mRemoteNG`
- **Nightly release:** https://github.com/robertpopa22/mRemoteNG/releases/tag/nightly
- **CI workflow:** Builds are reproducible via GitHub Actions
- **Industry recognition:** Reviewed by Redmond Magazine, 4sysops, Petri IT, PDQ, Comparitech
- **Enterprise integration:** Delinea (Thycotic) PAM, Rapid7 Metasploit module
- **Code signing:** SignPath Foundation application in progress

**Sample download:** https://github.com/robertpopa22/mRemoteNG/releases/download/nightly/mRemoteNG-nightly-20260304-v1.82.0-beta.1-a94feb2-x64.zip
**SHA256:** `02817ffbbd2f8995095a44ba2ef2a16f7c03a9b9e84205e50510f83e46d5b62d`
The flagged file is `mRemoteNG.dll` (2.5 MB) inside this ZIP.

Please review and remove this false positive detection. This affects the entire mRemoteNG user community across 7 AV products that license your engine.

Thank you for your time.

Best regards,
Robert Popa
mRemoteNG Contributor & Fork Maintainer
https://github.com/robertpopa22/mRemoteNG

---

## Email Template (Xcitium) — INDEPENDENT ENGINE

**To:** threatlabs@xcitium.com
**Subject:** False Positive Report — mRemoteNG (Application.Win32.DomaIQ.KKM@57at0e)

---

Dear Xcitium Threat Labs,

I am reporting a **false positive detection** on mRemoteNG, an open-source remote connections manager for Windows (10,633 GitHub stars, 9.5M+ downloads, GPL v2).

**Detection details:**
- **Detection name:** `Application.Win32.DomaIQ.KKM@57at0e`
- **Product version:** 1.82.0-beta.1
- **SHA256:** `02817ffbbd2f8995095a44ba2ef2a16f7c03a9b9e84205e50510f83e46d5b62d`
- **VirusTotal:** https://www.virustotal.com/gui/file-analysis/MGUyMjBlNmE5MTFkMTRkMDg5OWRkMmIxMDk2NDA2Y2M6MTc3MjY5ODM2MA==

**Source code:** https://github.com/mRemoteNG/mRemoteNG
**CI builds:** https://github.com/robertpopa22/mRemoteNG (reproducible via GitHub Actions)

The application uses standard Windows APIs (SendInput, DPAPI, COM Interop for RDP ActiveX, P/Invoke for window management) which trigger heuristic detection. All APIs are documented in our source code and required for core remote desktop management functionality.

Please review and whitelist. Thank you.

Best regards,
Robert Popa
mRemoteNG Contributor

---

## Email Template (CTX / SaintSecurity)

**To:** root@malwares.com
**Subject:** False Positive Report — mRemoteNG (zip.trojan.msilzilla)

---

Dear SaintSecurity / CTX team,

False positive report for mRemoteNG (open-source remote desktop manager, 10,633 GitHub stars):

- **Detection:** `zip.trojan.msilzilla`
- **SHA256:** `02817ffbbd2f8995095a44ba2ef2a16f7c03a9b9e84205e50510f83e46d5b62d`
- **Source:** https://github.com/mRemoteNG/mRemoteNG (GPL v2)
- **VirusTotal:** https://www.virustotal.com/gui/file-analysis/MGUyMjBlNmE5MTFkMTRkMDg5OWRkMmIxMDk2NDA2Y2M6MTc3MjY5ODM2MA==

Please review and remove this false positive. The APIs triggering detection (SendInput, DPAPI, COM Interop) are standard Windows APIs used for remote desktop management.

Thank you,
Robert Popa

---

## Web Form Template (Windows Defender / WDSI)

**URL:** https://www.microsoft.com/en-us/wdsi/filesubmission

**Steps:**
1. Select **"Software developer"** role
2. Select **"Incorrectly detected as malware/malicious (false positive)"**
3. Upload the flagged file or provide the nightly ZIP URL
4. In the description field, paste:

```
mRemoteNG is an open-source (GPL v2) multi-protocol remote connections manager
for Windows, with 10,633 GitHub stars and 9.5M+ downloads since 2007.

Repository: https://github.com/mRemoteNG/mRemoteNG
Fork (CI/nightly builds): https://github.com/robertpopa22/mRemoteNG
Chocolatey: https://community.chocolatey.org/packages/mRemoteNG (300K+ downloads)
winget: mRemoteNG.mRemoteNG
License: GPL v2

The flagged APIs (SendInput, DPAPI CryptProtectData, COM Interop for RDP ActiveX,
Assembly.LoadFrom for localization) are all required for the application's core
remote desktop management functionality. These are standard Windows APIs used
as documented by Microsoft.

Code signing via SignPath Foundation is in progress.
```

---

## Preparing the Submission Package

### Get SHA256 of latest nightly

```powershell
# Download and hash
$url = "https://github.com/robertpopa22/mRemoteNG/releases/download/nightly/mRemoteNG-nightly-20260304-v1.82.0-beta.1-a94feb2-x64.zip"
Invoke-WebRequest -Uri $url -OutFile nightly.zip
certutil -hashfile nightly.zip SHA256
```

### Create password-protected ZIP for email submission

```powershell
# Extract nightly, re-zip with password "infected"
Expand-Archive nightly.zip -DestinationPath nightly-extracted
# Use 7-Zip to create password-protected archive
& "C:\Program Files\7-Zip\7z.exe" a -p"infected" -tzip submission.zip .\nightly-extracted\mRemoteNG.dll
```

### Upload to VirusTotal first

Upload the nightly ZIP to https://www.virustotal.com/ before submitting to vendors. Include the VirusTotal scan URL in all submissions — this gives vendors immediate context about other engines' verdicts.

---

## APIs Justification Reference

For detailed technical documentation of each API and why it's required, see:
- [`docs/ANTIVIRUS_FALSE_POSITIVE.md`](ANTIVIRUS_FALSE_POSITIVE.md) — user-facing guide
- [`CODE_SIGNING_POLICY.md`](CODE_SIGNING_POLICY.md) — signing policy

## After Submission

1. **Track responses** — most vendors respond within 24-72 hours
2. **Re-scan on VirusTotal** after vendor confirms fix — detection count should drop
3. **BitDefender fix cascades** — once BD whitelists, ALYac/Arcabit/Emsisoft/GData/VIPRE follow within 24-48h (engine update cycle)
4. **Note:** Each new build produces a new hash. Signed builds accumulate SmartScreen reputation automatically, making future FP reports less necessary
5. **Long-term fix:** Authenticode signing via SignPath Foundation (in progress) dramatically reduces heuristic scores

## Backup: Certum OSS Code Signing Certificate

If SignPath Foundation declines the application:

| Item | Details |
|------|---------|
| **Provider** | [Certum (Asseco)](https://shop.certum.eu/open-source-code-signing.html) |
| **Type** | OV (Organization Validated) code signing certificate |
| **Cost** | ~69 EUR certificate + ~35 EUR smartcard shipping (from Poland) |
| **Delivery** | 3-5 business days from order |
| **Duration** | 1 year (renewable) |
| **Requirements** | Open source project, organization/individual identity verification |
| **SmartScreen** | Yes — builds reputation over time (slower than EV, but functional) |
| **CI integration** | Requires USB smartcard → not CI-automatable without HSM proxy. Alternative: Azure Trusted Signing (~$10/month, cloud HSM) |

**Decision tree:**
1. SignPath Foundation approval → free, CI-integrated, HSM-backed (**preferred**)
2. SignPath declines → Certum OSS (69 EUR, 3-5 days, manual signing)
3. Need CI automation without SignPath → Azure Trusted Signing ($10/month)
