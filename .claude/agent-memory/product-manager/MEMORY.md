# Product Manager — memory

Durable knowledge only: stakeholder needs as understood so far, client
context, and questions that keep recurring. Task-by-task detail belongs
on the issue itself, not here. Curate this file as it grows — date each
entry, keep it terse, and fold near-duplicates together.

## Understanding of the product

- [PLC Emulator kickoff](project_plc_emulator_kickoff.md) — GuardLogix-style
  PLC emulator for theme-park ride/show simulation + engineer training tool;
  CLI-only v1; architecture should keep a future real-PLC swap plausible.

## Client / stakeholder context

- Budget-conscious on Anthropic/GitHub usage; wants tight interview rounds,
  not thrashing across many concurrent issues. See
  [feedback: propose defaults](feedback_propose_defaults.md).

## Open questions log

- (none open) — all 7 kickoff questions from issue #1 answered by client
  2026-08-16 in a single reply; see decisions below and
  [PLC Emulator kickoff](project_plc_emulator_kickoff.md) for the full list.

## Decisions made

- 2026-08-16 — Drafted `docs/PROJECT_DEFINITION.md` with [CONFIRMED] items
  from the kickoff issue body and [PROPOSED] defaults for open items,
  flagged "delivered codebase must be extensible by client's own engineers"
  as a Deliverable Requirement (not a feature) — driven by client's stated
  training-tool and extensibility goals. Scope not yet fully confirmed;
  issue #1 still open, waiting on client reply.
- 2026-08-16 — Client answered all 7 open questions in one reply; finalized
  `docs/PROJECT_DEFINITION.md` (all items flipped to [CONFIRMED], added
  Concurrency and Roadmap subsections). v1.0 scope now fully locked — see
  [PLC Emulator kickoff](project_plc_emulator_kickoff.md) for the details.
  Closed issue #1, opened "RTVM" issue labeled `agent:systems-engineer`.
