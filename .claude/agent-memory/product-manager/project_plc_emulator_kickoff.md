---
name: project-plc-emulator-kickoff
description: Core product context for the PLC Emulator project, gathered at kickoff (issue #1)
metadata:
  type: project
---

The PLC_Emulator project is a server-based, extensible emulator that mimics an
Allen-Bradley GuardLogix-family safety PLC and its control network, used to
drive simulated theme-park ride/show systems (built in Unreal Engine or
Unity) for design validation, failure-state prediction, and as a training
tool for engineers learning PLC/automation-network design.

**Why:** The client explicitly wants two things bundled into one project:
(1) a functional simulator that can eventually be swapped for a real
GuardLogix PLC with minimal change to the target attraction — meaning
protocol/interface fidelity to real Rockwell hardware matters architecturally
even if not fully implemented in v1 — and (2) a codebase their own engineers
can learn from and extend, which I flagged as a Deliverable Requirement in
`docs/PROJECT_DEFINITION.md` (not just a feature list item).

**How to apply:** When scope or architecture questions come up later, weigh
them against both goals — "does this stay close enough to real GuardLogix
behavior to make future hardware swap-out plausible" and "does this stay
extensible/readable enough for the client's own engineers to build on." v1 is
CLI-only (GUI authoring tool deliberately deferred). Client is budget-conscious
about Anthropic/GitHub usage — keep interview rounds tight, propose defaults
they can confirm/veto rather than open-ended questions, per
[[feedback_propose_defaults]].

**v1.0 scope confirmed 2026-08-16** (client answered all 7 kickoff questions
in one reply, in full — no further rounds needed; a good example of
[[feedback_propose_defaults]] working as intended):
- Protocol: custom TCP/JSON between emulator and Unreal/Unity, NOT real
  EtherNet/IP CIP, for v1.
- Definitions: two separate custom JSON schemas — CONTROL_LOGIC (ladder +
  structured text) and NETWORK — NOT Rockwell `.L5X` import.
- MVP instruction set: discrete I/O, ladder rungs (contacts/coils), TON/TOF
  timers, CTU/CTD counters, basic compare/math, tag-based data model.
  Dual-channel safety instructions/motion control excluded from v1.0.
- Concurrency: v1.0 runs/tests exactly one PLC instance + one sim client at a
  time, BUT the architecture must support holding multiple distinct
  NETWORK/CONTROL_LOGIC configs simultaneously without a redesign later —
  this is a real architectural constraint for Systems Engineer/Solutions
  Architect to design around now, not a v1.0 feature to build/test.
- State: no persistence across restarts for v1.0 — in-memory, fresh load
  each launch.
- Stack: C#/.NET, JSON as data format. Avoid 3rd-party deps by default;
  allowed if a free industry-standard lib saves significant time, but only
  behind an interface for future replaceability.
- Deliverable: dev can use whatever's on the GitHub Actions VM, but the
  v1.0 deliverable must be refactored as a *final step* to compile as a
  Visual Studio project/solution — client's own engineers will extend it
  there. This is a scheduled late-stage v1.0 task, not indefinitely
  deferred.
- **Roadmap** (for scope consistency on future issues): v2.0 = safety I/O
  (E-stop/interlock) + true safety-rated logic; v3.0 = GUI authoring tool;
  v4.0+ = real CIP protocol + `.L5X` compatibility. If a future ask sounds
  like one of these, it's likely out of v1.0 scope by design, not an
  oversight.
