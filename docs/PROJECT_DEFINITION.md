# Project Definition

<!--
Owned by the Solutions Architect. Every item below is tagged
[CONFIRMED] (stated directly by the client) or [PROPOSED] (a
recommended default, not yet a decision). Nothing may be built
against a [PROPOSED] item — flip it to [CONFIRMED] once the client
has actually responded, before handing off to the Systems Engineer.
-->

## Mission Statement

[CONFIRMED] Provide a server-based, extensible PLC emulator that
mimics the Allen-Bradley GuardLogix 1756 family of safety PLC and PLC 
components in a control network, so that theme-park attraction and 
training-simulator designs (built in engines such as Unreal Engine or Unity) 
can be driven by realistic control logic — enabling design validation 
and failure-state prediction before any physical hardware exists.

## Value

[CONFIRMED]
- Lets designers validate ride/show control logic and predict failure
  states against a simulated attraction model, catching engineering
  problems before physical hardware is built.
- Architected so the emulator's interface can eventually be swapped
  for a real GuardLogix PLC with minimal change to the simulated
  attraction — i.e., the simulation is a design/validation stand-in
  for the real control network, not a permanently separate product.
- Doubles as a training tool: lets software engineers learn how PLCs
  and automation networks behave without needing real hardware first.

## Stakeholders and Needs

| Need ID | Stakeholder | Description & Rationale |
| --- | --- | --- |
| SN-1 | Ride/show control engineers | Need to model PLC-driven show control logic against an Unreal/Unity attraction model to validate design decisions and predict failure states before hardware exists. |
| SN-2 | Engineers-in-training | Need a hands-on, command-line tool that teaches PLC ladder-logic concepts and automation-network design without requiring real PLC hardware. |
| SN-3 | System integrators | Need the emulator's control interface to closely mirror a real GuardLogix PLC network so it can later be replaced by real hardware with minimal change to the target simulation. |
| SN-4 | Simulation/tooling developers | Need to extend the network with new "driver" components (e.g. relays, sensors, other field devices) without modifying the core PLC logic — decoupled, industry-standard architecture. |
| SN-5 | Client engineering team (long-term maintainers) | Need the delivered codebase to be one their own engineers can open, understand, and extend going forward — not just a working binary. *(Deliverable requirement — see below.)* |

## MVP Definition

- **Target platform:** [CONFIRMED] Cross-platform command-line server
  (Windows + Linux), no GUI in v1.
- **Language / stack:** [CONFIRMED] C#/.NET, chosen for strong Unity
  interop and fast structured-data/networking development. JSON is
  the preferred data transmission format for both the definition
  files and the runtime I/O interface. Third-party dependencies
  should be avoided by default; a free, industry-standard library may
  be used where it saves significant development time/tokens, but
  only behind an interface so it can be swapped out later without
  touching core logic.
- **Output format and delivery:** [CONFIRMED] A CLI server process that
  loads a human-readable, custom-JSON ladder-logic/structured-text
  (CONTROL_LOGIC) definition and a separate custom-JSON NETWORK
  definition at startup, then exposes a custom TCP/JSON interface over
  which an external simulation engine (Unreal/Unity) exchanges I/O
  state in real time. This is not a real Rockwell EtherNet/IP CIP
  implementation, and definitions are not Rockwell Studio 5000 `.L5X`
  files — both are deliberate v1 simplifications (see Roadmap below).

## Scope

### In scope for MVP

[CONFIRMED]
- CLI server application emulating Allen-Bradley GuardLogix 1756 family of safety PLC and PLC 
  components.
- CONTROL_LOGIC definition: a custom JSON schema for defining ladder
  logic and structured text scripting to "program" the central PLC.
- NETWORK definition: a separate custom JSON schema for defining the
  network of connected control components.
- Extensible "driver" architecture so new control-network component
  types (relays, sensors, etc.) can be added without modifying core
  PLC logic.
- A custom TCP/JSON interface (not real EtherNet/IP CIP) for an
  external simulation engine (Unreal/Unity) to read/write I/O state
  in real time.
