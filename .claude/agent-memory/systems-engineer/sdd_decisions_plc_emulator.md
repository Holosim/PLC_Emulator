---
name: sdd-decisions-plc-emulator
description: Architecture/interface decisions made in the PLC_Emulator SDD (issue #3) that later issues should reuse rather than re-derive
metadata:
  type: project
---

Key decisions locked into `docs/SDD.md` on 2026-08-16, for reuse when
writing Implementation Plan sequencing or answering Software
Engineer/Test Engineer questions on `[RTVM-014]`-style issues:

- **NFR-500 (concurrency-ready architecture) is satisfied by making
  all mutable runtime state instance fields of a `PlcController`
  class** — tag table, rungs, driver instances, write queue — with no
  static/singleton state anywhere. v1.0 only constructs one
  `PlcController`; the design doesn't prevent constructing more later.
  Instruction classes are stateless (operate only on the `TagTable`
  passed in), so they're safe to reuse across controller instances too.
- **TCP/JSON protocol (ICD in SDD.md):** newline-delimited JSON, one
  message per line. Three message types: `tag_update` (server→client,
  sent on connect and after every scan), `tag_write` (client→server,
  queued and applied atomically at next scan start), `read_request`
  (client→server, optional one-shot snapshot request). Exact JSON
  shape matches RTVM TP-301/TP-401.
- **Write-path threading:** network I/O thread never touches
  `TagTable` directly — writes go into a queue that `PlcController`
  drains at the start of its own scan. This keeps the scan loop
  single-threaded and avoids a second source of shared mutable state
  beyond the controller-isolation point above.
- **REVISED 2026-08-16 (issue #5, client decision):** NFR-501 and
  DELIV-900 are now verified together, once, as a single late-stage
  consolidation pass — NOT per-feature. The original plan below (kept
  for context) had NFR-501 gating every feature via a `ubuntu-latest` +
  `windows-latest` CI matrix; the client overrode this because the
  recurring cost of a second execution environment's setup/permission
  questions on every feature issue (proven out empirically by issue
  #5's own `workflows`-scope push rejection) outweighs the low
  Ubuntu/Windows divergence risk for this app. `docs/ci/*.yml` stay
  staged/undeployed in `docs/ci/`, not `.github/workflows/`, until the
  final consolidation issue. If a future `[RTVM-014]` issue asks "do I
  need to verify on Windows," the answer is **no** — that's a
  consolidation-issue concern, not a per-feature one.
- ~~DELIV-900 vs. NFR-501 are verified on different schedules,
  deliberately (see SDD's "Target-platform verification strategy"
  section): NFR-501 (Windows/Linux behavioral parity) gates *every*
  feature via a CI matrix (`ubuntu-latest` + `windows-latest`) because
  both runners are natively available at near-zero marginal cost.
  DELIV-900 (opens/builds cleanly as a Visual Studio solution) is a
  one-time late-stage consolidation task instead, because SDK-style
  `.csproj`/`.sln` (chosen from day one) is already VS-compatible, and
  actually opening the IDE is a human-facing check with no useful
  per-feature signal.~~ (superseded, see above)
- **Dependency policy (NFR-502):** `System.Text.Json` (SDK-included)
  is not a third-party dependency and is the project's JSON library
  for both config files and the wire protocol.
- **`IDriver` interface lives in `PlcEmulator.Core`** (namespace
  `PlcEmulator.Core.Drivers`), not `PlcEmulator.Drivers` — confirmed
  2026-08-16 (issue #5). `PlcEmulator.Drivers` holds only concrete
  implementations. Reason: `PlcController`/`TagTable` (in `Core`) are
  what drivers bind against, so the interface has to live with its
  consumer or `Core`↔`Drivers` would be a circular project reference.
  Standard dependency inversion; CORE-209 still holds since adding a
  driver only touches `PlcEmulator.Drivers`.
- **`IInstruction.Evaluate(TagTable tags, bool rungState)`** — revised
  2026-08-16 (issue #9) from the originally-documented single-parameter
  `Evaluate(TagTable tags)`. Software Engineer flagged that a coil
  (`OTE`) can't know whether the contacts preceding it in the same rung
  fired without a channel for rung power flow. `rungState` is standard
  ladder-logic rung-condition-in/rung-condition-out threading:
  condition-type instructions (contacts, compares) AND their own
  tag-based condition into the value and return it; action-type
  instructions (coils, timers, counters, math) consume it for their
  side effect and pass it through unchanged. `ScanEngine` seeds
  `rungState = true` at the start of every rung, not carried across
  rungs. Instructions remain fully stateless per-call (no change to the
  NFR-500 reuse-across-controllers property above). Confirmed/signed
  off in `docs/SDD.md` Architecture + Coding Standards sections.

- **NETWORK JSON wire shape (DATA-IN-102) — confirmed 2026-08-16 (issue
  #7):** top-level `{"components":[{"name","driver","tag"|"tags"},...]}`
  object, not a bare array. Accepts both singular `"tag"` (string) and
  plural `"tags"` (array), merged into one ordered list — both are
  valid, not just the singular form the RTVM's own TP-102 example
  happens to use. Signed off as proposed by Software Engineer; recorded
  in `docs/RTVM.md`'s Assumptions section too. Reuse this shape for
  CONTROL_LOGIC's wire format (issue #6) for consistency rather than
  inventing a different wrapper convention there.

**Why:** these are exactly the kind of decisions that are expensive to
change once Software Engineer starts building against them (wire
format, threading model, class boundaries) — recorded here so a future
`[RTVM-014]` query about "how should X talk to Y" gets answered
consistently with the SDD instead of re-litigated per issue.

**How to apply:** when Software Engineer asks an architecture question
on a feature issue, check here (and `docs/SDD.md` directly, which is
authoritative) before answering. If SDD.md changes, update this memory
to match — this is a snapshot of a point-in-time doc, not a substitute
for reading it. See [[rtvm-conventions-plc-emulator]] for the
requirement-ID side of the same project.
