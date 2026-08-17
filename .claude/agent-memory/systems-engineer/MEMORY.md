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
- CORE-203/CORE-204 (issue #11, `Ton`/`Tof`): marked `Verified` in RTVM 2026-08-17 on branch `issue-11` (fast-path after Test Engineer TP-203/TP-204 pass, commits `f22c025`/`c754615`), handed to CI/CD; commit SHA pending in RTVM Commit column. `IInstruction.Evaluate` gained a third `elapsed: TimeSpan` parameter — reviewed and signed off, see sdd_decisions memory. Feature-branch note: RTVM/SDD/memory edits for `[RTVM-014]`-style issues belong on the issue's own `issue-<n>` branch (per `.github/AGENT_LABELS.md`'s Branch convention), not `main` — feature branches can lag `main` if other issues merged concurrently (confirmed here: `origin/issue-11`'s docs/RTVM.md was missing DATA-IN-103/CORE-201/202 status updates that `main` already had from issues #8/#10). Non-overlapping table rows merge cleanly; this long single-line Documentation-index bullet does not, so append new decisions as their own new bullet line here rather than editing the shared long line, to avoid a same-line merge conflict with whatever other concurrently-running issue also touched it.
