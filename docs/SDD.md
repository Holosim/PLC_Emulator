# Software Design Document (SDD)

<!--
Owned by the Systems Engineer, refined with the Software Engineer.
Describes the system architecture and the build/toolchain
conventions the codebase follows.
-->

## Architecture

The system is a single OS process (`plcemu`, a cross-platform CLI
server) composed of five collaborating parts. Everything downstream of
the composition root is built around one core design decision, driven
by NFR-500: **all mutable runtime state for a PLC lives inside an
instantiable `PlcController` object, never behind a static/singleton
field.** Nothing else in this architecture depends on there being
exactly one controller — v1.0 simply chooses to construct and wire up
one.

- **Host (composition root).** Parses CLI arguments (UI-001), invokes
  the Config Loader, constructs one `PlcController` from the resulting
  definitions, constructs one `TcpJsonServer` bound to that
  controller, and starts both. Owns startup diagnostics (UI-002) and
  fail-fast error handling (UI-003) — nothing below the Host swallows
  a load error into a partially-started state.
- **Config Loader / Validator.** Parses CONTROL_LOGIC and NETWORK JSON
  into immutable definition objects (`ControlLogicDef`, `NetworkDef`),
  including cross-file validation (DATA-IN-103). Produces either a
  fully valid definition pair or a descriptive error; never a partial
  result.
- **PlcController.** The unit of isolation. Holds one controller's
  `TagTable` (DATA-OUT-300), its parsed rung program, its instantiated
  driver set, and its incoming-write queue. Exposes `RunScan()`,
  `GetSnapshot()`, and `QueueWrite(tag, value)`. Two `PlcController`
  instances constructed side by side in the same process share no
  mutable state — each owns its own tag table, driver instances, and
  scan state. This is what makes NFR-500 an architectural property
  rather than a v1.0 implementation detail: v1.0's Host simply chooses
  to construct exactly one.
- **Scan Engine.** Owned by (not shared across) a `PlcController`.
  Evaluates rungs in program order once per scan (CORE-200), updating
  the owning controller's `TagTable` before the next scan begins.
  Instruction classes (`XIC`, `OTE`, `TON`, `CTU`, etc.) are stateless
  evaluators operating on the tag table passed to them — no
  instruction class holds tag state itself, so the same instruction
  logic is reused correctly across multiple controller instances.
- **Driver layer.** NETWORK-defined components are instantiated
  through a common `IDriver` interface (CORE-209) and bound to their
  owning controller's `TagTable` at construction — never to a global
  registry. Adding a new component type means implementing `IDriver`;
  it never requires touching the Scan Engine or instruction classes.
- **TCP/JSON Server.** Wraps exactly one `PlcController` reference in
  v1.0 and enforces the single-client constraint (OUT-400) at the
  listener, not inside the controller — the controller itself has no
  concept of "the" client. This keeps the single-client rule a v1.0
  Host/Server policy, not a structural limit on `PlcController`.

### Component Architecture

Structural decomposition (block definition):

```mermaid
classDiagram
    class Host {
        +Main(args)
    }
    class ConfigLoader {
        +LoadControlLogic(path) ControlLogicDef
        +LoadNetwork(path) NetworkDef
        +Validate(ControlLogicDef, NetworkDef)
    }
    class PlcController {
        -TagTable tags
        -Rung[] rungs
        -IDriver[] drivers
        -WriteQueue pendingWrites
        +RunScan()
        +GetSnapshot() TagSnapshot
        +QueueWrite(tag, value)
    }
    class ScanEngine {
        +Evaluate(rungs, tags)
    }
    class TagTable {
        +Get(name)
        +Set(name, value)
    }
    class IDriver {
        <<interface>>
        +Bind(TagTable)
        +OnScanComplete()
    }
    class TcpJsonServer {
        -PlcController controller
        +Start(port)
        +Broadcast(snapshot)
        +OnClientMessage(msg)
    }
    Host --> ConfigLoader
    Host --> PlcController : constructs (1 in v1.0)
    Host --> TcpJsonServer
    PlcController --> ScanEngine
    PlcController --> TagTable
    PlcController --> IDriver : owns instances
    TcpJsonServer --> PlcController : holds reference to
```

