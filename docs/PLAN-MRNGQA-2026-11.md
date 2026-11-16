# MRNGQA — Free verification integrations

## Scope and completion

Integrate CodeRabbit OSS, Stryker.NET and Semgrep without paid subscriptions; apply for Greptile OSS. Verify actual results, document service activation separately from local configuration, and update the maintenance procedure and README. The follow-up explicitly authorizes fixing the three Semgrep alerts, improving mutation tests and pushing after successful required checks. Paid overages and private-repository access remain excluded.

| Objective | State | Evidence |
|---|---|---|
| MRNGQA-O1 CodeRabbit OSS | Integrated; no review yet | App limited to this fork, validated YAML; broader CLI OAuth declined |
| MRNGQA-O2 Stryker.NET | Complete | Linked-source pilot improved from 62.07% to 94.83%; 16 cases, same 65 mutants |
| MRNGQA-O3 Semgrep CE | Triaged with disclosed limits | KiTTY fixed; two SQL false positives reviewed through 43 callers; 23 parser diagnostics remain |
| MRNGQA-O4 Greptile OSS | Accepted | Repository enabled; 100 credits/month; zero reviews |
| MRNGQA-O5 Documentation | Complete | Procedure, README badges, verification guide and handover |

## În lucru

None. Integration and the requested follow-up are complete.

## De făcut

No required task remains in this scope. Semgrep parser gaps and pre-existing
Sonar/full-history secret findings remain disclosed follow-up work. Check live
credits before any future hosted review; none was invoked by this plan.

## Rezolvate

| Step | Evidence |
|---|---|
| MRNGQA-P1 Service onboarding | Exact app grants approved for this fork; Greptile OSS accepted; no payment or paid product |
| MRNGQA-P2 Mutation testing | Stryker 5.1.0, real linked source/tests, HTML/JSON; clean-snapshot repetition |
| MRNGQA-P3 Static scanning | CE 1.185.0 and pinned rules; Windows/Linux JSON/SARIF; scanner errors preserved |
| MRNGQA-P4 Integration validation | Full build exit 0; 4,124 tests passed, zero crashes; YAML schema and script syntax valid |
| MRNGQA-P5 Documentation and publication | Skill, README badges, guide and PROGRESS; tooling-only publication with skip-ci |
| MRNGQA-P6 Cleanup | Reports retained outside tracked source; CLI OAuth listeners stopped; no generated output staged |
| MRNGQA-P7 SQL alert disposition | False positives for all 43 current callers; raw findings retained |
| MRNGQA-P8 KiTTY path validation | Five unsafe paths reproduced before fix and rejected after; encoded literal names preserved; real NUnit regression passed |
| MRNGQA-P9 Mutation test improvement | 94.83%, 55 killed, 3 equivalent survivors, none uncovered; source/config unchanged |
| MRNGQA-P10 Verification and publication | Build PASS; local 4,135/4,135; targeted 30/30; guest startup 3/3; tripwire PASS. Product ff0c04c2f pushed; all five CI workflows passed; nightly 4,113/4,113 and published ZIP/MSI hashes verified. Sonar gate remains red |

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
