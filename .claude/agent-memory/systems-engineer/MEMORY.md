# Systems Engineer — memory

## RTVM conventions

<!-- ID scheme (e.g. RTVM-<product-prefix>-###), category tags in use,
     and the verification-method vocabulary (test, analysis,
     demonstration, inspection). -->

- [RTVM conventions — PLC_Emulator](rtvm_conventions_plc_emulator.md) — default skeleton categories (UI/DATA-IN/CORE/DATA-OUT/OUT/NFR/DELIV) reused as-is; category meanings as applied to this project.

## Cross-product interface standards

<!-- Contracts shared across product lines — e.g. a common sensor-polling
     interface, a shared audio-latency budget, a common telemetry
     schema — so the same interface doesn't get redefined per product. -->

## Requirements patterns and traps

<!-- Requirement types that tend to get written ambiguously, and the
     phrasing that's proven to tighten them up. -->

- [PLC domain requirements traps](requirements_traps_plc_domain.md) — resolve standard domain-implementation gaps (e.g. missing companion instructions) yourself and document as an assumption; don't escalate those as scope ambiguity.

## Feedback

- [Platform verification schedule](feedback_platform_verification_schedule.md) — default to one-time late-stage consolidation for target-platform verification, not a per-feature dual-runner CI matrix, even when the second runner is "free."
- [Label reconciliation](feedback_label_reconciliation.md) — on a label-reconciliation flag (mutually exclusive status labels), verify real state via git/RTVM before acting; don't trust the kept label or assume partial work either way.

## Documentation index

<!-- Where the current SDD, interface docs, and test procedures live for
     each active product line (VR HMD, gesture-tracking gloves, video
     jukebox controller). -->

- PLC_Emulator: `docs/RTVM.md`/`docs/SDD.md`/`docs/IMPLEMENTATION_PLAN.md` all populated & closed (issues #2-#4). Generate Code Base done (#5). Feature work in issues #6-#27; #6-#19 and #23 (NFR-500, commit `5df0234`) all fully closed/verified as of 2026-08-17 (#17 UI-002 commit `148648d`, #19 DATA-OUT-301 commit `00f44ee`) — see [[issue-closeout-log-plc-emulator]] for per-issue commit SHAs, merge-conflict notes, and open caveats.
- [SDD decisions — PLC_Emulator](sdd_decisions_plc_emulator.md) — NFR-500 controller-isolation design, TCP/JSON ICD message types (tag_update/tag_write/read_request), DELIV-900/NFR-501 one consolidated late-stage pass (not per-feature CI matrix), IDriver lives in PlcEmulator.Core not .Drivers, DriverResolver pattern, Tag.Fault fault-flag mechanism.
- [Implementation Plan — PLC_Emulator](implementation_plan_plc_emulator.md) — issue-number map (#5 Generate Code Base, #6–#27 one per RTVM group) and dependency rationale for feature-issue queries.
- [Issue closeout log — PLC_Emulator](issue_closeout_log_plc_emulator.md) — detailed per-issue history (#6-#18): RTVM verify status, CI/CD merge SHAs, conflict resolutions, still-open caveats (e.g. TP-001 listening-clause re-verification pending issue #20/OUT-400), and recurring merge/signature-conflict patterns (`Evaluate` arity growth, `PlcController` ctor, shallow-clone `merge-base` failures, feature-branch lag).
