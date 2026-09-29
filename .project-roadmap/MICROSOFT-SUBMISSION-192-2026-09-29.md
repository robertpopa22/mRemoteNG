# Microsoft sample package for #192

Status: sample prepared and verified; developer submission currently requires Microsoft sign-in.
No submission ID or vendor verdict exists yet.

Source: [published v1.82.0 release](https://github.com/robertpopa22/mRemoteNG/releases/tag/v1.82.0),
the version in the original report. The September 22 retest did not identify its build.

| Field | Verified value |
|---|---|
| Archive | `mRemoteNG-v1.82.0-x64.zip` |
| Archive SHA-256 | `af7c6125dc58c4ba84488aa085829992fbaa82c8141287d645d0f37ce6fc65f7` |
| Checksum source | Release asset digest and `checksums-SHA256.txt`, both matched |
| Extracted member | `ExternalConnectors.dll` |
| DLL size | 4,609,024 bytes |
| DLL SHA-256 | `4d4002f0078c8b939938b23c33d5aa8c3b590c101198bd57800356df9ad9fdc9` |
| Authenticode | NotSigned |
| Reported detection | `Trojan:Win32/Bearfoos.A!ml` |

## Prepared explanation

We maintain the public open-source mRemoteNG fork at https://github.com/robertpopa22/mRemoteNG.
A user reported that Microsoft Defender quarantined ExternalConnectors.dll with detection
Trojan:Win32/Bearfoos.A!ml. The attached DLL is extracted without modification from our published
v1.82.0 x64 ZIP, with the verified hashes above. The component implements integrations with
credential providers and external tools; no user credentials or configuration are included.
Please analyze whether this detection is correct and provide a submission result. We suspect
a false positive but have not independently established that. A later user report on September 22
did not identify its exact build, so we cannot assert this is the same binary as that later event.

## Completion evidence required

Record the actual Microsoft submission ID, submission date and sample hash after successful
submission. Retain the vendor result separately; submission is not clearance of the detection.