- MVP instruction/logic feature set: discrete I/O, basic ladder rungs
  (contacts/coils), timers (TON/TOF), counters (CTU/CTD), basic
  compare/math instructions, and a tag-based data model.
- Architecture that *supports* loading and holding multiple distinct
  NETWORK/CONTROL_LOGIC configurations at once, even though only one
  is exercised end-to-end at a time in v1.0 (see Concurrency below).

### Explicitly out of scope

[CONFIRMED]
- Graphical interface for authoring ladder logic or the network
  definition — deferred to a future version (proposed v3.0). v1 is
  CLI-only.
- Real Rockwell EtherNet/IP CIP protocol compatibility, and Rockwell
  Studio 5000 `.L5X` import/compatibility for either CONTROL_LOGIC or
  NETWORK definitions — both deferred to a later version (proposed
  v4.0+, after the GUI). v1 uses the custom JSON schemas and TCP/JSON
  interface instead.
- Dual-channel safety-rated instructions, motion control, and other
  advanced GuardLogix-specific instructions. Basic safety I/O (e.g.
  simple E-stop/interlock logic) and true safety-rated logic are
  planned for v2.0, immediately following v1.0 — not excluded from
  the product, just sequenced after the MVP.
- Concurrently *running* multiple simulated attractions/controllers
  at once — v1.0 runs and is tested against exactly one connected
  simulation client at a time. (The architecture itself should not
  preclude this later; see Concurrency below.)
- Persistence of runtime tag/controller state across server restarts
  — v1.0 loads its program fresh each launch and runs in-memory only.

### Concurrency

[CONFIRMED] v1.0 drives one PLC emulator instance and one connected
simulation client at a time — this is the only configuration that
will be built and tested in v1.0. However, the server should be
architected so it *could* hold multiple distinct NETWORK/CONTROL_LOGIC
configurations simultaneously without a fundamental redesign; actually
exercising multiple concurrent simulated attractions/controllers is
deferred to a later version. Flagged for the Systems Engineer/Solutions
Architect as an architectural constraint to design around, not a v1.0
feature to build or test.

### Roadmap (context, not v1.0 scope)

[CONFIRMED] Sequencing the client has stated for versions after v1.0,
recorded here so later scope decisions stay consistent with it:
- **v2.0** — basic safety I/O (E-stop/interlock) and true
  safety-rated logic, immediately following v1.0.
- **v3.0** — GUI for authoring CONTROL_LOGIC/NETWORK definitions.
- **v4.0+** — real Rockwell EtherNet/IP CIP protocol compatibility and
  Rockwell Studio 5000 `.L5X` import/compatibility.

## Deliverable Requirements

[CONFIRMED] The client has described this project in terms that go
beyond a working program: it is meant to be extended over time
("extensible with drivers"), used as a teaching tool for engineers,
and ultimately architected so a real PLC could stand in for the
emulator with minimal change elsewhere. This implies the delivered
codebase itself — not just its runtime behavior — is a client
deliverable: it must be maintainable and extensible by the client's
own engineering team.

Build-tooling/documentation decision (confirmed): development itself
may use whatever environment is available in the GitHub Actions VM —
no specific IDE is required during the build process. As a final step
before the v1.0 deliverable ships, the codebase must be refactored as
needed so that it compiles successfully as a Microsoft Visual Studio
project/solution, since the client's own engineers will open and
extend it in Visual Studio going forward. Flagged for Systems Engineer
follow-up: this is a build-tooling/documentation decision to schedule
as an explicit late-stage v1.0 task (e.g. an implementation-plan item
near the end of the sequence), not something to defer indefinitely or
let fall out of scope.

NOTE WELL: Development occurs entirely within the pipeline's native environment (Ubuntu/.NET). Refactoring into a Visual Studio project is a final deliverable step, performed once, after v1.0 is functionally complete and tested — not verified continuously during development. No parallel Windows/MSVC verification pipeline runs per-feature.

## Status

All open questions from the Issue #1 kickoff have been answered by the
client (see issue #1 comments for the full exchange) and are now
folded into the sections above. Scope for v1.0 is fully defined.
