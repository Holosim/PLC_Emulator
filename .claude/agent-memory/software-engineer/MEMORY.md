# Software Engineer — memory

## Architecture patterns

<!-- Module layout, naming conventions, and data-schema decisions that
     should stay consistent across new work. -->
- [PLC_Emulator scaffold](project_plc_emulator_scaffold.md) — PlcEmulator.sln layout, project reference graph, IDriver placement decision (issue #5)
- [CONTROL_LOGIC parsing pattern](pattern_control_logic_parsing.md) — Config-generic/Core-specific DTO split; reuse for NETWORK schema (issue #6)
- [Scan engine rung power-flow](pattern_scan_engine_rung_power_flow.md) — IInstruction.Evaluate(tags, rungState) contract; XIC/XIO/OTE (CORE-201/202) landed as reference impl (issue #10), pattern still governs #11-#14
- [NETWORK JSON schema](schema_network_json.md) — wire shape + DTO/domain-model split for DATA-IN-102 (issue #7); reuse for CONTROL_LOGIC and DATA-IN-103
- [TagSnapshot/GetSnapshot scope](pattern_tag_snapshot_scope.md) — scalar-tags-only, no timer/counter sub-elements; reuse for DATA-OUT-301/OUT-401 (issue #18)
- [Timer elapsed-time pattern](pattern_timer_elapsed_time.md) — IInstruction.Evaluate gained `TimeSpan elapsed`, measured by ScanEngine's own Stopwatch (not per-instruction state), for TON/TOF (issue #11); reuse for any future controller-owned clock state
- [ConfigLoader.Validate cross-file check](pattern_config_loader_validate.md) — DATA-IN-103 (issue #8): per-tag-per-component check against ControlLogicDef.Tags; driver-type resolution deliberately deferred to Core
- [CompareInstruction numeric matching](pattern_compare_instruction_numeric_matching.md) — CORE-207 template-method design + confirmed "matching numeric type" interpretation call (issue #13); reused/confirmed for CORE-208 math instructions
- [Fault-flag mechanism](pattern_fault_flag.md) — `Tag.Fault` (nullable string), set/cleared by the instruction owning the destination tag, for CORE-208-class "defined runtime error, not a crash" requirements (issue #14)
- [Driver resolution across Core/Drivers boundary](pattern_driver_resolution.md) — DriverResolver delegate in Core, DriverFactory impl in Drivers, Host wires them (CORE-209, issue #15)
- [Counter edge-detection gap](pattern_counter_edge_detection.md) — CTU/CTD rising-edge state needed CounterState.Cu/Cd fields beyond the documented 3-field schema; flagged for SE sign-off (issue #12)
- [Host CLI startup wiring](pattern_host_cli_startup.md) — Program.cs UI-001/003 implementation, default `--port 5000` assumption, and the known TcpJsonServer.Start (OUT-400/#20) stub gap that keeps TP-001 from fully passing yet (issue #16)

## Platform-specific notes

<!-- Firmware and SDK quirks or constraints, grouped by target. Add a
     subsection per platform as real work starts on it:
     - VR HMD gaming interaction
     - Gesture-tracking gloves (embedded audio)
     - Video jukebox player / controller -->
- [.github/workflows/ push rejected](workflows_permission_blocker.md) — stage new/changed CI workflow YAML under docs/ci/ instead, confirmed empirically

## Reusable solutions

<!-- Algorithms or components already solved well enough to reuse rather
     than re-derive — what it does, where it lives, what it assumes. -->

## Coding standards

<!-- Pointer to the Systems Engineer's standards doc, plus any
     clarifications this role has had to make in practice. -->
- `docs/SDD.md`'s "Coding Standards" section (namespaces/project
  layout, naming, dependency direction) is authoritative — see its
  Component Architecture diagram for the intended project-reference
  graph (`Core` depends on neither `Network` nor `Drivers`).

## Git / tooling gotchas

- [Shallow-clone false "unrelated histories"](gotcha_shallow_clone_merge.md) — unshallow-fetch before assuming trunk was reset when a merge is refused.
- [Trunk lags a closed dependency](gotcha_trunk_lag_behind_dependency.md) — a closed dependency issue's code may not actually be on `main` yet (CI/CD merge never triggered); check before branching from `main` blind (issue #6).
- [Base branch is issue-5, not main (temporary)](gotcha_base_branch_not_main.md) — main doesn't have the scaffolding merged yet as of issue #7; branch off origin/issue-5 until CI/CD merges it.
- [Concurrent instruction-group issues](gotcha_concurrent_instruction_issues.md) — issues #10-#14 run as parallel SE hand-offs sharing `Instructions/`; stay strictly scoped to your own issue's files (issue #14).
