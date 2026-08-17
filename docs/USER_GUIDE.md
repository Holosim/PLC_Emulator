# PLC Emulator — User Quick-Start Guide

Traces to **DELIV-901** (`docs/RTVM.md`). This guide is written for a
reader who has never seen this project before: everything you need to
build the solution, author a CONTROL_LOGIC/NETWORK configuration pair,
launch the emulator, connect a simulation client to it, and extend it
with a new driver is here — you should not need to read any source
file first.

All commands below assume a shell at the repository root, immediately
after `git clone`. Paths are written with forward slashes; the
`dotnet` CLI accepts them the same way on Windows and Linux.

## 1. What's in the solution

Opening `PlcEmulator.sln` (Visual Studio, `dotnet build`, or
`dotnet run` — no IDE is required; see §3) gives you six projects:

| Project | What it does |
| --- | --- |
| `PlcEmulator.Host` | The composition root and CLI entry point. This is the `plcemu` executable: parses command-line arguments, loads and validates your CONTROL_LOGIC/NETWORK files, builds one `PlcController`, starts the TCP/JSON server, and prints startup diagnostics. |
| `PlcEmulator.Config` | Parses and validates CONTROL_LOGIC and NETWORK JSON into plain definition objects (`ControlLogicDef`, `NetworkDef`, etc.). Knows nothing about ladder-logic evaluation or drivers — it only understands the file formats. |
| `PlcEmulator.Core` | The simulation engine: `TagTable` (runtime tag values), `ScanEngine` and the instruction classes (`XIC`, `OTE`, `TON`, `CTU`, ...), and `PlcController`, which owns one isolated PLC's worth of state. Also declares the `IDriver` interface that every component driver implements (see §5 for why it lives here and not in `PlcEmulator.Drivers`). |
| `PlcEmulator.Drivers` | The built-in `IDriver` implementations — `DiscreteSensorDriver`, `RelayDriver` — plus `DriverFactory`, which maps a NETWORK component's `"driver"` name to a concrete driver instance. This is where a new driver you write goes too (§5). |
| `PlcEmulator.Network` | The TCP/JSON server (`TcpJsonServer`) and its wire-message types. Talks to `PlcEmulator.Core` only through `PlcController`'s public methods. |
| `PlcEmulator.Tests` | The automated test suite (MSTest) covering every project above. Not part of the shipped `plcemu` executable. |

How they depend on each other (an arrow means "references"):

```
PlcEmulator.Host  --> Config, Core, Drivers, Network
PlcEmulator.Drivers --> Core, Config
PlcEmulator.Network --> Core
PlcEmulator.Core --> Config
PlcEmulator.Config --> (nothing else in the solution)
```

`Core` never references `Network` or `Drivers` — the simulation engine
has no idea a TCP server or a concrete driver exists. `Host` is the
only project that references everything, because it's the only place
that needs to wire the whole system together (this is also why
resolving a NETWORK component's driver type happens the way it does —
see §5).

## 2. Configuration file formats

`plcemu` reads exactly two JSON files at startup: **CONTROL_LOGIC**
(the tag database and ladder-logic program) and **NETWORK** (the
control-network components and what each is wired to). Both are
authored by hand — there is no GUI editor.

### CONTROL_LOGIC.json

```json
{
  "tags": [
    { "name": "Start_PB", "type": "BOOL", "initialValue": false },
    { "name": "Motor_Run", "type": "BOOL", "initialValue": false }
  ],
  "rungs": [
    { "instructions": [
        { "op": "XIC", "operands": ["Start_PB"] },
        { "op": "OTE", "operands": ["Motor_Run"] }
    ] }
  ]
}
```

