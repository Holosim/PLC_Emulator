---
name: pattern_inspection_only_issue
description: How to handle an RTVM issue whose verification method is Inspection with no expected code change (e.g. NFR-502 dependency review, issue #25)
metadata:
  type: project
---

Some `[RTVM-...]` issues (verification method "Inspection" in
`docs/RTVM.md`, test procedure "N/A (design review)" in the Test
Procedures section) don't ask for new code — they ask SE to confirm
the existing codebase still complies with a design constraint already
"Approved" in the SDD. NFR-502 (issue #25, third-party dependency
policy) was the first concrete example: reviewed every `.csproj` for
`<PackageReference>` entries, confirmed zero in any runtime/core
project (`Config`/`Core`/`Drivers`/`Network`/`Host`), confirmed
`System.Text.Json` is exempt per `docs/SDD.md` (it's SDK-provided, not
third-party), confirmed the only `PackageReference`s anywhere are
test-only tooling in `PlcEmulator.Tests.csproj` (out of policy scope
since not reachable from core logic), and confirmed `dotnet build`
still succeeds.

**How to apply:** still create/push the `issue-<n>` branch per
convention even when there's no source diff — Test Engineer/CI/CD
expect that branch to exist and be checkable-out regardless of role.
Document the review findings in the hand-off comment in enough detail
that Test Engineer can re-verify by inspection without re-deriving the
whole review (list every file checked, not just the conclusion). Hand
off to `agent:test-engineer` with `status:ready-for-test` exactly like
a normal implemented feature — Inspection is still a verification
method that needs Test Engineer sign-off before Systems Engineer can
flip the RTVM status to `Verified`.

**Also applies when RTVM lists the verification method as "Test", not
just "Inspection"**, if the requirement turns out to already hold by
design review alone: NFR-503 (issue #26, no-persistence-across-restart)
is `docs/RTVM.md`-labeled "Test" with a concrete TP-503 procedure, but
review found zero persistence code anywhere in `src/` (no
`PackageReference` for storage, no `File.Write`/database calls,
`Program.Main` already builds a fresh `PlcController` every launch,
`ControlLogicBuilder.CreateTag` already builds new `Tag` instances from
`TagDef.InitialValue` every call) — so the fix was already "in" by
construction. Unlike a pure-Inspection issue, still add the concrete
unit-test artifact the TP calls for (see
[[pattern_multi_controller_isolation_review]]'s
`MultiControllerIsolationTests` for the precedent shape): construct one
config object, mutate/scan a first controller instance, then build a
*second* controller from the *same* config object (simulating the
restart) and assert the fresh instance shows initial values, not the
first instance's mutated ones. Reusing the same config object across
both instances is the strongest form of the check — it would catch an
accidental cache keyed off the shared config, which two independently
re-parsed config objects would not expose.
