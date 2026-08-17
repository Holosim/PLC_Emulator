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
- [Workflows permission resolution — PLC_Emulator](workflows_permission_resolution_plc_emulator.md) — App can never get `workflows` scope (doesn't declare it); manual human copy from `docs/ci/` is the permanent route. `windows-verification.yml` deleted for good, not rewritten.
- **SDK/toolchain-pin claims are cheaply verifiable, don't take on faith:** to check a `global.json`/SDK-resolution bug report, copy one SDK version into an isolated dir and point `DOTNET_ROOT`/`PATH` at it (`cp -r /usr/share/dotnet/{sdk/<ver>,host,shared,dotnet} /tmp/x`) — reproduces "only version N installed" exactly, on this same Ubuntu pipeline, no second machine needed. Used successfully on PLC_Emulator #27 (2026-08-17) to confirm `rollForward: latestFeature`→`latestMajor` was the real, complete fix before handing to Software Engineer.

## Documentation index

<!-- Where the current SDD, interface docs, and test procedures live for
     each active product line (VR HMD, gesture-tracking gloves, video
     jukebox controller). -->

- PLC_Emulator: `docs/RTVM.md`/`docs/SDD.md`/`docs/IMPLEMENTATION_PLAN.md` all populated (issues #2-#4 closed). Generate Code Base done (#5). All of #6-#26 closed, 27/27 RTVM rows `Verified`. **#27 DELIV-900 post-release fix, second release cut**: client field defect (`global.json` SDK-pin) fixed by Software Engineer (`98c6485`, `rollForward`→`latestMajor`), independently re-verified by Test Engineer, RTVM flipped `In Implementation`→`Verified`, CI/CD merged as `5f4c5d6` and cut **`v1.0.339`** ("Windows SDK Build Fix") — recorded in RTVM Commit(s) column (`ecbc190, 98c6485, 5f4c5d6`), Product Manager notified, handed to `agent:test-engineer` for the post-merge regression pass CI/CD flagged; issue #27 still open pending that. First release `v1.0.2` (2026-08-17) still stands as the original; `v1.0.339` is the field-fix follow-up on the *same* still-open issue #27, not a new issue — a post-release defect reopens the original RTVM issue rather than spawning a new one. See [[issue-closeout-log-plc-emulator]] for per-issue commit SHAs, merge-conflict notes, and open caveats (incl. #21's flagged-but-unowned scan-cadence gap).
- [SDD decisions — PLC_Emulator](sdd_decisions_plc_emulator.md) — NFR-500 controller-isolation design, TCP/JSON ICD message types (tag_update/tag_write/read_request), DELIV-900/NFR-501 one consolidated late-stage pass (not per-feature CI matrix), IDriver lives in PlcEmulator.Core not .Drivers, DriverResolver pattern, Tag.Fault fault-flag mechanism.
- [Implementation Plan — PLC_Emulator](implementation_plan_plc_emulator.md) — issue-number map (#5 Generate Code Base, #6–#27 one per RTVM group) and dependency rationale for feature-issue queries.
- [Issue closeout log — PLC_Emulator](issue_closeout_log_plc_emulator.md) — detailed per-issue history (#6-#18): RTVM verify status, CI/CD merge SHAs, conflict resolutions, still-open caveats (e.g. TP-001 listening-clause re-verification pending issue #20/OUT-400), and recurring merge/signature-conflict patterns (`Evaluate` arity growth, `PlcController` ctor, shallow-clone `merge-base` failures, feature-branch lag).
