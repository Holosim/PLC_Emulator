---
name: requirements-traps-plc-domain
description: PLC/ladder-logic domain details the client's Project Definition didn't spell out, and how they were resolved without escalating
metadata:
  type: feedback
---

When a Project Definition names a PLC instruction family (e.g.
"counters (CTU/CTD)") but omits a companion instruction that's
required for it to function at all in real ladder logic (e.g. `RES`
to reset a counter), treat that as a domain-standard implementation
detail to resolve directly and document as an assumption — not a scope
ambiguity to escalate. Same for picking the concrete mnemonic set
behind a vague term like "basic compare/math" (resolved to Rockwell's
EQU/NEQ/GRT/LES/GEQ/LEQ and ADD/SUB/MUL/DIV) and the minimum tag type
set a stated instruction set implies (BOOL/DINT/REAL).

**Why:** the systems-engineer instructions say to escalate genuine
ambiguity (sizing limits, MVP-vs-later scope) rather than guess at
intent — but standard domain conventions needed to make a named
instruction functional aren't intent questions, they're engineering
completeness. Escalating these would have added an unnecessary
round-trip on something well-established in the PLC/GuardLogix domain.

**How to apply:** resolve this class of gap yourself, write it into
the RTVM as an explicit "Assumptions made while breaking down scope"
section (see `docs/RTVM.md` on PLC_Emulator for the template), and
flag it plainly in the handoff comment so the client/PM can correct it
if wrong — but don't block the RTVM approval on it. Reserve actual
escalation for true MVP-vs-later or sizing-limit ambiguity.

2026-08-17 (issue #13, CORE-207): same pattern for requirement-text
*wording* ambiguity discovered during implementation, not just missing
scope. "Two operands ... of matching numeric type" (CORE-207/CORE-208)
was implemented by Software Engineer as "both operands numeric"
(DINT/REAL tags may mix, promoted to `double`) rather than "identical
declared tag type required" — standard RSLogix compare/math behavior.
Confirmed this reading directly (no solutions-architect escalation
needed — it's a domain-convention question, not a scope one) and
reworded both CORE-207 and CORE-208's RTVM text to say so explicitly,
so the identical question doesn't resurface when CORE-208 (same
tag-or-literal operand shape) is implemented. General lesson: when a
Software Engineer's design-decision note is really "which of two
standard domain conventions did you mean," resolve and pre-clarify the
RTVM text for every sibling requirement with the same shape in the
same pass, not just the one that prompted the question.
