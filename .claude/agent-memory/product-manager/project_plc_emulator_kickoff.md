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
