# Requirements Traceability & Verification Matrix (RTVM)

<!--
Owned by the Systems Engineer. Don't enter line items against a
[PROPOSED] item in docs/PROJECT_DEFINITION.md — wait for it to become
[CONFIRMED]. See systems-engineer.md for the escalation and handoff
rules this document participates in.
-->

## ID scheme

The category blocks below are a starting point — adjust them to fit
the project, not a fixed requirement:

| Category | Prefix | Range |
| --- | --- | --- |
| UI | UI | 001–099 |
| Data in | DATA-IN | 100–199 |
| Core algorithm / processing | CORE | 200–299 |
| Data out | DATA-OUT | 300–399 |
| Output | OUT | 400–499 |
| Non-functional | NFR | 500–599 |
| Deliverable | DELIV | 900–999 |

Companion schemes: `SN-<n>` for stakeholder needs (defined in
`docs/PROJECT_DEFINITION.md`), `TP-<nnn>` for test procedures.

For this project: `UI` = the CLI operator/engineer-in-training
surface (startup args, diagnostics, exit behavior) — there is no
GUI in v1.0. `DATA-IN` = the CONTROL_LOGIC/NETWORK JSON schemas and
their parse/validate step. `CORE` = the scan-cycle execution engine
and instruction set. `DATA-OUT` = the internal runtime state model.
`OUT` = the TCP/JSON interface to the external simulation engine.

## Verification vocabulary

Test / Demonstration / Analysis / Inspection. `DELIV` items are
typically verified by inspection and specified in `docs/SDD.md`'s
build/toolchain conventions rather than by a runtime test.

## Status vocabulary

Draft → Approved → In Implementation → In Test → Verified, plus
Blocked / Withdrawn.

## Assumptions made while breaking down scope

`docs/PROJECT_DEFINITION.md` is fully confirmed, but a few
implementation-level details below it aren't spelled out. These are
standard PLC/ladder-logic conventions, not scope questions, so they
were resolved here rather than escalated — flagged plainly in case the
client's intent differs:

- **Counter reset (RES).** The project definition lists CTU/CTD but
  doesn't mention a reset instruction. A CTU/CTD counter is not
  functionally complete without one (real GuardLogix ladder logic
  always pairs them with `RES`), so `RES` is included as a companion
  instruction under CORE-206, not a separate MVP feature.
- **Compare/math instruction set.** "Basic compare/math" is
  interpreted as the standard Rockwell mnemonics: `EQU, NEQ, GRT, LES,
  GEQ, LEQ` for compare and `ADD, SUB, MUL, DIV` for math (CORE-207,
  CORE-208).
- **Tag data types.** The tag-based data model needs at least `BOOL`
  (discrete I/O, contacts/coils), `DINT` (counters, integer math), and
  `REAL` (basic math) to support the listed instruction set (DATA-IN-100).
- **TCP port.** No specific port was specified by the client;
  OUT-400 treats it as operator-configurable (e.g. a CLI argument),
  not a hardcoded value.
- **NETWORK JSON wire shape (DATA-IN-102).** Confirmed as proposed by
  Software Engineer on issue #7: a top-level `{"components": [...]}`
  object (not a bare top-level array), for consistency with the
  expected CONTROL_LOGIC shape and future extensibility (e.g. adding
  metadata alongside `components` later without a breaking schema
  change). Each component accepts a singular `"tag"` string and/or a
  plural `"tags"` array, merged into one ordered list — both forms are
  valid NETWORK JSON, not just the singular form used in the RTVM's
  own TP-102 example. Binding, not the wrapper shape or key name, is
  what DATA-IN-102 cares about.
