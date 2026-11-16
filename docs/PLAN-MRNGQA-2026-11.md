# MRNGQA — Free verification integrations

## Scope and completion

Integrate CodeRabbit OSS, Stryker.NET and Semgrep without paid subscriptions; apply for Greptile OSS. Verify actual results, document service activation separately from local configuration, and update the maintenance procedure and README. No product behavior changes, paid overages or private-repository access are authorized.

| Objective | State | Evidence |
|---|---|---|
| MRNGQA-O1 CodeRabbit OSS | Integrated; no review yet | App limited to this fork, validated YAML; broader CLI OAuth declined |
| MRNGQA-O2 Stryker.NET | Complete | Linked-source pilot, seven existing tests, reproducible 62.07% |
| MRNGQA-O3 Semgrep CE | Complete with disclosed limits | 727 files / 15 rules; 3 findings, 23 parser diagnostics |
| MRNGQA-O4 Greptile OSS | Accepted | Repository enabled; 100 credits/month; zero reviews |
| MRNGQA-O5 Documentation | Complete | Procedure, README badges, verification guide and handover |

## În lucru

None. The scoped integration is complete; publication is recorded by the commit
containing this plan. Hosted review execution and source-level disposition of
baseline findings are follow-ups, not completed validations.

## De făcut

None within the requested integration. No product changes or new CI workflows.

## Rezolvate

| Step | Evidence |
|---|---|
| MRNGQA-P1 Service onboarding | Exact app grants approved for this fork; Greptile OSS accepted; no payment or paid product |
| MRNGQA-P2 Mutation testing | Stryker 5.1.0, real linked source/tests, HTML/JSON; clean-snapshot repetition |
| MRNGQA-P3 Static scanning | CE 1.185.0 and pinned rules; Windows/Linux JSON/SARIF; scanner errors preserved |
| MRNGQA-P4 Integration validation | Full build exit 0; 4,124 tests passed, zero crashes; YAML schema and script syntax valid |
| MRNGQA-P5 Documentation and publication | Skill, README badges, guide and PROGRESS; tooling-only publication with skip-ci |
| MRNGQA-P6 Cleanup | Reports retained outside tracked source; CLI OAuth listeners stopped; no generated output staged |

## Decisions

- MRNGQA-D1: Maintainer authorizes these integrations and the Greptile OSS application, with no cost. Paid plans, overage billing and paid trials are excluded.
- MRNGQA-D2: Keep the existing main-only workflow. Do not create an artificial PR to trigger review.
- MRNGQA-D3: Integration is not evidence of full coverage. Record pilot scope, skipped checks, findings and pending vendor acceptance explicitly.
- MRNGQA-D4: Check live provider usage before and after hosted reviews. Reserve 20 of Greptile's 100 monthly credits, never double-count trial/OSS grants, and stop on unknown allowance. No paid overages or automatic top-ups.
- MRNGQA-D5: Provider-created no-card trials remain separate from OSS. No conversion or payment is authorized. Tooling/docs publication uses `[skip ci]` to preserve the existing nightly.

## Change history

- 2026-11-16 10:41 Europe/Bucharest: plan opened; repository initially clean at `5e429aae4`.
- 2026-11-16: integrations verified, Greptile OSS accepted, badges and credit controls added; no hosted review completed and baseline findings remain disclosed.
