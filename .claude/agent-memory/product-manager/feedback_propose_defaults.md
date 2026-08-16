---
name: feedback-propose-defaults
description: Interview style for this client — propose concrete defaults tagged [PROPOSED] rather than open-ended questions
metadata:
  type: feedback
---

When interviewing this client, draft `docs/PROJECT_DEFINITION.md` with
concrete [PROPOSED] defaults for open items (stack, protocol, scope cuts)
rather than asking open-ended questions with no anchor. Pair each with a
one-line rationale so the client can confirm/override in a single reply
instead of a back-and-forth.

**Why:** The client stated explicitly at kickoff (issue #1) that the project
runs on a throttled monthly Anthropic subscription and limited GitHub Actions
budget, and asked that the pipeline avoid "thrashing" from too many
concurrent/interrupted issue cycles. Minimizing interview rounds is a direct
budget concern, not just a style preference.

**How to apply:** At every stage where I'd normally ask the client an open
question (kickoff interview, scope refinements, escalations bounced up from
Solutions Architect), lead with a recommended default and ask for
confirmation/veto rather than posing the question bare. See
[[project_plc_emulator_kickoff]] for the project this was first applied to.
