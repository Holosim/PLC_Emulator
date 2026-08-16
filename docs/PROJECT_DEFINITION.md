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
- **Language / stack:** [CONFIRMED] Core programming in C#, using JSON 
  or other structured data transmission formats for persistent messaging 
  (messages continue to be available between connection points in the network 
  in case continuous power/connection is unavailable) — candidates and
  trade-offs raised for client input in the kickoff questions below.
- **Output format and delivery:** [CONFIRMED] A CLI server process that
  loads a human-readable structured ladder-logic/network definition
  file at startup, then exposes a defined interface over which an
  external simulation engine (Unreal/Unity) exchanges I/O state in
  real time.

## Scope

### In scope for MVP

[CONFIRMED]
- CLI server application emulating Allen-Bradley GuardLogix 1756 family of safety PLC and PLC 
  components.
- A human-readable, structured file format (e.g. JSON) for defining
  ladder logic and structured text scripting to "program" the central PLC.
- A human-readable, structured file format (e.g. JSON) for defining
  the network of connected control components.
- Extensible "driver" architecture so new control-network component
  types (relays, sensors, etc.) can be added without modifying core
  PLC logic.
- A defined interface for an external simulation engine (Unreal/Unity)
  to read/write I/O state in real time. 
- A baseline instruction/logic feature set sufficient for representative
  ride-control scenarios (exact subset TBD — see kickoff questions).

### Explicitly out of scope

[CONFIRMED]
- Graphical interface for authoring ladder logic or the network
  definition — deferred to a future version. v1 is CLI-only.

[CONFIRMED]
- Full parity with GuardLogix's complete instruction set, including
  advanced/dual-channel safety-rated instructions and motion control.
- Multi-instance / Multi-tenant operation (TBD). Unless the server 
  can handle multiple different control networks simultaneously, 
  simulated attractions will each have their own running server instance.
- Persistence of runtime tag/controller state across server restarts.

## Deliverable Requirements

[CONFIRMED] The client has described this project in terms that go
beyond a working program: it is meant to be extended over time
("extensible with drivers"), used as a teaching tool for engineers,
and ultimately architected so a real PLC could stand in for the
emulator with minimal change elsewhere. This implies the delivered
codebase itself — not just its runtime behavior — is a client
deliverable: it must be maintainable and extensible by the client's
own engineering team. Exact build-tooling and documentation
conventions (e.g. whether an IDE-ready project/solution is required)
are still open — see kickoff questions below. Flagged for Systems
Engineer follow-up as a build-tooling/documentation decision once
confirmed.


## Questions from Issue #1 Project Kickoff: PLC Emulator
Open questions that need answering to lock the MVP:

1. Engine communication protocol — The emulator does not need to speak to a real Rockwell industrial protocol (EtherNet/IP CIP) in version 1. Protocol-compatiblilty with real PLC hardware/tooling can be added in a later version, perhaps v4.0 or later since v3.0 would more likely be the integration of a GUI interface. It is acceptable for v1 to use a simpler custom TCP/JSON interface between the emulator and Unreal/Unity, with real-protocol compatibility deferred to a later version once the core logic engine is proven.

2.  A. CONTROL_LOGIC definition: Ladder logic and structured text scripting can be defined using a custom JSON schema, not an import of real Rockwell Studio 5000 .L5X project files. Later versions can introduce a shift to working with Rockwell Studio 5000 .L5X project files, such as the previously mentioned Engine communication protocol upgrade proposed for version 4.0 or later.
    B. NETWORK definition: Network definition can be defined using another custom JSON schema, not an import of real Rockwell Studio 5000 .L5X project files.


3.  A. PLC feature scope for MVP v1.0 — 
    - discrete I/O
    - basic ladder rungs (contacts/coils)
    - timers (TON/TOF)
    - counters (CTU/CTD)
    - basic compare/math instructions
    - tag-based data model. 

    B. Explicitly excluding from MVP v1.0: 
    - Dual-channel safety-rated instructions, motion control, and other advanced GuardLogix-specific instructions. 
    - Given "safety PLC" is central to the value proposition — it will be crucial to add basic safety I/O behavior (e.g., simple E-stop/interlock logic) and true safety-rated logic in version 2.0 immediately after confirmation of v1.0 is completed.

4. Concurrency:
  - One PLC emulator instance driving one connected simulation client at a time is sufficient for v1.0 .
  - Having said that, let's try to architect the server to handle multiple different configurations simultaneously based on the NETWORK definition and CONTROL_LOGIC definition. We can defer testing multiple simulated attractions/controllers running concurrently in a later version. For now we will only define and handle one simulation at a time.

5. State persistence — The emulator does not need to save/restore runtime tag state across restarts for MVP v1.0. It is sufficient to "load program fresh each launch, run in-memory".

6. Language / stack — 
    - The core programming language is C#/.NET for its strong Unity interop and fast structured-data/networking development
    - Preferred data transmission format is JSON.
    - Prefer to avoid 3rd party dependencies. However, if introducing a free industry standard 3rd party library will save a significant amount of time and tokens, then we can use it via interfaces that will simplify replacement later.

7. Deliverable form:
    - Build tooling can be whatever development environment is available in the Github VM. No specific IDE required for development.
    - However, the final v1.0 deliverable must be refactored to compile successfully in Microsoft Visual Studio as a final step. Since this is also meant to be extended by my team and used as a teaching tool, the codebase itself is a deliverable, not just a runnable binary.