Signal/data flow at runtime (internal block view):

```mermaid
flowchart LR
    CF["CONTROL_LOGIC.json"] --> CL[ConfigLoader]
    NF["NETWORK.json"] --> CL
    CL -->|ControlLogicDef, NetworkDef| PC[PlcController instance]
    PC --> SE[ScanEngine]
    SE -->|reads/writes each scan| TT[TagTable]
    PC --> DR[Driver instances]
    DR -->|bound to| TT
    TT -->|end-of-scan snapshot| TJ[TcpJsonServer]
    TJ -->|tag_update| SIM[Simulation client]
    SIM -->|tag_write| TJ
    TJ -->|queued write, applied at next scan start| PC
```

Note the write path: `tag_write` messages are queued by the server and
drained by `PlcController` at the start of its own scan, not applied
directly to `TagTable` from the network thread — this is what keeps
scan evaluation single-threaded and avoids introducing shared mutable
state between the I/O thread and the scan loop (reinforcing NFR-500's
"no shared mutable state" property at the threading level, not just
the controller level).

## Coding Standards

Established here; open to refinement with the Software Engineer as
implementation surfaces real questions — flag anything that doesn't
fit cleanly.

- **Namespaces / project layout:** `PlcEmulator.Host`,
  `PlcEmulator.Config`, `PlcEmulator.Core` (TagTable, ScanEngine,
  instructions), `PlcEmulator.Drivers` (`IDriver` + built-in drivers),
  `PlcEmulator.Network` (TCP/JSON server, message schema). One project
  per namespace root, referenced from a top-level `PlcEmulator.sln`.
- **Naming:** standard .NET conventions — `PascalCase` for
  types/public members/methods, `camelCase` for locals/parameters,
  `_camelCase` for private instance fields. Ladder-logic domain names
  (tag names, instruction mnemonics) are preserved verbatim from
  CONTROL_LOGIC JSON (`XIC`, `OTE`, `TON`, etc.) rather than translated
  into more "C#-ish" names — the client's engineers already think in
  Rockwell mnemonics (SN-3).
- **Tag data model:** `TagType` enum (`Bool`, `Dint`, `Real`); a `Tag`
  class holding a value plus optional structured sub-elements
  (`TimerState { Pre, Acc, Dn, En }`, `CounterState { Pre, Acc, Dn }`)
  per DATA-IN-100.
- **Instruction classes:** one class per mnemonic under
  `PlcEmulator.Core.Instructions`, each implementing a shared
  `IInstruction.Evaluate(TagTable tags)` — stateless, operating only
  on the tag table it's given (see Architecture above — this is what
  keeps instruction logic reusable across isolated controller
  instances).
- **Driver interface:** `IDriver.Bind(TagTable tags, NetworkComponentConfig config)`
  at construction, `IDriver.OnScanComplete()` called once per scan
  after tag values settle, for drivers that need to react to state
  changes (e.g. a sensor driver recomputing a derived reading).
- **Error handling:** load-time errors (UI-003, DATA-IN-103) throw
  descriptive exceptions caught once at the Host boundary and reported
  as non-zero exit + stderr message; the Scan Engine never throws for
  expected runtime conditions like divide-by-zero (CORE-208) — those
  set a fault flag on the offending tag/instruction result instead, so
  a single bad rung can't crash the scan loop.
- **Documentation:** public types/members get XML doc comments —
  required reading for the client's own engineers extending the
  codebase later (SN-5, DELIV-900).

## Build & Toolchain Conventions

