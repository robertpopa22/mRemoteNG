# SignPath Foundation Reply — 2026-03-05

## Context

Phillip Deng (SignPath) replied that they couldn't find enough references for the
fork `robertpopa22/mRemoteNG`. This email explains the fork→upstream relationship
and provides concrete references for the mRemoteNG project's reputation.

---

## Email

**To:** Phillip Deng (SignPath Foundation)
**Subject:** Re: SignPath Foundation Application — mRemoteNG (.NET 10 modernization fork)

---

Dear Phillip,

Thank you for reviewing our application. I understand the concern — the fork
`robertpopa22/mRemoteNG` is relatively new, but it is a **direct modernization
fork** of the well-established `mRemoteNG/mRemoteNG` project.

### Fork → Upstream Relationship

- **Upstream:** [github.com/mRemoteNG/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG) — the original project
- **Our fork:** [github.com/robertpopa22/mRemoteNG](https://github.com/robertpopa22/mRemoteNG) — .NET 10 modernization
- **Active upstream PR:** [#3189](https://github.com/mRemoteNG/mRemoteNG/pull/3189) — contributing all improvements back to upstream
- **Same codebase, same license (GPL v2)** — we are not a separate project, but an active contributor modernizing the platform

The fork exists because the upstream project needed a significant platform upgrade
(.NET Framework 4.8 → .NET 10), which required extensive changes that are being
contributed back incrementally. All signed releases would carry the **mRemoteNG** name
and serve the **same user community**.

### mRemoteNG Project References

mRemoteNG is one of the most widely-used open-source remote desktop managers for Windows,
with **16+ years of continuous development** (since 2007, originally mRemote by Felix Deimel).

**GitHub & Package Managers:**
| Metric | Value |
|--------|-------|
| GitHub Stars | **10,633** |
| GitHub Forks | **1,563** |
| Contributors | **110+** |
| Total Downloads | **9.5M+** (GitHub Releases) |
| Chocolatey | **300K+ downloads** — [community.chocolatey.org/packages/mRemoteNG](https://community.chocolatey.org/packages/mRemoteNG) |
| Microsoft winget | Official package: `mRemoteNG.mRemoteNG` |
| Softpedia | Listed and reviewed — [softpedia.com](https://www.softpedia.com/get/Internet/Remote-Utils/mRemoteNG.shtml) |

**IT Industry Publications & Reviews:**
| Publication | Type |
|-------------|------|
| **Redmond Magazine** (Microsoft IT publication) | Feature article |
| **Petri IT Knowledgebase** | Tutorial/review |
| **4sysops** | Feature article — "mRemoteNG: A free remote desktop manager" |
| **Paessler (PRTG)** | Listed as "essential sysadmin app" — [blog.paessler.com](https://blog.paessler.com/) |
| **PDQ** | "Best tools every sysadmin should use" |
| **Comparitech** | "12 Best Remote Desktop Connection Managers" |
| **TrustRadius** | Professional IT reviews |
| **Wikipedia** | Listed in "Comparison of remote desktop software" |

**Enterprise & Security Integrations:**
| Integration | Significance |
|-------------|-------------|
| **Rapid7 / Metasploit** | Dedicated post-exploitation module — Metasploit only creates modules for **widely deployed** software |
| **Delinea (Thycotic)** | Official enterprise PAM (Privileged Access Management) integration documentation |
| **CVE Database** | Assigned CVEs (MITRE/NVD treat it as **production software**) |

**Documentation:**
- Official docs: [mremoteng.readthedocs.io](https://mremoteng.readthedocs.io/)
- Active community: GitHub Discussions, Reddit r/sysadmin mentions

### Our Modernization Contribution

The fork has contributed:
- **5,247 analyzer warnings eliminated** (zero remaining)
- **6,123 tests** passing (from ~4,800 in upstream)
- **843 issues triaged** (702 resolved, 83.3%)
- Migration from .NET Framework 4.8 to **.NET 10** (cross-platform ready)
- CI/CD with SonarCloud, CodeQL, and VirusTotal scanning
- All changes tracked via upstream PR [#3189](https://github.com/mRemoteNG/mRemoteNG/pull/3189)

### Why Code Signing Matters

Our nightly builds currently show **8/66 VirusTotal detections** — all false positives
triggered by legitimate Windows APIs (SendInput for key release, DPAPI for credential
encryption, COM Interop for Microsoft's RDP ActiveX control). Authenticode signing via
SignPath would:
1. Dramatically reduce heuristic false positives
2. Build SmartScreen reputation for the mRemoteNG name
3. Benefit the entire mRemoteNG community (upstream + fork)

### Summary

This is not a new or unknown project — it's **mRemoteNG**, one of the most popular
sysadmin tools for Windows, with 10K+ stars and millions of downloads. Our fork is
simply the modernization effort, actively contributing back to upstream.

I would be happy to provide any additional information or arrange a call if helpful.

Best regards,
Robert Popa
mRemoteNG Contributor & Fork Maintainer
https://github.com/robertpopa22/mRemoteNG

---

*No attachments needed — all references are hyperlinked above.*
