# Software Engineer — memory

## Architecture patterns

<!-- Module layout, naming conventions, and data-schema decisions that
     should stay consistent across new work. -->
- [PLC_Emulator scaffold](project_plc_emulator_scaffold.md) — PlcEmulator.sln layout, project reference graph, IDriver placement decision (issue #5)

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