- **Runtime/language:** C# on .NET 8 (LTS) — long support window,
  first-class cross-platform (Windows + Linux) support, native
  SDK-style project files.
- **Project structure:** SDK-style `.csproj` per namespace root (see
  Coding Standards) referenced from one `PlcEmulator.sln` at the repo
  root, generated with `dotnet new`. SDK-style `.csproj`/`.sln` files
  are natively Visual Studio-compatible — day-to-day development does
  not need a separate "CLI project format" vs. "VS project format";
  there is one project structure throughout.
- **Day-to-day build/test:** `dotnet build`, `dotnet test`,
  `dotnet run` — no IDE requirement during development, per
  `docs/PROJECT_DEFINITION.md`'s Deliverable Requirements. CI (GitHub
  Actions, Ubuntu runner) uses these same commands.
- **Dependency policy (NFR-502):** no third-party NuGet packages by
  default. `System.Text.Json` (part of the .NET SDK, not a third-party
  package) is the JSON library for both config parsing and the
  TCP/JSON protocol. If a genuine third-party dependency is ever
  adopted, it is referenced only from a wrapper class behind an
  interface in the relevant namespace (e.g. an `IJsonCodec` wrapper if
  the built-in serializer ever needs replacing) — never called
  directly from `PlcEmulator.Core` or `PlcEmulator.Drivers`.
- **DELIV-900 (Visual Studio solution, late-stage task):** because the
  project already builds as SDK-style `.csproj`/`.sln` from day one,
  DELIV-900 is scheduled in the Implementation Plan as a
  **verification and cleanup** pass, not a structural rewrite:
  confirm the solution opens cleanly in Visual Studio, confirm no
  missing project references or CLI-only assumptions (e.g. hardcoded
  `/`-only paths — see NFR-501), and fix anything that surfaces. This
  keeps DELIV-900 cheap precisely because the toolchain decision above
  avoided a format mismatch to begin with. Development occurs entirely within the Github/Anthropic agentic pipeline's native environment (Ubuntu/.NET). Refactoring into a Visual Studio project is a final deliverable step, performed once, after v1.0 is functionally complete and tested — not verified continuously during development. No parallel Windows/MSVC verification pipeline runs per-feature.

### Target-platform verification strategy (explicit decision)

The agent pipeline executes on Ubuntu; the deliverable targets both
Windows and Linux (NFR-501) and, separately, must open in the Windows
Visual Studio IDE (DELIV-900) AFTER the final codebase for v1.0 is tested and verified. These are two different platform
concerns and are verified on two different schedules, deliberately:

- **NFR-501 (Windows/Linux behavioral parity) gates every feature.**
  GitHub Actions provides `windows-latest` and `ubuntu-latest` runners
  natively — no extra environment setup or permissions beyond what CI
  already has. Because that cost is already low, CI runs `dotnet
  build` + `dotnet test` on **both** runners for every
  `[RTVM-014]`-style feature, per the RTVM's test procedures (e.g.
  TP-501). Deferring this to a one-time consolidation step would risk
  an OS-specific assumption (path separators, line endings, file
  locking) accumulating silently across many features and surfacing
  as one large, hard-to-bisect failure at the end — not worth it when
  the second runner is nearly free to add per-feature.
- **DELIV-900 (opens/builds as a Visual Studio solution) is a
  one-time, late-stage consolidation task, not a per-feature gate.**
  `windows-latest` runners already carry MSBuild/the .NET SDK, so
  `dotnet build` on that runner is a reasonable continuous proxy for
  "this still compiles under a VS-compatible toolchain" — but actually
  opening the solution in the Visual Studio *IDE* and confirming a
  clean project load is a manual, human-facing check with no
  meaningful per-feature signal (it either works because the project
  format is sound, or it doesn't for reasons unrelated to any single
  feature). Gating every feature on it would add a second execution
  environment's setup/permission questions to every feature for a
  check that only actually needs to happen once, right before
  delivery. Therefor, only focus on the single verification step, and leave all windows-verification until the very last issue.

