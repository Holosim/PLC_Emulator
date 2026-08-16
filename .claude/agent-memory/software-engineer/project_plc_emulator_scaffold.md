---
name: project-plc-emulator-scaffold
description: PLC_Emulator repo — solution layout, project reference graph, and the two open decisions raised on the Generate Code Base issue (#5)
metadata:
  type: project
---

Scaffolded on issue #5 (branch `issue-5`): `PlcEmulator.sln` at repo
root, SDK-style `.csproj` per namespace root, .NET 8 (LTS) pinned via
`global.json`. Layout: `src/PlcEmulator.{Host,Config,Core,Drivers,Network}`,
`tests/PlcEmulator.Tests` (MSTest).

**Project reference graph** (Config is the only leaf):
`Config` ← `Core` ← `Drivers`, `Core` ← `Network`, all four ← `Host`.
`Core` depends on neither `Network` nor `Drivers` — this satisfies the
SDD's explicit "Core does not depend on Network" and avoids a
circular Core↔Drivers reference.

**Why `IDriver` lives in `PlcEmulator.Core` (namespace
`PlcEmulator.Core.Drivers`), not in `PlcEmulator.Drivers`:**
`PlcController` (in Core) holds `IDriver[]` instances, and any driver
implementation needs `TagTable` (also in Core) — so if the interface
lived in `Drivers`, `Core` and `Drivers` would need to reference each
other, which .NET can't build. Standard dependency inversion: the
consumer (Core) owns the interface, the producer (Drivers project,
namespace `PlcEmulator.Drivers`) implements it. Flagged to Systems
Engineer for `docs/SDD.md` Coding Standards sign-off — that bullet
currently reads "`PlcEmulator.Drivers` (`IDriver` + built-in drivers)"
as if the interface itself lived there too.

**Why no workflow files ended up in `.github/workflows/` on this
issue:** pushing there was rejected — `refusing to allow a GitHub App
to create or update workflow ... without 'workflows' permission`.
This confirms `docs/RTVM.md` §9.1.4's claim empirically, for *any*
agent role, not just a hypothetical. Staged both the customized
`docs/ci/windows-verification.yml` (paths filter + `SOLUTION:
PlcEmulator.sln` customized in place) and a new
`docs/ci/build-and-test.yml` (the NFR-501 ubuntu-latest/windows-latest
`dotnet build`+`dotnet test` matrix) under `docs/ci/` instead, same
convention as the original template file. A human (or a
`workflows`-scoped token) still needs to copy both into
`.github/workflows/` before either runs — see [[workflows-permission-blocker]].

Test project uses **MSTest** (`dotnet new mstest`), reasoned as
Microsoft's own first-party test framework rather than a third-party
NuGet dependency, matching NFR-502's "no third-party dependencies by
default" as closely as any .NET test tooling can (all test frameworks
require *some* NuGet package — there's no in-box `dotnet test`
framework). Flagged for Systems Engineer to confirm this reading is
acceptable, not just assumed.
