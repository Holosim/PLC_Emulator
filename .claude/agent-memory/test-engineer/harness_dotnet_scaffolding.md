---
name: harness-dotnet-scaffolding
description: How to verify the PlcEmulator .NET solution builds/tests clean, matches SDD's project reference graph, and where TP-1xx procedures live
metadata:
  type: project
---

The PLC_Emulator repo (`PlcEmulator.sln`) is a .NET 8 solution (pinned via
`global.json`, SDK 8.0.100, `rollForward: latestFeature`) with one
SDK-style `.csproj` per namespace root under `src/` (`.Config`, `.Core`,
`.Drivers`, `.Network`, `.Host`) plus `tests/PlcEmulator.Tests` (MSTest).

**How to verify a build/test pass from scratch:**
```
find . -name bin -o -name obj | xargs rm -rf
dotnet build PlcEmulator.sln
dotnet test PlcEmulator.sln
```
Expect 0 warnings/0 errors on build; test count grows as features land
(was 1/1 — a scaffolding smoke test — as of issue #5; 11/11 as of issue
#6, DATA-IN-100/101).

**SDD dependency direction to check on every scaffolding-adjacent
issue** (grep `ProjectReference` in each `src/*/*.csproj`): `Config` is
a leaf; `Core → Config`; `Drivers → Core, Config`; `Network → Core`;
`Host → all four`. `Core` must never reference `Network` or `Drivers`.
`IDriver` lives in `src/PlcEmulator.Core/Drivers/IDriver.cs` (namespace
`PlcEmulator.Core.Drivers`) — interface next to its consumer
(`PlcController`/`TagTable`), not in `PlcEmulator.Drivers` — this was
an explicit, confirmed architecture decision (see issue #5 thread), not
a mistake to flag.

**NFR-502 (no third-party NuGet in src/):** `grep -rn PackageReference
src/*/*.csproj` should return nothing; only `tests/PlcEmulator.Tests`
should reference MSTest/coverlet packages.

**Why:** Windows/Visual Studio verification (NFR-501, DELIV-900) is a
one-time, late-stage consolidation step per client decision recorded on
issue #5 — not a per-feature CI matrix. `docs/ci/windows-verification.yml`
and `docs/ci/build-and-test.yml` are meant to stay staged, undeployed,
under `docs/ci/` (not copied into `.github/workflows/`) for the
duration of feature development. If a future issue's test procedure
seems to call for Windows-runner verification before the late-stage
consolidation issues (Implementation Plan #24/#27), that's a red flag —
check `docs/RTVM.md` TP-501/TP-900 wording first before assuming it's
required now.

**RTVM test procedures (TP-1xx):** live in `docs/RTVM.md`'s "Test
Procedures" table, keyed by ID and cross-referenced from the requirement
rows above it (e.g. DATA-IN-100 → TP-100). The Software Engineer's
dedicated test methods for a given TP- item are usually named
`TpNNN_...` in `tests/PlcEmulator.Tests/` — cheap way to locate exactly
which test implements which procedure, but always read the test body
against the RTVM row's expected-result text rather than trusting the
method name alone. Confirmed on issue #6 (2026-08-16): `Tp100_...`/
`Tp101_...` test names matched TP-100/TP-101 exactly, line-by-line, no
drift.

**How to apply:** Use this checklist for any issue that touches
scaffolding, adds a new `src/*` project, or changes project references —
not just issue #5 itself. For all issues, start test verification by
grepping `tests/PlcEmulator.Tests/` for the TP-NNN number(s) named in
the requirement's RTVM row, then confirm build/test clean from a fresh
`bin`/`obj` wipe.

**Also doubles as the post-merge regression checklist:** when CI/CD
hands back a trunk-merge regression request (issue #6, 2026-08-16), the
same steps apply against `main` instead of the feature branch: fresh
`bin`/`obj` wipe + build/test, re-check `NFR-502` (no `PackageReference`
in `src/*/*.csproj`) and the `ProjectReference` graph, confirm
`docs/RTVM.md`'s `Verified` row(s) carry the real merge commit SHA, and
confirm no stray SDD lock markers / dirty `git status` were left behind
by the merge. No separate regression-specific procedure exists — RTVM
TP-1xx + this checklist is the whole regression suite at this project
stage.
