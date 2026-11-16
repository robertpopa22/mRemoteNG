# MRNGQA — Free verification integrations

## Scope and completion

Integrate CodeRabbit OSS, Stryker.NET and Semgrep without paid subscriptions; apply for Greptile OSS. Verify actual results, document service activation separately from local configuration, and update the maintenance procedure and README. The follow-up explicitly authorizes fixing the three Semgrep alerts, improving mutation tests and pushing after successful required checks. Paid overages and private-repository access remain excluded.

| Objective | State | Evidence |
|---|---|---|
| MRNGQA-O1 CodeRabbit OSS | Integrated; no review yet | App limited to this fork, validated YAML; broader CLI OAuth declined |
| MRNGQA-O2 Stryker.NET | Complete | Linked-source pilot improved from 62.07% through 94.83% to 100%; 17 cases, 48/48 scored mutants killed |
| MRNGQA-O3 Semgrep CE | Complete scan; findings triaged | KiTTY fixed; two SQL false positives reviewed through 43 callers; parser diagnostics resolved by O8 |
| MRNGQA-O4 Greptile OSS | Accepted | Repository enabled; 100 credits/month; zero reviews |
| MRNGQA-O5 Documentation | Complete | Procedure, README badges, verification guide and handover |
| MRNGQA-O6 Sonar Quality Gate | Complete | Latest analysis ca6927af-a7b8-4b37-94bd-c137f9305cb9: gate OK, new coverage 80.69%, A/A/A, zero bugs/vulnerabilities; thresholds and exclusions unchanged |
| MRNGQA-O7 Mutation pilot 100% | Complete and published | 48/48 scored mutants killed, 17 tests; standard-library clamp; unchanged scope/configuration |
| MRNGQA-O8 Complete Semgrep parsing | Complete and published | 900efb02d; 727/727 files, same 15 rules, zero errors/timeouts; two reviewed SQL findings retained; no additional exclusions |
| MRNGQA-O9 Recent Sonar maintainability | In progress | Verify closure of the 22 findings from the Semgrep and Sonar follow-ups with the active rules and gate |

## În lucru

| Step | Owner | Progress | Status |
|---|---|---|---|
| MRNGQA-P22 Publication and readback | Coordinator | 25% | Local checks passed. Next verify CI, the 22 issue keys, Quality Gate and nightly on the published commit |

## De făcut

None. Publication and server verification are tracked above.

Check live credits before any hosted review; none is needed for the local checks.

## Rezolvate

| Step | Evidence |
|---|---|
| MRNGQA-P21 Recent Sonar findings | 2026-11-16: source fixes for the 22 findings from the two latest product revisions. Build, 4,151 unit tests, 7 Pester tests and 3 guest startup checks pass. Semgrep scans 728 files without errors. Local new-code coverage projection 80.94% |
| MRNGQA-P20 Verification and publication | 2026-11-16 final readback: approved source diff pushed as 900efb02d; all five CI workflows pass; local 4,151/4,151, CI 4,129/4,129, guest 3/3, Semgrep 727/727 without errors. Sonar gate OK at 80.69%; release target, downloaded hashes and assembly inventory verified. VM restored; docs-only readback uses skip-ci |
| MRNGQA-P18 Parser diagnosis | 2026-11-16 15:50 checkpoint: reproduced baseline 23 PartialParsing errors; positive before/after canary and official grammar-gap evidence identify 25 primary constructors |
| MRNGQA-P19 Parser repair | 2026-11-16 15:50 checkpoint: explicit constructors in the same 23 source files; full 727-file/15-rule scan has zero errors, same two findings, same path set; no scanner/config changes; build passes |
| MRNGQA-P12 Product findings | 100%: eleven fixes and one individually documented executable-name false positive; server reports zero open bugs/vulnerabilities |
| MRNGQA-P13 Coverage | 100%: server new-code coverage 80.55%, all six gate conditions pass; no threshold/exclusion/comparison-period relaxation |
| MRNGQA-P16 Security review and publication | 100%: maintainer approved exact workflow diff and override; b685313c0 pushed; all five CI workflows pass; nightly target and downloaded ZIP/MSI hashes verified |
| MRNGQA-P17 Documentation and cleanup | 100%: README badge/results, procedure, guide, canon and handover reconciled; VM restored; evidence outside checkout; docs-only readback uses skip-ci |
| MRNGQA-P11 Current Sonar inventory | Exact analysis, all 12 bug/vulnerability findings, 759 new coverable lines and 337 uncovered captured |
| MRNGQA-P14 Mutation pilot | 100%, 48 killed, zero surviving/uncovered; five default ignored, one compile error; 17 tests |
| MRNGQA-P15 Local verification | Full build PASS; 4,151/4,151 suite; Pester 7/7; guest startup 3/3; actionlint PASS; Semgrep baseline limitations retained |
| MRNGQA-P1 Service onboarding | Exact app grants approved for this fork; Greptile OSS accepted; no payment or paid product |
| MRNGQA-P2 Mutation testing | Stryker 5.1.0, real linked source/tests, HTML/JSON; clean-snapshot repetition |
| MRNGQA-P3 Static scanning | CE 1.185.0 and pinned rules; Windows/Linux JSON/SARIF; scanner errors preserved |
| MRNGQA-P4 Integration validation | Full build exit 0; 4,124 tests passed, zero crashes; YAML schema and script syntax valid |
| MRNGQA-P5 Documentation and publication | Skill, README badges, guide and PROGRESS; tooling-only publication with skip-ci |
| MRNGQA-P6 Cleanup | Reports retained outside tracked source; CLI OAuth listeners stopped; no generated output staged |
| MRNGQA-P7 SQL alert disposition | False positives for all 43 current callers; raw findings retained |
| MRNGQA-P8 KiTTY path validation | Five unsafe paths reproduced before fix and rejected after; encoded literal names preserved; real NUnit regression passed |
| MRNGQA-P9 Mutation test improvement | 94.83%, 55 killed, 3 equivalent survivors, none uncovered; source/config unchanged |
| MRNGQA-P10 Verification and publication | Earlier Semgrep follow-up: build PASS; local 4,135/4,135; targeted 30/30; guest startup 3/3; tripwire PASS. Product ff0c04c2f pushed; all five CI workflows passed; nightly 4,113/4,113 and ZIP/MSI hashes verified. Its red Sonar gate was resolved by P12/P13/P16 |

