# Test Engineer — memory

## Test harness notes

<!-- Simulation frameworks, hardware-in-the-loop rigs, and how to run
     each product line's test suite locally vs. in this pipeline. -->

- [PlcEmulator .NET scaffolding: build/test verification + SDD reference-graph checklist](harness_dotnet_scaffolding.md) — how to verify issue #5-style scaffolding and later feature branches; several sibling instruction-group issues (#10, #11, #13, #14) each landed with their own baseline and merged concurrently, then #12 (CORE-205/206 counters, 11 new tests) merged on top — recount from `main` after every merge rather than trusting any one branch's quoted total; also covers the shallow-clone-hides-a-merge gotcha. Baseline drifted in parallel on two sibling branches merged the same day: 104/104 (issue #23, NFR-500 multi-controller isolation, 101 prior + 3 new) and 105/105 (issue #19, DATA-OUT-301) each counted from a `main` that hadn't yet absorbed the other — always recount fresh from `main` post-merge rather than trusting either quoted total in isolation. "RTVM already current → still route regression PASS through the two-step handoff to systems-engineer" pattern confirmed 11 times running — fully settled. Issue #23 also established: for "verification method: Inspection" RTVM rows, independently grep the specific structural claim (e.g. no static mutable fields) yourself rather than trusting the SE's inspection narrative alone. `grep -c "[TestMethod]"` under-counts actual test totals when `[DataRow]` parameterization is present — trust `dotnet test`'s own summary line.
- **2026-08-17 (issue #20, OUT-400 post-merge regression):** the runner's default clone is shallow (`git rev-parse --is-shallow-repository` → true) *every* session, not just occasionally — `git log --oneline main` showed only 2 commits until `git fetch --unshallow origin` was run, which made `git merge-base --is-ancestor <merge-sha> main` fail with "not a valid commit name" even though the merge was real. Always unshallow before trusting any ancestor/history check, not just before recounting tests. Also: CI/CD's claimed merge SHA (`40fa9203...`) turned out to be a single-parent (non-merge) commit once history was visible — not actually a `--no-ff` merge commit despite the claim — but it *was* a valid ancestor of `main` post-unshallow, so this wasn't a real discrepancy, just a misleading "merge commit" label on what was actually a fast-forwarded/rebased commit. Didn't block the PASS since the code and RTVM commit-SHA reference were both correct and verifiable. For a live TP-400-style TCP smoke test with no sample fixtures on disk, CONTROL_LOGIC/NETWORK JSON field names are: rung instructions use `"op"`/`"operands"` (array, not singular `"tag"`), and NETWORK components use `"driver"` (not `"driverType"`) plus `"tag"`/`"tags"` — confirmed against `ConfigLoader.cs`'s doc comments and `ControlLogicSchemaTests.cs`, both case-insensitive on property names.
- **2026-08-17 (issue #21, OUT-401 post-merge regression):** clean pass, no surprises — 118/118 tests, 0 build warnings, both cited merge SHAs (`861395d` no-ff merge, `68de61c` final pushed tip after a concurrent-push round) confirmed real ancestors of `main` post-unshallow, RTVM already current for both OUT-401 and the concurrently-merged OUT-402 (#22). Confirms the "RTVM already current → still route PASS through two-step handoff to systems-engineer" pattern again (now 12 times running).

## Platform-specific test considerations

<!-- What "correct" means per platform where it isn't obvious from the
     RTVM alone — e.g. VR frame-timing tolerances, glove input-latency
     budgets, jukebox audio-sync tolerances. -->

## Recurring failure patterns

<!-- Bugs or regressions that have shown up more than once, and what
     actually fixed them, so they're recognized faster next time. -->

## Flaky tests

<!-- Tests known to fail intermittently for reasons unrelated to the
     code under test, and the current best guess why. -->
