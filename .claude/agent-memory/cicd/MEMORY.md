# CI/CD — memory

## Branching conventions

<!-- Naming scheme, when a branch is warranted vs. committing straight to
     trunk, and how branches map to product lines. -->

## Build & toolchain notes

- [Shallow-clone merge-base gotcha](build_toolchain_shallow_clone.md) — unshallow before merging branches with divergent fetch depths.
- PlcEmulator: `dotnet build PlcEmulator.sln` / `dotnet test PlcEmulator.sln` from repo root, .NET 8, clean 0-warning build expected each merge.

## Release & versioning

- [Which versioning scheme is authoritative](release_versioning_scheme.md) — RESOLVED 2026-08-17: cicd.md now has one section only; BUILD = `git rev-list --count HEAD` (jumped 2 → 339, expected).
- [Same-day BUILD-number tag collision](release_versioning_tag_collision_same_day.md) — from the old date-based scheme; likely moot now BUILD is commit-count-based, but keep for history.
- [First-ever GitHub Release, v1.0.2 (issue #27)](release_first_project_release_v1_0_2.md) — `gh release create` uses an already-existing tag's stale commit as-is (`--target` ignored); hand-write release notes from RTVM.md instead of `--generate-notes` when that happens.
- [Post-release field defect reopens the same issue](post_release_field_defect_reopens_issue.md) — issue #27/DELIV-900 reopened after close+release for a client build defect; can trigger a legitimate second Release (v1.0.339) on the same requirement.

## Workflow patterns

- [Second ready-for-commit hand-off, no new merge](second_ready_for_commit_no_new_merge.md) — happens after post-merge regression loop closes; check `git log origin/main..origin/issue-N` before assuming there's a branch to merge.

## Known issues

- [git merge silently fast-forwards, dropping -m message](feedback_git_merge_ff.md) — use `--no-ff` if the crafted commit message must land on trunk.
- [Merge can surface a real build break with no text conflict](merge_required_member_break.md) — always rebuild/retest after resolving conflict markers, not just after they're gone (issue #7: `required` member; issue #10: `IInstruction.Evaluate` gained a 3rd param on a sibling branch, broke a same-side-only test file with zero conflict markers).
- [Concurrent CI/CD runs push to main same day](concurrent_cicd_runs_same_day.md) — expect rejected pushes when multiple issues are being merged in parallel; fetch+merge+retest+push again (issue #7, concurrent with issue #9's finalization).
- [RTVM.md merge conflict from parallel sibling branches](rtvm_merge_conflict_parallel_verification.md) — union both sides for independent parallel edits; but pick the fuller side (no union) if it's the same bullet as a sequential continuation, not parallel content (issue #30).