## Decisions

- MRNGQA-D1: Maintainer authorizes these integrations and the Greptile OSS application, with no cost. Paid plans, overage billing and paid trials are excluded.
- MRNGQA-D2: Keep the existing main-only workflow. Do not create an artificial PR to trigger review.
- MRNGQA-D3: Integration is not evidence of full coverage. Record pilot scope, skipped checks, findings and pending vendor acceptance explicitly.
- MRNGQA-D4: Check live provider usage before and after hosted reviews. Reserve 20 of Greptile's 100 monthly credits, never double-count trial/OSS grants, and stop on unknown allowance. No paid overages or automatic top-ups.
- MRNGQA-D5: Provider-created no-card trials remain separate from OSS. No conversion or payment is authorized. Tooling/docs publication uses `[skip ci]` to preserve the existing nightly.

## Change history

- 2026-11-16 10:41 Europe/Bucharest: plan opened; repository initially clean at `5e429aae4`.
- 2026-11-16: integrations verified, Greptile OSS accepted, badges and credit controls added; no hosted review completed and baseline findings remain disclosed.
- 2026-11-16: follow-up opened to resolve Semgrep findings and improve the Stryker pilot through tests; existing credit controls retained.
- 2026-11-16: follow-up complete and published as ff0c04c2f. README/PROGRESS record CI readback, source-specific limits and zero hosted review-credit use.
- 2026-11-16 12:11 Europe/Bucharest: user requested Sonar gate green without relaxed thresholds and a legitimate attempt at 100% Stryker. Reopened this plan; source starts at 96faa85d2 with a clean tree. Sol/high agents handle bounded product, coverage and mutation work; coordinator reviews cross-cutting results. No hosted AI review calls planned.
- 2026-11-16: follow-up complete on b685313c0. All five CI workflows succeeded; exact-revision Sonar gate passed at 80.55% new-code coverage. Nightly ZIP/MSI downloaded and verified, 71/71 runtime assemblies present. Stryker placement pilot 100%; no hosted reviews invoked. Final documentation records measured results and remaining limits.
- 2026-11-16 15:41 Europe/Bucharest: reopened for the requested complete Semgrep scan without additional exclusions. Source starts at 3b5f6b859 with a clean tree. One native Sol/high agent diagnoses parser behavior; no hosted review calls. First checkpoint within ten minutes; #ponytail: prefer supported parser configuration over product rewrites, preserve scanner failures.
- 2026-11-16: parser follow-up published as 900efb02d after explicit source-diff security approval. Exact-revision CI/gate/release readback passes. README, procedure, verification guide and handover reconciled; no hosted review credits used.
