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

- 2026-08-16 (issue #1, kickoff): protocol fidelity to real Allen-Bradley
  EtherNet/IP CIP vs. simpler custom protocol for v1; ladder-logic definition
  format (custom JSON vs. Rockwell `.L5X`); MVP instruction/safety-logic
  scope; concurrency (single vs. multi-instance); state persistence; language/
  stack choice; deliverable form (IDE-ready project vs. buildable-from-source).
  Answers pending from client as of this date.

## Decisions made

- 2026-08-16 — Drafted `docs/PROJECT_DEFINITION.md` with [CONFIRMED] items
  from the kickoff issue body and [PROPOSED] defaults for open items,
  flagged "delivered codebase must be extensible by client's own engineers"
  as a Deliverable Requirement (not a feature) — driven by client's stated
  training-tool and extensibility goals. Scope not yet fully confirmed;
  issue #1 still open, waiting on client reply.
