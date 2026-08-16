# CI/CD — memory

## Branching conventions

<!-- Naming scheme, when a branch is warranted vs. committing straight to
     trunk, and how branches map to product lines. -->

## Build & toolchain notes

- [Shallow-clone merge-base gotcha](build_toolchain_shallow_clone.md) — unshallow before merging branches with divergent fetch depths.
- PlcEmulator: `dotnet build PlcEmulator.sln` / `dotnet test PlcEmulator.sln` from repo root, .NET 8, clean 0-warning build expected each merge.

## Release & versioning

- [Which versioning scheme is authoritative](release_versioning_scheme.md) — resolves cicd.md's two conflicting sections; v1.0.1 was PLC_Emulator's first tag.
- [Same-day BUILD-number tag collision](release_versioning_tag_collision_same_day.md) — don't force-move an existing tag; verify ancestry and skip re-tagging if the day hasn't advanced.

## Known issues

- [git merge silently fast-forwards, dropping -m message](feedback_git_merge_ff.md) — use `--no-ff` if the crafted commit message must land on trunk.
- [Merge can surface a real build break with no text conflict](merge_required_member_break.md) — always rebuild/retest after resolving conflict markers, not just after they're gone (issue #7: `required` member added on one branch broke a call site untouched on the other).
- [Concurrent CI/CD runs push to main same day](concurrent_cicd_runs_same_day.md) — expect rejected pushes when multiple issues are being merged in parallel; fetch+merge+retest+push again (issue #7, concurrent with issue #9's finalization).