- **`tags`** — every tag your program uses, each with a unique `name`.
  `type` is one of:
  - `BOOL`, `DINT`, `REAL` — scalar; requires an `initialValue`
    (JSON `bool`/`number`/`number` respectively).
  - `TIMER`, `COUNTER` — structured; uses `preset` (a JSON number,
    the tag's `.PRE`) instead of `initialValue`. `.ACC`/`.DN`/`.EN`
    (and, for counters, `.CU`/`.CD`) always start at their zero/false
    defaults — they can't be set from the file.
- **`rungs`** — an ordered array of rungs; each rung is an ordered
  array of `instructions`. Every instruction is
  `{ "op": "<MNEMONIC>", "operands": [...] }`. An operand is a JSON
  string (a tag-name reference) or a JSON number (a literal). See §4
  for the full instruction set and how many operands each one takes.

### NETWORK.json

```json
{
  "components": [
    { "name": "StartButton", "driver": "DiscreteSensor", "tag": "Start_PB" }
  ]
}
```

- **`components`** — an array of control-network components. Each
  needs a unique `name`, a `driver` type name (built-in: `DiscreteSensor`,
  `Relay` — see §5 to add your own), and the tag(s) it binds to,
  either as a single `"tag"` string or a `"tags"` array if a
  component needs more than one. Every tag referenced here must
  already be defined in your CONTROL_LOGIC file — `plcemu` refuses to
  start otherwise (see §3, "Startup diagnostics and errors").

### A complete, working example

Create a `quickstart/` folder at the repository root with these two
files. This is the exact rung used throughout `docs/RTVM.md`'s test
procedures (TP-200/TP-401), so it is a proven-working starting point.

**`quickstart/CONTROL_LOGIC.json`:**

```json
{
  "tags": [
    { "name": "Start_PB", "type": "BOOL", "initialValue": false },
    { "name": "Motor_Run", "type": "BOOL", "initialValue": false }
  ],
  "rungs": [
    { "instructions": [
        { "op": "XIC", "operands": ["Start_PB"] },
        { "op": "OTE", "operands": ["Motor_Run"] }
    ] }
  ]
}
```

**`quickstart/NETWORK.json`:**

```json
{
  "components": [
    { "name": "StartButton", "driver": "DiscreteSensor", "tag": "Start_PB" }
  ]
}
```

This declares two BOOL tags and one rung (`Start_PB` energizes
`Motor_Run`), plus one discrete-sensor component wired to `Start_PB`.
§3 launches `plcemu` against exactly these two files.

## 3. Launching the emulator

### Build it

From the repository root:

```
dotnet build PlcEmulator.sln
```

(Or open `PlcEmulator.sln` in Visual Studio and build there — see
§1; both produce the same output. `global.json` at the repository
root pins the SDK `plcemu` builds with and rolls forward
automatically to a newer installed SDK, so you do not need an exact
SDK version match.)

### Run it

```
dotnet run --project src/PlcEmulator.Host -- --control-logic quickstart/CONTROL_LOGIC.json --network quickstart/NETWORK.json --port 5050
```

(Everything after the bare `--` is passed to `plcemu` itself, not to
`dotnet run`.) Equivalently, after `dotnet build`, run the built
executable directly:

```
dotnet src/PlcEmulator.Host/bin/Debug/net8.0/PlcEmulator.Host.dll --control-logic quickstart/CONTROL_LOGIC.json --network quickstart/NETWORK.json --port 5050
```

CLI arguments, all `--name value` pairs:

| Argument | Required? | Meaning |
| --- | --- | --- |
| `--control-logic <path>` | Yes | Path to your CONTROL_LOGIC JSON file. |
| `--network <path>` | Yes | Path to your NETWORK JSON file. |
| `--port <number>` | No (defaults to `5000`) | TCP port the JSON server listens on. |

### Startup diagnostics and errors

On success, `plcemu` prints a summary of everything it loaded, then
starts listening and blocks (it's a long-running server, not a
one-shot command — leave it running in this terminal and use another
terminal, or a separate simulation client, for §3's TCP walkthrough
below):

```
plcemu: 2 tags loaded from 'quickstart/CONTROL_LOGIC.json':
plcemu:   Start_PB (BOOL)
plcemu:   Motor_Run (BOOL)
plcemu: 1 components loaded from 'quickstart/NETWORK.json':
plcemu:   StartButton (DiscreteSensor)
plcemu: listening on TCP port 5050.
```

If CONTROL_LOGIC or NETWORK is malformed, references an undefined
tag, or names a driver type `plcemu` doesn't recognize, it prints a
`plcemu: <descriptive error>` line to stderr and exits with a non-zero
status — **before** the TCP listener ever starts, so you never end up
talking to a half-loaded server.

### Connecting a simulation client

The protocol is plain TCP: one JSON object per line (UTF-8,
`\n`-terminated — "NDJSON"). Any TCP client works; here's a minimal
one using `python3` (no extra packages required) against the running
process from §3 above:

```python
import socket, json
s = socket.create_connection(("127.0.0.1", 5050))
f = s.makefile("rw", buffering=1)

print(f.readline())  # initial snapshot, sent immediately on connect
f.write('{"type":"read_request"}\n')
print(f.readline())  # a fresh snapshot, answered immediately

f.write('{"type":"tag_write","tags":{"Start_PB":true}}\n')

# plcemu's scan loop is free-running (OUT-403) and re-broadcasts a
# tag_update after every scan, so the write's effect shows up in the
# stream almost immediately in real time. But the loop runs far faster
# than a client can read line-by-line, so don't assume the *next* line
# is the one that reflects your write — you may first read past a
# batch of tag_update lines that were already queued from scans just
# before it landed. Keep reading until you actually see it:
while True:
    msg = json.loads(f.readline())
    if msg["tags"]["Start_PB"]:
        print("write took effect:", msg)
        break
```

What you should see (abbreviated — `plcemu` sends far more `tag_update`
lines than shown between the third and last lines below, because the
scan loop free-runs with no fixed period):

```
S->C: {"type":"tag_update","tags":{"Start_PB":false,"Motor_Run":false}}
C->S: {"type":"read_request"}
S->C: {"type":"tag_update","tags":{"Start_PB":false,"Motor_Run":false}}
C->S: {"type":"tag_write","tags":{"Start_PB":true}}
S->C: {"type":"tag_update","tags":{"Start_PB":false,"Motor_Run":false}}   (repeated many times)
...
S->C: {"type":"tag_update","tags":{"Start_PB":true,"Motor_Run":true}}    <- the write has taken effect
```

The three message types:

| Type | Direction | Schema |
| --- | --- | --- |
| `tag_update` | Server → Client | `{"type":"tag_update","tags":{"<name>":<value>,...}}` — sent immediately on connect, and again after every scan cycle completes. The scan loop is free-running (no fixed period; see CORE-203/204 and OUT-403), so expect a very high message rate (hundreds of thousands per second on typical hardware), not a periodic tick. |
| `tag_write` | Client → Server | `{"type":"tag_write","tags":{"<name>":<value>,...}}` — asks the server to set one or more input tag values; queued and applied atomically at the start of the next scan cycle, never applied instantly or mid-scan. |
| `read_request` | Client → Server | `{"type":"read_request"}` — ask for the current snapshot outside the normal per-scan push. |

`tags` values are JSON `bool` for a `BOOL` tag, JSON `number` for
`DINT`/`REAL`. Only one client may be connected at a time — a second
connection attempt is accepted at the TCP layer and then immediately
closed. A client disconnecting (or a rejected second connection) never
stops the server; it keeps listening for the next connection.

**Observing a `tag_write`'s effect on a live process:** a `tag_write`
is validated, queued, and applied atomically at the very start of the
controller's next scan cycle. `plcemu` runs a free-running background
scan loop for as long as the process is up (OUT-403): back-to-back,
no artificial delay between scans, matching how a real PLC scans (see
CORE-203/204 for why v1.0 deliberately has no fixed scan period). That
means the next scan — and your write's effect — happens within a
fraction of a millisecond of the write being received; this is
genuinely live-observable, confirmed by running the exact exchange
above against a live `quickstart/` process. The one practical wrinkle
is on the *reading* side, not the writing side: because the loop
broadcasts a fresh `tag_update` after every single scan without
throttling for a slow reader, a client that only reads one line at a
time can fall behind a busy process and see a batch of already-queued
`tag_update` lines (reflecting state from just before your write
landed) before it reaches the one that reflects it — that's why the
snippet above reads in a small loop instead of checking only the very
next line. It is not a sign the write failed.

## 4. Writing ladder logic and a component network

### The instruction set

Every instruction is `{ "op": "<MNEMONIC>", "operands": [...] }`
inside a rung's `instructions` array, evaluated left to right. An
operand is either a tag-name string or a numeric literal. Rungs (and
the instructions within a rung) only run in series — v1.0 has no
parallel-branch ("OR") syntax.

| Mnemonic | Operands | Behavior |
| --- | --- | --- |
| `XIC` | 1 tag (`BOOL`) | Normally-open contact: true when the tag is true. |
| `XIO` | 1 tag (`BOOL`) | Normally-closed contact: true when the tag is false. |
| `OTE` | 1 tag (`BOOL`) | Output coil: sets the tag equal to the rung's evaluated (non-latching) logic each scan. |
| `TON` | 1 tag (`TIMER`) | Timer-on-delay: while enabled, `.ACC` accumulates elapsed time; `.DN` becomes true once `.ACC >= .PRE`; disabling resets `.ACC` to 0 and `.DN` to false. |
| `TOF` | 1 tag (`TIMER`) | Timer-off-delay: `.DN` is true immediately while enabled; on disable, `.DN` stays true until `.PRE` has elapsed, then goes false. |
| `CTU` | 1 tag (`COUNTER`) | Count-up: `.ACC` increments by 1 on each rising edge of the instruction's enable input; `.DN` becomes (and stays) true once `.ACC >= .PRE`. |
| `CTD` | 1 tag (`COUNTER`) | Count-down: `.ACC` decrements by 1 on each rising edge; `.DN` is true when `.ACC <= 0`. |
| `RES` | 1 tag (`COUNTER`) | Resets a counter's `.ACC` to 0 and `.DN` to false. |
| `EQU` / `NEQ` / `GRT` / `LES` / `GEQ` / `LEQ` | 2 (tag or literal, numeric) | Compares two numeric (`DINT`/`REAL`) operands; ANDs the boolean result into the rung. |
| `ADD` / `SUB` / `MUL` / `DIV` | 2 sources (tag or literal, numeric) + 1 destination tag (`DINT`/`REAL`) | Computes a result from the two source operands and writes it to the destination tag. `DIV` by zero sets a fault on the destination tag (see below) instead of crashing. |

**Important limitation:** a `TIMER`/`COUNTER` tag's `.DN`/`.ACC`
sub-elements cannot be used as an operand anywhere else in v1.0 — a
contact (`XIC`/`XIO`) requires a `BOOL` tag, and a compare instruction
requires a `DINT`/`REAL` tag; neither accepts a `TIMER`/`COUNTER` tag
directly, and there is no dotted `Tag.DN`-style addressing. In
practice this means you cannot chain "timer done" or "counter done"
into further rung logic yet — each `TON`/`TOF`/`CTU`/`CTD` stands on
its own. If a math instruction's destination tag ends up with a
divide-by-zero or similar defined runtime error, that tag's value is
left at its last good value and the error is recorded on the tag
(inspectable in code as `Tag.Fault`) rather than crashing the scan —
there is no way to see `Fault` over the TCP protocol in v1.0, only
from code that holds the `PlcController` directly (e.g. a test or an
embedding host).

### A larger worked example

This extends the quickstart example with a timer, a counter, and math
— confirmed to load cleanly against a real `plcemu` process the same
way §3's example does.

**CONTROL_LOGIC.json:**

```json
{
  "tags": [
    { "name": "Start_PB", "type": "BOOL", "initialValue": false },
    { "name": "Stop_PB", "type": "BOOL", "initialValue": false },
    { "name": "Motor_Run", "type": "BOOL", "initialValue": false },
    { "name": "Run_Timer", "type": "TIMER", "preset": 5000 },
    { "name": "Part_Sensor", "type": "BOOL", "initialValue": false },
    { "name": "Reset_PB", "type": "BOOL", "initialValue": false },
    { "name": "Parts_Made", "type": "COUNTER", "preset": 10 },
    { "name": "Batch_Total", "type": "DINT", "initialValue": 0 },
    { "name": "Batch_Full", "type": "BOOL", "initialValue": false }
  ],
  "rungs": [
    { "instructions": [
        { "op": "XIC", "operands": ["Start_PB"] },
        { "op": "XIO", "operands": ["Stop_PB"] },
        { "op": "OTE", "operands": ["Motor_Run"] }
    ] },
    { "instructions": [
        { "op": "XIC", "operands": ["Motor_Run"] },
        { "op": "TON", "operands": ["Run_Timer"] }
    ] },
    { "instructions": [
        { "op": "XIC", "operands": ["Part_Sensor"] },
        { "op": "CTU", "operands": ["Parts_Made"] }
    ] },
    { "instructions": [
        { "op": "XIC", "operands": ["Reset_PB"] },
        { "op": "RES", "operands": ["Parts_Made"] }
    ] },
    { "instructions": [
        { "op": "XIC", "operands": ["Part_Sensor"] },
        { "op": "ADD", "operands": ["Batch_Total", 1, "Batch_Total"] }
    ] },
    { "instructions": [
        { "op": "GEQ", "operands": ["Batch_Total", 100] },
        { "op": "OTE", "operands": ["Batch_Full"] }
    ] }
  ]
}
```

**NETWORK.json**, wiring a start button, a stop button, and a motor
relay to three of the tags above:

```json
{
  "components": [
    { "name": "StartButton", "driver": "DiscreteSensor", "tag": "Start_PB" },
    { "name": "StopButton", "driver": "DiscreteSensor", "tag": "Stop_PB" },
    { "name": "MotorRelay", "driver": "Relay", "tag": "Motor_Run" }
  ]
}
```

Read it top to bottom: rung 1 is a classic start/stop seal circuit
shape (minus true seal-in, since v1.0 has no OR-branch syntax — see
above); rung 2 runs a 5-second timer while the motor runs; rungs 3-4
count and reset parts made by a sensor; rung 5 tallies a running
batch total; rung 6 sets a "batch full" flag once the total reaches
100.

### Mapping the component network

Every NETWORK component just names a driver type and the CONTROL_LOGIC
tag(s) it's wired to — it carries no logic of its own; behavior lives
entirely in the driver implementation (§5). The two built-in driver
types (`DiscreteSensor`, `Relay`) both require binding to exactly one
`BOOL` tag; `plcemu` rejects the file at startup (§3) if a component's
tag doesn't exist, isn't `BOOL`, or the driver type name isn't
recognized.

## 5. Extending the system: adding a new driver

Adding a new component type never requires touching the scan engine
or any instruction class (`CORE-209`) — it's a small, self-contained
addition to `PlcEmulator.Drivers`.

1. **Implement the interface.** Add a new class to
   `src/PlcEmulator.Drivers/`, implementing
   `PlcEmulator.Core.Drivers.IDriver`:

   ```csharp
   void Bind(TagTable tags, NetworkComponentConfig config);
   void OnScanComplete();
   ```

   `Bind` runs once, when the driver is constructed — capture whatever
   tag(s) the driver needs from `config.Tags` here (and throw a
   `PlcEmulator.Config.ConfigValidationException` if the component's
   configuration doesn't make sense for this driver, the same way the
   built-in drivers do). `OnScanComplete` runs once per scan, after
   tag values settle, for any derived behavior your driver needs.

   If your device is a simple on/off device bound to exactly one
   `BOOL` tag (the common case — most real components are), you can
   extend `SingleTagDriverBase` instead of implementing `IDriver`
   directly and get the tag-binding/validation logic for free:

   ```csharp
   namespace PlcEmulator.Drivers;

   public sealed class PressureSensorDriver : SingleTagDriverBase
   {
       protected override string DriverTypeName => "PressureSensor";
   }
   ```

   (`IDriver` itself lives in `PlcEmulator.Core.Drivers`, not
   `PlcEmulator.Drivers` — that's deliberate: `PlcController` in
   `PlcEmulator.Core` needs the interface type without a project
   reference to `PlcEmulator.Drivers`. You never need to touch
   `PlcEmulator.Core` to add a driver; this is just where the
   interface it implements happens to be declared.)

2. **Register it in `DriverFactory`.** Add a driver-type-name constant
   and one `case` to the switch in
   `src/PlcEmulator.Drivers/DriverFactory.cs`:

   ```csharp
   public const string PressureSensor = "PressureSensor";
   // ...
   public static IDriver Create(string driverType) => driverType switch
   {
       DiscreteSensor => new DiscreteSensorDriver(),
       Relay => new RelayDriver(),
       PressureSensor => new PressureSensorDriver(),
       // ...
   };
   ```

3. **Reference it from NETWORK JSON.** Use the same string in a
   component's `"driver"` field, bound to a tag already defined in
   your CONTROL_LOGIC file:

   ```json
   { "name": "PressureSensor1", "driver": "PressureSensor", "tag": "High_Pressure_Alarm" }
   ```

4. **Rebuild** (`dotnet build PlcEmulator.sln`) and launch as in §3.
   Nothing outside `src/PlcEmulator.Drivers/DriverFactory.cs` and your
   new driver's own file needs to change — `PlcEmulator.Core`'s scan
   engine and instruction classes are untouched.
