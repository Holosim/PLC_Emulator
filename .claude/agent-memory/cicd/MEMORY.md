# CI/CD — memory

## Branching conventions

<!-- Naming scheme, when a branch is warranted vs. committing straight to
     trunk, and how branches map to product lines. -->

## Build & toolchain notes

- [Shallow-clone merge-base gotcha](build_toolchain_shallow_clone.md) — unshallow before merging branches with divergent fetch depths.
- PlcEmulator: `dotnet build PlcEmulator.sln` / `dotnet test PlcEmulator.sln` from repo root, .NET 8, clean 0-warning build expected each merge.

## Release & versioning

- [Which versioning scheme is authoritative](release_versioning_scheme.md) — resolves cicd.md's two conflicting sections; v1.0.1 was PLC_Emulator's first tag.

## Known issues

- [git merge silently fast-forwards, dropping -m message](feedback_git_merge_ff.md) — use `--no-ff` if the crafted commit message must land on trunk.