- **CORE-207/CORE-208 "matching numeric type."** Confirmed as
  implemented on issue #13: "numeric type" means either operand
  resolves to a number — i.e. neither operand is a non-numeric
  `BOOL`/`TIMER`/`COUNTER` tag — not that two tag operands must share
  an identical declared `DINT`/`REAL` type. A `DINT` tag compared (or
  combined, for CORE-208) against a `REAL` tag is valid; both are
  promoted to `double` for the operation. This matches standard
  Rockwell/RSLogix compare/math instruction behavior and applies
  identically to CORE-208's `ADD`/`SUB`/`MUL`/`DIV` operand resolution
  (same tag-or-literal operand shape) — no separate confirmation
  needed when CORE-208 is implemented.
- **UI-001/UI-003 CLI wiring (issue #16).** `--port` is an optional
  CLI argument defaulting to `5000` when omitted (consistent with the
  existing "TCP port" assumption above — operator-configurable, not
  hardcoded); TP-001's example command omits `--port` entirely so a
  default was needed for the happy path to make sense. UI-001/UI-003
  are marked Verified on the strength of TP-002/TP-004 (fully passing,
  no caveats) and the argument-parsing/load/validate portion of
  TP-001; TP-001's final clause ("begins listening on the configured
  TCP port") cannot be exercised until `TcpJsonServer.Start` has a
  real implementation (OUT-400, issue #20, which correctly declares
  `Finish-Start: #16`) — re-verify that clause once #20 lands.
  **Resolved 2026-08-17 (issue #20):** Test Engineer's TP-400 pass
  drove the real listener end-to-end (process launched with
  `--port 5050`, client connects and receives the initial snapshot),
  which is exactly TP-001's remaining clause. No further re-verification
  needed; UI-001/UI-003 rows are fully covered as-is.
- **DELIV-900 field defect (issue #27, 2026-08-17 client report).**
  `global.json` pinned `"version": "8.0.100"` with
  `"rollForward": "latestFeature"`. `latestFeature` only rolls forward
  within the same major (8.x); a client workstation with only
  `9.0.317`/`10.0.303` installed (no `8.x`) gets `NETSDK1141` in Visual
  Studio for every project. Reproduced directly (SE/Systems Engineer,
  2026-08-17): with only a `9.0.316` SDK visible and the original
  `global.json`, `dotnet --version` fails with the identical
  "Requested SDK version: 8.0.100 ... Installed SDKs: 9.0.316" error;
  changing only `rollForward` to `"latestMajor"` (leaving
  `"version": "8.0.100"` as the floor) resolves cleanly to `9.0.316`
  with no other change needed. Fix: `global.json`'s `rollForward` →
  `"latestMajor"`. TP-900/DELIV-900 updated above to test this
  explicitly. Handed to Software Engineer to apply the one-line fix
  and confirm a full build/test pass under a non-`8.x`-only SDK
  environment before Test Engineer re-verifies.
  **Resolved 2026-08-17 (issue #27, commit `98c6485`).** Software
  Engineer applied the one-line `rollForward: "latestMajor"` fix;
  both Software Engineer and Test Engineer independently reproduced
  the exact field failure in an isolated single-SDK (`9.0.316`-only)
  environment, confirmed the fix resolves it, and confirmed no
  regression in the normal multi-SDK build (119/119 tests both
  scenarios). Runner-specific-assumption scan (both passes) found
  nothing else in the build that assumes CI's pre-provisioned SDK
  state — the only reason this class of defect never surfaced in CI
  is that `build-and-test.yml` pre-provisions `8.0.x` via
  `setup-dotnet` before every run, which a clean client workstation
  has no equivalent of. DELIV-900/TP-900 flipped back to `Verified`.
- **DELIV-901 added (issue #28, 2026-08-17 client request).** Client
  asked for a user quick-start guide now that the solution builds and
  runs (post-DELIV-900). Product Manager captured this in
  `docs/PROJECT_DEFINITION.md`'s Deliverable Requirements as a new
  non-functional deliverable, not a feature — no runtime behavior for
  the RTVM to exercise, just a document (`docs/USER_GUIDE.md`) to
  produce and verify for completeness/accuracy against a concrete
  acceptance bar ("clone to running simulation using only the guide").
  Modeled directly on the DELIV-900/TP-900 precedent above. Sequenced
  as a late-stage v1.0 task, same category as DELIV-900, depending on
  it (Finish-Start) since the guide documents the delivered state of
  the VS solution and would go stale if written before that
  consolidation landed.
- **`docs/ci/windows-verification.yml` — not recreated.** The client's
  2026-08-17 comment states this file "has been added to the workflows
  in Github," but it does not exist on `main`, in `docs/ci/`, in
  `.github/workflows/`, or on any branch in this repo's history (last
  touched at commit `c8a1837`, which *removed* it as C++/MSBuild
  scaffolding with no role in a .NET project — see
  `workflows_permission_resolution_plc_emulator.md`). Treating this as
  the client referring to `build-and-test.yml`'s existing
  `windows-latest` CI leg (which does exist and already satisfies
  NFR-501/TP-501) rather than a literal request to recreate the
  deleted file; not acted on. Flag to Product Manager/client for
  clarification if a literal `windows-verification.yml` is actually
  wanted.
- **OUT-403 added (issue #29, 2026-08-17).** While verifying DELIV-901's
  guide against a live `plcemu` process (not a test harness), Software
  Engineer found there is no free-running background scan loop
  anywhere in the Host: `Program.cs` starts `TcpJsonServer` and then
  blocks forever (`Thread.Sleep(Timeout.Infinite)`); nothing ever
  calls `PlcController.RunScan()` on its own. A `tag_write` is queued
  correctly but never applied/broadcast in a live process — this had
  been flagged three times before (issues #21, #22, #23) as "not
  blocking, no owning RTVM item," because every test procedure through
  OUT-402 explicitly drove `RunScan()` itself the same way the unit
  tests do. TP-901 is the first procedure that requires a genuinely
  live demonstration, so the gap is no longer harmless.
  Resolving this as a plain requirements gap, not a scope question for
  the Solutions Architect, because scope was already decided:
  `docs/PROJECT_DEFINITION.md`'s MVP definition explicitly requires
  the TCP/JSON interface to "exchange I/O state in real time," and
  `docs/SDD.md`'s own ICD already documents `tag_update` being sent
  "again after every scan cycle completes" — i.e. a continuous loop was
  always the intended design, just never wired up in `Program.cs`. The
  cadence question SE also raised is likewise already answered by
  existing SDD text: CORE-203/204's `.ACC` timing was deliberately
  built around the Scan Engine measuring its own elapsed wall-clock
  time *because* "v1.0 does not define a fixed scan period" — so the
  new loop is free-running (calls `RunScan()` back-to-back, no
  artificial delay/sleep between scans, matching how a real PLC scans
  as fast as it can), not a new cadence policy invented here. Added
  **OUT-403** (new RTVM item, `docs/SDD.md` Architecture section
  updated to name Host as the owner of this loop — not `TcpJsonServer`,
  correcting a misleading comment in `Program.cs` that implied
  otherwise) with **TP-403**. DELIV-901 (#29) depends on it
  (Finish-Start) since TP-901 step 4 cannot be demonstrated against a
  live process until it lands; tracked as a separate issue (#30) rather
  than folded into #29 so each commit still traces to one RTVM ID.

## Requirements

| Req ID | Requirement | Stakeholder Need(s) | Verification Method | Status | Commit(s) |
| --- | --- | --- | --- | --- | --- |
| UI-001 | CLI server accepts startup arguments specifying the path to a CONTROL_LOGIC JSON file and a NETWORK JSON file, and loads both before beginning execution. | SN-1, SN-2 | Test | Verified | fa26c47 |
| UI-002 | CLI server prints structured startup diagnostics to console/log: number of tags loaded, number of network components loaded, and a per-tag/per-component summary. | SN-2 | Test | Verified | `148648d` |
| UI-003 | CLI server fails fast on invalid startup input: if required arguments are missing, or a CONTROL_LOGIC/NETWORK file is malformed JSON or fails schema/cross-reference validation, the process exits with a non-zero code and a descriptive error identifying the file and problem, without starting the TCP listener. | SN-2 | Test | Verified | fa26c47 |
| DATA-IN-100 | CONTROL_LOGIC JSON schema defines a tag-based data model: named tags with a type (`BOOL`, `DINT`, `REAL`) and initial value, plus structured tag types for timers (`.PRE`, `.ACC`, `.DN`, `.EN`) and counters (`.PRE`, `.ACC`, `.DN`). | SN-1, SN-3 | Test | Verified | b0ebb72 |
| DATA-IN-101 | CONTROL_LOGIC JSON schema defines ladder rungs as an ordered list of instructions drawn from the MVP instruction set: contacts (`XIC`, `XIO`), coil (`OTE`), timers (`TON`, `TOF`), counters (`CTU`, `CTD`, `RES`), compare (`EQU`, `NEQ`, `GRT`, `LES`, `GEQ`, `LEQ`), and math (`ADD`, `SUB`, `MUL`, `DIV`). | SN-1 | Test | Verified | b0ebb72 |
| DATA-IN-102 | NETWORK JSON schema defines a set of control-network components (e.g. relay, discrete sensor), each with a name, a driver-type reference, and a binding to one or more CONTROL_LOGIC tags — with no PLC logic embedded in the network definition itself. | SN-3, SN-4 | Test | Verified | 5e2402a |
| DATA-IN-103 | Server parses and validates CONTROL_LOGIC and NETWORK JSON at startup into an internal in-memory model, including cross-file validation (every NETWORK component's tag binding must reference a tag that exists in CONTROL_LOGIC), rejecting invalid input with a descriptive error. | SN-1, SN-3 | Test | Verified | `15267cb` |
| CORE-200 | Scan-cycle execution engine evaluates all ladder rungs in program order once per scan, updating tag values from rung logic before the next scan begins. | SN-1 | Test | Verified | 49d5150 |
| CORE-201 | `XIC`/`XIO` contact instructions evaluate against a `BOOL` tag's current value: `XIC` is true when the tag is true, `XIO` is true when the tag is false. | SN-1 | Test | Verified | `12d6457` |
| CORE-202 | `OTE` coil instruction sets its `BOOL` output tag equal to the evaluated (non-latching) logic of the rung each scan. | SN-1 | Test | Verified | `12d6457` |
| CORE-203 | `TON` (timer-on-delay): while enabled, `.ACC` accumulates elapsed time; `.DN` becomes true once `.ACC >= .PRE`; disabling the instruction resets `.ACC` to 0 and `.DN` to false. | SN-1 | Test | Verified | `2e107fa` |
| CORE-204 | `TOF` (timer-off-delay): `.DN` is true immediately while enabled; on disable, `.DN` remains true until `.PRE` has elapsed since disable, then goes false. | SN-1 | Test | Verified | `2e107fa` |
| CORE-205 | `CTU` (count-up): `.ACC` increments by 1 on each rising edge of the instruction's enable input; `.DN` becomes true once `.ACC >= .PRE` (and remains true if counting continues past preset). | SN-1 | Test | Verified | `32d86b4` |
| CORE-206 | `CTD` (count-down) decrements `.ACC` by 1 on each rising edge of its enable input, with `.DN` true when `.ACC <= 0`; `RES` resets a counter's `.ACC` to 0 and `.DN` to false when executed. | SN-1 | Test | Verified | `32d86b4` |
| CORE-207 | Compare instructions (`EQU`, `NEQ`, `GRT`, `LES`, `GEQ`, `LEQ`) evaluate two operands (tag or literal) of matching numeric type (see note above: either operand numeric, `DINT`/`REAL` tags may mix with numeric promotion) and produce a boolean rung-true/false result. | SN-1 | Test | Verified | `6dfb295` |
| CORE-208 | Math instructions (`ADD`, `SUB`, `MUL`, `DIV`) compute a result from two operands (tag or literal, `DINT`/`REAL` may mix per the CORE-207 note above) and write it to a destination tag; `DIV` by zero is a defined runtime error, not a crash. | SN-1 | Test | Verified | `10c9dad` |
| CORE-209 | Driver architecture: NETWORK-defined components are instantiated through a common driver interface, so a new component type (e.g. a new sensor) can be added by implementing that interface without modifying the scan-cycle engine or existing instruction logic. | SN-3, SN-4 | Demonstration | Verified | `310a198` |
| DATA-OUT-300 | Internal runtime state model holds current values for every tag (including timer/counter sub-elements) and is updated at the end of every scan cycle, queryable by the rest of the system. | SN-1 | Test | Verified | `77336c5` |
| DATA-OUT-301 | Internal runtime state serializes to the TCP/JSON output message format (current I/O tag values) for transmission to the connected simulation client. | SN-1, SN-3 | Test | Verified | `00f44ee` |
| OUT-400 | Server exposes a TCP listener on an operator-configurable port implementing a custom JSON protocol; exactly one external simulation client may be connected and read current tag state in real time (v1.0 single-client constraint). | SN-1, SN-3 | Test | Verified | 40fa9203139cc380aec7abe685de900e11acec19 |
| OUT-401 | Server accepts JSON write messages from the connected simulation client to set input tag values (e.g. simulated sensor states); writes are applied to the internal model and take effect on the next scan cycle. | SN-1, SN-3 | Test | Verified | `861395d` |
| OUT-402 | Server detects simulation-client disconnect, logs the event (per UI-002's diagnostics), and continues running/accepting a new connection without crashing or requiring a restart. | SN-1 | Test | Verified | `e200537` |
| OUT-403 | Host process runs a continuous, free-running background scan loop once startup succeeds: repeatedly invokes `PlcController.RunScan()` back-to-back with no fixed/idealized scan period (consistent with CORE-203/204's own elapsed-time design — see SDD Architecture) and broadcasts a `tag_update` snapshot (DATA-OUT-301) after each scan completes, so a live `plcemu` process actually exhibits the "real time" I/O exchange described in `docs/PROJECT_DEFINITION.md`'s MVP definition and `docs/SDD.md`'s ICD (`tag_update` sent "again after every scan cycle completes") — not only the externally/test-harness-driven scans OUT-400/OUT-401 were verified against. | SN-1, SN-3 | Test | In Test | `a66ea25`, `c707b04` (lock-contention fix) |
| NFR-500 | Architecture supports holding multiple distinct, independently-stated NETWORK/CONTROL_LOGIC configurations in memory at once (each with isolated tag/runtime state), even though v1.0 only wires up and tests one PLC instance + one simulation client end-to-end. This is an architectural constraint on the SDD, not a v1.0 runtime feature. | SN-1 | Inspection | Verified | `5df0234` |
| NFR-501 | Server builds and runs identically on Windows and Linux from the same C#/.NET codebase, with no OS-specific code path left unabstracted. | SN-1, SN-5 | Test | Verified | CI run `31997343615`; merge `03970cd` |
| NFR-502 | Third-party dependencies are avoided by default; any dependency adopted is referenced only from behind an internal interface/wrapper, never directly from core logic, so it can be swapped later. | SN-5 | Inspection | Verified | `d312747` |
| NFR-503 | Server does not persist runtime tag/controller state across restarts; each launch (re)loads CONTROL_LOGIC/NETWORK definitions fresh and runs in-memory only. | SN-1 | Test | Verified | `9567727` |
| DELIV-900 | As a late-stage v1.0 task, the codebase is organized/refactored to compile as a Microsoft Visual Studio solution (`.sln`) with appropriate project files, so the client's engineering team can open and extend it directly in Visual Studio — **including on a workstation whose installed .NET SDKs are all newer than `global.json`'s pinned floor** (no exact-version match required; a fresh clone must not require installing an old SDK side by side just to build). | SN-5 | Inspection | Verified | `ecbc190`, `98c6485` (rollForward fix), `5f4c5d6` (merge) |
| DELIV-901 | As a late-stage v1.0 task (after DELIV-900), deliver a user quick-start guide at `docs/USER_GUIDE.md` covering: (1) an outline of all projects in the Visual Studio solution — what each does and how it fits together; (2) the CONTROL_LOGIC and NETWORK JSON config formats, including a complete working example with real file paths the reader can copy and run immediately; (3) how to launch the emulator (CLI args, startup diagnostics, TCP/JSON connection) and what to expect; (4) how to author ladder logic and map a component network using the documented schemas; (5) how to extend the system — where a new driver goes, what interface (`IDriver`) it implements, and the minimal steps to register it. Acceptance bar: a reader who has never seen this project can go from `git clone` to a running simulation using only this document and the delivered solution files, no tribal knowledge, no reading source first. | SN-2, SN-4, SN-5 | Demonstration | Approved | |

## Test Procedures

<!-- TP-<nnn>, one per verifiable requirement, with concrete test
     input values and expected output — not just "it works." -->

| TP ID | Verifies | Input / Preconditions | Steps | Expected Result |
| --- | --- | --- | --- | --- |
| TP-001 | UI-001 | `plcemu --control-logic control.json --network network.json`, both files valid. | Launch process with the arguments shown. | Process starts, logs successful load, begins listening on the configured TCP port. |
| TP-002 | UI-001 | `plcemu --network network.json` (missing `--control-logic`). | Launch process. | Process exits non-zero; stderr contains `Missing required argument: --control-logic`. |
| TP-003 | UI-002 | Valid CONTROL_LOGIC with 3 tags (`Start_PB:BOOL`, `Motor_Run:BOOL`, `Preset_Count:DINT`), valid NETWORK with 2 components. | Launch process. | Startup log reports `3 tags loaded`, `2 components loaded`, and lists each tag name/type and component name/driver. |
| TP-004 | UI-003 | CONTROL_LOGIC file with a trailing comma (invalid JSON). | Launch process. | Process exits non-zero; stderr identifies the file and a JSON parse error; no TCP listener starts. |
| TP-005 | UI-003, DATA-IN-103 | NETWORK component `{"name":"ProxSensor1","driver":"DiscreteSensor","tag":"Undefined_Tag"}` where `Undefined_Tag` is not defined in CONTROL_LOGIC. | Launch process. | Process exits non-zero; stderr reports the undefined tag reference by component name and tag name; no TCP listener starts. |
| TP-100 | DATA-IN-100 | CONTROL_LOGIC tags: `Start_PB:BOOL=false`, `Motor_Run:BOOL=false`, `Preset_Count:DINT=0`. | Load and query internal tag table. | Table contains exactly 3 entries with the given names/types/initial values. |
| TP-101 | DATA-IN-101 | Rung: `XIC(Start_PB) OTE(Motor_Run)`. | Load and inspect the parsed rung. | Internal model has 1 rung with instruction sequence `[XIC:Start_PB, OTE:Motor_Run]` in order. |
| TP-102 | DATA-IN-102 | NETWORK component `{"name":"ProxSensor1","driver":"DiscreteSensor","tag":"Start_PB"}` (with `Start_PB` defined in CONTROL_LOGIC). | Load and inspect the parsed network model. | 1 component instantiated with driver type `DiscreteSensor`, bound to tag `Start_PB`. |
| TP-103 | DATA-IN-103 | Same input as TP-005. | Load. | Cross-reference validation fails with a descriptive error (see TP-005); server does not start. |
| TP-200 | CORE-200 | Rung `XIC(A) OTE(B)`; `A:BOOL=true` before scan. | Run 1 scan cycle, then set `A=false`, run 1 more scan cycle. | After scan 1: `B=true`. After scan 2: `B=false`. |
| TP-201 | CORE-201 | Tag `C:BOOL`, tested with `C=true` and `C=false`. | Evaluate `XIC(C)` and `XIO(C)` for each value of `C`. | `C=true`: `XIC`=true, `XIO`=false. `C=false`: `XIC`=false, `XIO`=true. |
| TP-202 | CORE-202 | Rung `XIC(A) XIC(B) OTE(C)` (series AND); `A=true,B=true` then `A=true,B=false`. | Run 1 scan per input combination. | `A=true,B=true` → `C=true`. `A=true,B=false` → `C=false`. |
| TP-203 | CORE-203 | `TON` with `.PRE=2000` (ms). | Set enable=true at t=0. Sample `.ACC`/`.DN` at t=1000ms and t=2100ms. Then set enable=false. | t=1000ms: `.ACC`≈1000, `.DN`=false. t=2100ms: `.DN`=true. After disable: `.ACC`=0, `.DN`=false. |
| TP-204 | CORE-204 | `TOF` with `.PRE=1000` (ms). | Set enable=true (observe `.DN`), then enable=false at t=0; sample `.DN` at t=500ms and t=1100ms. | Enable=true: `.DN`=true immediately. t=500ms after disable: `.DN`=true. t=1100ms after disable: `.DN`=false. |
| TP-205 | CORE-205 | `CTU` with `.PRE=3`. | Drive 3 rising edges on enable, sample state, then 1 more rising edge. | After 3 edges: `.ACC=3`, `.DN=true`. After 4th edge: `.ACC=4`, `.DN` remains true. |
| TP-206 | CORE-206 | `CTD` with `.PRE=3`, `.ACC` starting at 3. | Drive 3 rising edges on enable, sample state, then execute `RES`. | After 3 edges: `.ACC=0`, `.DN=true`. After `RES`: `.ACC=0`, `.DN=false`. |
| TP-207 | CORE-207 | `GRT(Preset_Count, 5)` with `Preset_Count=6`, then `Preset_Count=4`. | Evaluate instruction for each value. | `Preset_Count=6` → true. `Preset_Count=4` → false. |
| TP-208 | CORE-208 | `ADD(A,B,Dest)` with `A=4, B=3`; `DIV(A,B,Dest)` with `A=4, B=0`. | Execute both instructions. | `ADD`: `Dest=7`. `DIV` by zero: defined runtime error/fault flag is raised on the instruction, scan cycle does not crash. |
| TP-209 | CORE-209 | A new driver type (e.g. `DiscreteSensor`) referenced from NETWORK JSON. | Add the new driver implementing the documented driver interface; do not modify the scan engine or instruction classes. | Server loads the new component and its bound tag behaves correctly with no changes to core scan/instruction code (code review + TP-200-class scan test). |
| TP-300 | DATA-OUT-300 | Same tags as TP-100. | Run 1 scan cycle that sets `Motor_Run=true`, `Preset_Count=5`. | Runtime state query returns `{Start_PB:false, Motor_Run:true, Preset_Count:5}`. |
| TP-301 | DATA-OUT-301 | Runtime state from TP-300. | Serialize to the TCP/JSON output schema. | Output message matches `{"type":"tag_update","tags":{"Start_PB":false,"Motor_Run":true,"Preset_Count":5}}` (exact schema finalized in SDD/ICD). |
| TP-400 | OUT-400 | Server started with `--port 5050`. | Connect a TCP client to port 5050, send a read request; then attempt a second concurrent client connection. | First client receives current tag snapshot (per TP-301). Second connection is rejected/refused per the v1.0 single-client constraint. |
| TP-401 | OUT-401 | Server running with rung from TP-200 (`XIC(A) OTE(B)`, renamed `A=Start_PB`, `B=Motor_Run`). | Connected client sends `{"type":"tag_write","tags":{"Start_PB":true}}`. | Next scan cycle: `Start_PB=true`, `Motor_Run=true`. |
| TP-402 | OUT-402 | Server running with 1 connected client. | Client disconnects (TCP FIN). Wait, then connect a new client. | Startup/runtime log records the disconnect event; server keeps running scan cycles; new client connection succeeds without a restart. |
| TP-403 | OUT-403 | `plcemu` launched as a real, standalone process (not a unit-test harness) with the rung from TP-200 (`XIC(Start_PB) OTE(Motor_Run)`), default initial values (`Start_PB=false`, `Motor_Run=false`). | Connect a TCP/JSON client and, without issuing any explicit "run a scan" command or any other message, observe messages received for 2 seconds; then send `{"type":"tag_write","tags":{"Start_PB":true}}` and continue observing for up to 2 more seconds, issuing no further messages. | At least one unsolicited `tag_update` arrives during the first observation window (proves the loop free-runs even with no writes pending). Within 2 seconds of the `tag_write`, a `tag_update` arrives showing `Start_PB=true, Motor_Run=true` with no `read_request` or other manual trigger sent — proves the write is picked up and broadcast by the background loop on its own. |
| TP-500 | NFR-500 | N/A (design review). | Inspect SDD architecture and controller/network state classes; instantiate two independent controller/network objects with distinct CONTROL_LOGIC/NETWORK configs in the same process (unit test). | No shared mutable/static state between the two instances; each holds and scans its own tag/runtime state independently. |
| TP-501 | NFR-501 | Scan-cycle scenario from TP-200. | Run once, as part of the late-stage consolidation pass alongside TP-900 (not per-feature during development — see SDD's "Target-platform verification strategy"): build and run on a `windows-latest` CI runner and a `ubuntu-latest` CI runner (e.g. `docs/ci/build-and-test.yml`, promoted to `.github/workflows/` at consolidation time). | Identical output on both platforms. |
| TP-502 | NFR-502 | N/A (design review). | Review `.csproj`/package references; confirm any third-party package is only referenced from a wrapper/interface class. | No direct third-party API usage from core logic classes. |
| TP-503 | NFR-503 | Server run once, `Start_PB` set true via TP-401. | Stop the process, restart with the same CONTROL_LOGIC/NETWORK files. | `Start_PB` (and all tags) reset to their CONTROL_LOGIC-defined initial values, not the prior run's values. |
| TP-900 | DELIV-900 | Delivered repository at the late-stage v1.0 milestone. **Plus:** a workstation whose only installed .NET SDKs are newer major versions than `global.json`'s pinned floor (e.g. only `9.0.317`/`10.0.303` present, no `8.x`) — reproduces the client's 2026-08-17 field report exactly (VS2022, `dotnet --list-sdks` showing `9.0.317`/`10.0.303` only, `global.json` pinned to `8.0.100`). | Open the `.sln` in Visual Studio (or run `msbuild`/`dotnet build` against it as a CI proxy) in both scenarios: (a) normal CI image with the pinned SDK present, (b) only a newer-major SDK present, no exact/`8.x` match. | (a) and (b) both: all projects load and the solution builds successfully with no missing project-file errors and no `NETSDK1141`/"Unable to resolve the .NET SDK version" error — `global.json`'s `rollForward` must resolve to the newest installed SDK rather than failing. |
| TP-901 | DELIV-901 | A machine with no prior exposure to this project: fresh `git clone` of the delivered repository, plus `docs/USER_GUIDE.md`, no other context. | Following *only* the guide's instructions, verbatim, in order: (1) build/open the solution per the guide's project-outline section; (2) copy the guide's complete CONTROL_LOGIC/NETWORK example to the paths it documents; (3) launch the emulator with the guide's documented CLI invocation; (4) connect a TCP/JSON client per the guide and confirm a tag exchange (read + one write) as documented; (5) separately, follow only the guide's extension section to add one trivial new driver (e.g. a stub sensor) implementing `IDriver`, and confirm it loads without touching core scan-engine/instruction code. | At every step, the guide's instructions match actual behavior exactly — no missing prerequisite, no undocumented manual step, no path/command that doesn't work as written. Reader reaches a running simulation exchanging TCP/JSON tag state using only the guide + delivered files. The new driver added purely from the extension section's instructions loads and its bound tag behaves correctly (CORE-209-class check), with no source file outside the new driver touched. |