## Data Architecture

This system has two communicating components in a single OS process
boundary each — the `plcemu` server process and an external
Unreal/Unity simulation client process — connected over a custom
TCP/JSON interface (OUT-400/401/402, DATA-OUT-300/301).

- **Transfer method:** a single persistent TCP connection per client.
  Because TCP is a byte stream, messages need explicit framing: each
  message is one **newline-delimited JSON object** (NDJSON) —
  `System.Text.Json` serializes to a single line, terminated by `\n`.
  Chosen over length-prefixed binary framing because it stays
  dependency-free, is trivially diagnosable with a raw TCP/telnet
  client during development and training use (SN-2), and message
  sizes here (a tag snapshot, a handful of writes) are small enough
  that framing overhead is a non-issue.
- **Ordering guarantees:** TCP already guarantees in-order, reliable
  delivery on a single connection, so no additional sequencing layer
  is needed. On the write path, `tag_write` messages are queued by the
  server as they arrive and drained by `PlcController` atomically at
  the start of its next scan (never mid-scan) — this guarantees a
  scan always sees a consistent set of inputs, and also means the
  network I/O thread never mutates `TagTable` directly (see
  Architecture above).
- **Storage:** none, by design (NFR-503). CONTROL_LOGIC and NETWORK
  JSON files are the only persisted state, read once at startup;
  `TagTable` and all driver state live in memory for the life of the
  process and are discarded on exit. There is no database, cache, or
  on-disk runtime state to reason about.

## Interface Control Document (ICD): TCP/JSON Protocol

External simulation engines (Unreal/Unity) build against this
directly (SN-1, SN-3), so it's specified here rather than left
implicit in code.

**Transport:** one TCP connection, one message per line, UTF-8 JSON,
`\n`-terminated. Port is operator-configurable at startup (OUT-400,
e.g. `--port 5050`).

**Connection lifecycle:**

```mermaid
stateDiagram-v2
    [*] --> Listening
    Listening --> Connected: client connects (accepted)
    Connected --> Connected: additional connect attempts refused (v1.0 single-client, OUT-400)
    Connected --> Listening: client disconnects (FIN), logged per UI-002 (OUT-402)
```

**Message types:**

| Type | Direction | Schema | Sent when |
| --- | --- | --- | --- |
| `tag_update` | Server → Client | `{"type":"tag_update","tags":{"<name>":<value>,...}}` | Immediately on connect (serves as the initial snapshot), and again after every scan cycle completes (DATA-OUT-301) |
| `tag_write` | Client → Server | `{"type":"tag_write","tags":{"<name>":<value>,...}}` | Any time the client wants to set input tag values (e.g. a simulated sensor state); applied at the start of the next scan (OUT-401) |
| `read_request` | Client → Server (optional) | `{"type":"read_request"}` | A one-shot client (e.g. a diagnostic/training tool, not a continuous simulation loop) explicitly asking for a snapshot outside the normal per-scan push |

Example exchange, matching RTVM test procedures TP-301/TP-401:

```
S→C: {"type":"tag_update","tags":{"Start_PB":false,"Motor_Run":false,"Preset_Count":0}}
C→S: {"type":"tag_write","tags":{"Start_PB":true}}
S→C: {"type":"tag_update","tags":{"Start_PB":true,"Motor_Run":true,"Preset_Count":0}}
```

`tags` values are JSON `bool` for `BOOL`, JSON `number` for `DINT`/
`REAL`. Structured tag sub-elements (`.PRE`/`.ACC`/`.DN`/`.EN`) are not
addressed individually over this interface in v1.0 — only their
parent tag's externally-relevant value is exposed; internal
timer/counter bookkeeping stays server-side. (Flagged in case a future
version needs sub-element visibility over the wire — not a v1.0 gap,
since no MVP scope item calls for it.)
