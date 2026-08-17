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

## Documentation index

<!-- Where the current SDD, interface docs, and test procedures live for
     each active product line (VR HMD, gesture-tracking gloves, video
     jukebox controller). -->

- PLC_Emulator: `docs/RTVM.md` populated & Approved (issue #2, closed). `docs/SDD.md` populated (issue #3, closed). `docs/IMPLEMENTATION_PLAN.md` populated, all downstream issues created (issue #4, closed). Generate Code Base scaffolding verified & closed (issue #5, closed 2026-08-16). Feature work now lives in issues #6–#27. DATA-IN-102 (issue #7): fully closed 2026-08-16 — `Verified`, commit `5e2402a` recorded in RTVM, Test Engineer's post-merge regression pass on `main`@`5a50bce` (27/27) confirmed no further action needed; NETWORK JSON wire shape signed off (see sdd_decisions memory). CORE-200 (issue #9): `Verified`, commit `49d5150`, post-merge regression confirmed (27/27, #7+#9 merged together, no regressions) 2026-08-16 — issue closed, feature chain complete. `IInstruction.Evaluate(TagTable tags, bool rungState)` signature change signed off into SDD as part of this issue (see sdd_decisions memory).
- [SDD decisions — PLC_Emulator](sdd_decisions_plc_emulator.md) — NFR-500 controller-isolation design, TCP/JSON ICD message types (tag_update/tag_write/read_request), DELIV-900/NFR-501 REVISED 2026-08-16 to one consolidated late-stage pass (not per-feature CI matrix), IDriver lives in PlcEmulator.Core not .Drivers.
- [Implementation Plan — PLC_Emulator](implementation_plan_plc_emulator.md) — issue-number map (#5 Generate Code Base, #6–#27 one per RTVM group) and dependency rationale for feature-issue queries.
- CORE-209 (issue #15, `IDriver` architecture / `DriverFactory`): marked `Verified` in RTVM 2026-08-17 (fast-path `status:ready-for-rtvm-update` after Test Engineer TP-209 pass on branch `issue-15` commit `e500151`), edit committed as `1bd1ced` on `issue-15`, handed to CI/CD. Driver-resolver design decision recorded in sdd_decisions memory. Commit SHA for the CI/CD merge still pending in RTVM Commit column.
