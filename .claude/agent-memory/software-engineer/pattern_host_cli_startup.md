---
name: pattern_host_cli_startup
description: Program.cs composition-root wiring for UI-001/UI-003 CLI startup, and the known TcpJsonServer.Start stub gap it exposed
metadata:
  type: project
---

**What (issue #16):** `PlcEmulator.Host/Program.cs` now does real work:
manual `--key value` CLI parsing (no third-party arg-parsing lib, per
NFR-502), required-arg check (`--control-logic`, `--network`) with the
exact `Missing required argument: --xxx` stderr text TP-002 checks
for, optional `--port` (default `5000` — SDD/RTVM don't specify a
default; picked because TP-001's example command omits `--port`
entirely, so *some* default is required for the happy path to make
sense; flagged as an assumption in code comments and the issue #16
hand-off rather than silently baked in), then
`ConfigLoader.LoadControlLogic` → `LoadNetwork` → `Validate` →
`new PlcController(..., DriverFactory.Create)`, all inside one
try/catch on `ConfigValidationException` so any failure anywhere in
that chain reports a descriptive stderr message and exits 1 *before*
constructing `TcpJsonServer` at all (UI-003's "no partially-started
state, no TCP listener starts").

**Known gap this exposed:** `TcpJsonServer.Start` is still
`throw new NotImplementedException(...)` (OUT-400, issue #20 — which
correctly depends on #16, not the other way around, confirmed by
reading #20's own issue body). So today, the full TP-001 happy path
(`plcemu --control-logic ... --network ...` with valid files) prints
the successful-load line, then exits 1 with `plcemu: failed to start
TCP listener on port 5000: TcpJsonServer.Start is scaffolding only.`
— this is *expected* given the dependency ordering, not a bug in #16.
TP-001 will only fully pass once #20 lands. [[pattern_driver_resolution]]
covers the DriverFactory/DriverResolver wiring this reuses.

**Reusable shape for future Host changes:** two separate try/catch
blocks — one around config-load-through-controller-construction
(`ConfigValidationException` → fail-fast, no listener), one around
network-layer startup (`Exception` → same reporting style, since a
listener-start failure isn't itself a UI-003 validation error but
should still fail the same clean way). After a successful `Start`,
`Main` blocks forever (`Thread.Sleep(Timeout.Infinite)`) since plcemu
is a long-running server, not a one-shot command — this blocking-main
approach can be revisited once OUT-400 defines whether `Start` itself
blocks.
