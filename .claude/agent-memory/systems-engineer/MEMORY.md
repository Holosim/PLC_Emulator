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

- PLC_Emulator: `docs/RTVM.md` populated & Approved (issue #2, closed). `docs/SDD.md` populated (issue #3, closed). `docs/IMPLEMENTATION_PLAN.md` populated, all downstream issues created (issue #4, closed). Generate Code Base scaffolding verified & closed (issue #5, closed 2026-08-16). Feature work now lives in issues #6–#27. DATA-IN-102 (issue #7): fully closed 2026-08-16 — `Verified`, commit `5e2402a` recorded in RTVM, Test Engineer's post-merge regression pass on `main`@`5a50bce` (27/27) confirmed no further action needed; NETWORK JSON wire shape signed off (see sdd_decisions memory). CORE-200 (issue #9): `Verified`, commit `49d5150`, post-merge regression confirmed (27/27, #7+#9 merged together, no regressions) 2026-08-16 — issue closed, feature chain complete. `IInstruction.Evaluate(TagTable tags, bool rungState)` signature change signed off into SDD as part of this issue (see sdd_decisions memory). DATA-IN-103 (issue #8, `ConfigLoader.Validate`): marked `Verified` in RTVM 2026-08-16, handed to CI/CD (`status:ready-for-commit`); commit SHA still pending in RTVM Commit column, awaiting CI/CD confirmation comment. Note for future TP-005/TP-103/UI-00x work: end-to-end "process exits, no TCP listener" behavior for these two test procedures is still only unit-tested (`ConfigLoader.Validate` level) since Host/CLI wiring (`Program.cs`, UI-001/002/003) doesn't exist yet — don't be surprised when that issue lands and needs a process-level re-verification pass. CORE-201/CORE-202 (issue #10, `Xic`/`Xio`/`Ote`): marked `Verified` in RTVM 2026-08-17 (fast-path `status:ready-for-rtvm-update` after Test Engineer TP-201/TP-202 pass on branch `issue-10` commit `98b4418`), handed to CI/CD; commit SHA pending in RTVM Commit column.
- [SDD decisions — PLC_Emulator](sdd_decisions_plc_emulator.md) — NFR-500 controller-isolation design, TCP/JSON ICD message types (tag_update/tag_write/read_request), DELIV-900/NFR-501 REVISED 2026-08-16 to one consolidated late-stage pass (not per-feature CI matrix), IDriver lives in PlcEmulator.Core not .Drivers.
- [Implementation Plan — PLC_Emulator](implementation_plan_plc_emulator.md) — issue-number map (#5 Generate Code Base, #6–#27 one per RTVM group) and dependency rationale for feature-issue queries.
- CORE-203/CORE-204 (issue #11, `Ton`/`Tof`): marked `Verified` in RTVM 2026-08-17 on branch `issue-11` (fast-path after Test Engineer TP-203/TP-204 pass, commits `f22c025`/`c754615`), handed to CI/CD; commit SHA pending in RTVM Commit column. `IInstruction.Evaluate` gained a third `elapsed: TimeSpan` parameter — reviewed and signed off, see sdd_decisions memory. Feature-branch note: RTVM/SDD/memory edits for `[RTVM-014]`-style issues belong on the issue's own `issue-<n>` branch (per `.github/AGENT_LABELS.md`'s Branch convention), not `main` — feature branches can lag `main` if other issues merged concurrently (confirmed here: `origin/issue-11`'s docs/RTVM.md was missing DATA-IN-103/CORE-201/202 status updates that `main` already had from issues #8/#10). Non-overlapping table rows merge cleanly; this long single-line Documentation-index bullet does not, so append new decisions as their own new bullet line here rather than editing the shared long line, to avoid a same-line merge conflict with whatever other concurrently-running issue also touched it.
- DATA-IN-103 (issue #8) commit SHA follow-up 2026-08-17: CI/CD merged `issue-8`→`main` as commit `15267cb` (resolved a sibling-branch conflict in `.claude/agent-memory/software-engineer/MEMORY.md` by union, unrelated to RTVM), tagged `v1.0.2` (same build-number tag as issue #11, confirmed ancestor not recreated — no release, several `type:requirement` issues still open). Recorded `15267cb` in RTVM Commit column for DATA-IN-103; edit made directly on `main` since issue-8's branch was already merged by the time this handoff arrived (no need for an `issue-8` feature branch at this point). CI/CD flagged trunk-merge regression testing needed → handed to `agent:test-engineer` per the fast path, no Product Manager notification (no release).
- Issue #11 closed out 2026-08-17: CI/CD merged `issue-11`→`main` as commit `2e107fa` (resolved a `docs/RTVM.md` conflict against concurrently-merged issue #10, kept CORE-201/202/203/204 all `Verified`), tagged `v1.0.2` (build-number bump, not a release — several `type:requirement` issues still open). Recorded `2e107fa` in RTVM Commit column for CORE-203/204. CI/CD flagged trunk-merge regression testing needed → handed to `agent:test-engineer` per the "Receiving a commit confirmation from CI/CD" fast path (no release, so no Product Manager notification this time). Test Engineer's regression pass on `main`@`e45538d` came back clean (39/39) 2026-08-17 — confirmed RTVM already current (no edit needed when commit SHA is already recorded), commented, and closed issue #11. CORE-203/CORE-204 chain fully complete.
- Issue #8 (DATA-IN-103) closed out 2026-08-17: Test Engineer's post-merge regression pass on `main`@`160bbc5` (43/43, post issue-8 merge `15267cb`) confirmed no regressions. RTVM already showed DATA-IN-103 `Verified` with commit `15267cb` recorded (matching CI/CD's SHA) — no edit needed, same "already current" pattern as issue #11. Commented confirming and closed the issue. DATA-IN-103 chain fully complete; the TP-005/TP-103 end-to-end Host-wiring caveat (unit-level only, pending UI-001/002/003 `Program.cs`) remains open until that issue lands — watch for it.
- CORE-201/CORE-202 (issue #10) commit SHA follow-up 2026-08-17: CI/CD merged `issue-10`→`main` as commit `12d6457`, which required genuine conflict resolution (not just RTVM bookkeeping) because issue #11's `TimeSpan elapsed` 3rd-arg addition to `IInstruction.Evaluate` landed on `main` after `issue-10` branched — CI/CD updated `Xic`/`Xio`/`Ote` to the 3-arg signature (`elapsed` ignored) and fixed `XicXioOteTests.cs`'s direct `.Evaluate()` calls that weren't routed through `ScanEngine`. Recorded `12d6457` in RTVM Commit column for both rows; edit made directly on `main` (issue-10's branch already merged). CI/CD flagged trunk-merge regression testing needed → handed to `agent:test-engineer` per the fast path; not a release this round (several `type:requirement` issues still open), no Product Manager notification.
