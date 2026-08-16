# Software Engineer — memory

## Architecture patterns

<!-- Module layout, naming conventions, and data-schema decisions that
     should stay consistent across new work. -->
- [PLC_Emulator scaffold](project_plc_emulator_scaffold.md) — PlcEmulator.sln layout, project reference graph, IDriver placement decision (issue #5)
- [CONTROL_LOGIC parsing pattern](pattern_control_logic_parsing.md) — Config-generic/Core-specific DTO split; reuse for NETWORK schema (issue #6)
- [Scan engine rung power-flow](pattern_scan_engine_rung_power_flow.md) — IInstruction.Evaluate(tags, rungState) contract for #10-#14's instruction semantics; flagged to SE for SDD.md update (issue #9)
- [NETWORK JSON schema](schema_network_json.md) — wire shape + DTO/domain-model split for DATA-IN-102 (issue #7); reuse for CONTROL_LOGIC and DATA-IN-103
- [Timer elapsed-time pattern](pattern_timer_elapsed_time.md) — IInstruction.Evaluate gained `TimeSpan elapsed`, measured by ScanEngine's own Stopwatch (not per-instruction state), for TON/TOF (issue #11); reuse for any future controller-owned clock state

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
