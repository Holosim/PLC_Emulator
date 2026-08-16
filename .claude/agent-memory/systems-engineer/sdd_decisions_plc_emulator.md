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
- **DELIV-900 vs. NFR-501 are verified on different schedules,
  deliberately (see SDD's "Target-platform verification strategy"
  section):** NFR-501 (Windows/Linux behavioral parity) gates *every*
  feature via a CI matrix (`ubuntu-latest` + `windows-latest`) because
  both runners are natively available at near-zero marginal cost.
  DELIV-900 (opens/builds cleanly as a Visual Studio solution) is a
  one-time late-stage consolidation task instead, because SDK-style
  `.csproj`/`.sln` (chosen from day one) is already VS-compatible, and
  actually opening the IDE is a human-facing check with no useful
  per-feature signal.
- **Dependency policy (NFR-502):** `System.Text.Json` (SDK-included)
  is not a third-party dependency and is the project's JSON library
  for both config files and the wire protocol.

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
