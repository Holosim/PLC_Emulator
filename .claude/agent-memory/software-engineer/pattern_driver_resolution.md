---
name: pattern-driver-resolution
description: How CORE-209's IDriver wiring resolves driver-type-name -> concrete instance across the Core/Drivers project boundary (issue #15)
metadata:
  type: project
---

`PlcEmulator.Core` cannot reference `PlcEmulator.Drivers` (dependency
direction per docs/SDD.md), but `PlcController` (in `Core`) must turn
each `NetworkComponentConfig.DriverType` string into a concrete
`IDriver` instance. Resolved with a delegate declared in `Core`,
implemented in `Drivers`, wired by `Host`:

- `PlcEmulator.Core.Drivers.DriverResolver` — `delegate IDriver
  DriverResolver(string driverType)`, next to `IDriver` in `Core`.
- `PlcController`'s constructor takes a third param, `DriverResolver
  driverFactory`, and does the per-`NetworkDef.Components` loop itself
  (resolve, then `driver.Bind(tags, component)`) — `Core` still owns
  the iteration/binding orchestration; it just doesn't know the
  concrete types.
- `PlcEmulator.Drivers.DriverFactory` (leaf project, static class,
  mirrors `Core.Instructions.InstructionFactory`'s mnemonic-switch
  pattern) is the concrete resolver: `"DiscreteSensor"` ->
  `DiscreteSensorDriver`, `"Relay"` -> `RelayDriver`, else throws
  `ConfigValidationException`.
- Host (once UI-001/#12 lands) is expected to pass
  `PlcEmulator.Drivers.DriverFactory.Create` as the `driverFactory` arg
  — not yet wired, `Program.cs` is still scaffolding.

`DiscreteSensorDriver`/`RelayDriver` share `PlcEmulator.Drivers.SingleTagDriverBase`:
validates exactly one tag binding, resolves it via `TagTable.TryGet`,
requires `TagType.Bool`, guards `OnScanComplete` against being called
before `Bind`. Both drivers are currently behaviorally identical
(no-op `OnScanComplete`) — v1.0 has no external device simulation
(that's OUT-400/401, not yet built) to give a sensor driver and a
relay driver actually different runtime behavior yet. Don't invent
divergent behavior for them without a stated requirement; extend
`SingleTagDriverBase.OnScanComplete` (virtual) when one exists.

**Why:** keeps "add a driver type without touching the Scan Engine or
instruction classes" (CORE-209/TP-209) literally true — adding a
driver only ever touches `PlcEmulator.Drivers` (new class + one
`DriverFactory.Create` case), never `Core`.

**How to apply:** reuse this exact resolver-delegate pattern for any
future `Core`-defined extension point whose concrete implementations
must live in a leaf project `Core` can't reference. Flagged to SE:
`docs/SDD.md`'s `NetworkComponentConfig.DriverType` doc comment says
resolution happens "by the Host at PlcController construction time" —
implemented as Host supplying a resolver *function*, not Host
pre-building the whole driver array itself; noted as non-blocking in
the issue #15 hand-off in case SDD's Coding Standards should spell
this out explicitly.
