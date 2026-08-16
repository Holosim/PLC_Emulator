---
name: pattern-compare-instruction-numeric-matching
description: CORE-207 CompareInstruction.Evaluate template-method design and the "matching numeric type" interpretation call — reuse for CORE-208 math instructions
metadata:
  type: project
---

Established on issue #13 (CORE-207, branch `issue-13`), landed on top of
[[pattern-scan-engine-rung-power-flow]]'s `IInstruction.Evaluate(tags, rungState)`
contract:

**Design:** `CompareInstruction` (base class for `Equ`/`Neq`/`Grt`/`Les`/`Geq`/`Leq`)
implements `Evaluate` itself as a template method —
`rungState && Compare(ResolveNumeric(Left, tags), ResolveNumeric(Right, tags))`.
Each subclass supplies only `protected abstract bool Compare(double, double)`
and its `Mnemonic`. No logic duplicated across the six mnemonics.

**Operand resolution ("matching numeric type," RTVM CORE-207's wording):**
a literal operand is always numeric; a tag operand must be `TagType.Dint` or
`TagType.Real` — a `BOOL`/`TIMER`/`COUNTER` tag operand throws
`InvalidOperationException`. **Interpretation call, flagged to Systems
Engineer in the #13 hand-off, not yet confirmed:** "matching numeric type"
was read as "both operands resolve to *some* number" (reject non-numeric
tags), NOT "both tag operands must share the identical DINT/REAL type" — a
DINT-vs-REAL tag comparison is allowed with implicit `double` promotion,
matching standard Rockwell/RSLogix behavior. If SE/Solutions Architect
instead wants DINT-vs-REAL rejected, it's a one-line addition
(`tagLeft.Type == tagRight.Type` check when both operands are tags) to
`CompareInstruction.ResolveNumeric`/`Evaluate`.

**Why this matters for CORE-208 (issue for `MathInstruction`):** `ADD`/`SUB`/
`MUL`/`DIV` have the identical "two operands, tag or literal" shape (see
`MathInstruction.cs`) and will face the exact same numeric-type-matching
question, plus the destination tag's type. Whatever answer comes back for
CORE-207 should be applied consistently there — check for a Systems
Engineer reply on issue #13 (or wherever it's relayed) before assuming.

**No exception type exists yet at the Core layer** for this kind of runtime
data error — `InvalidOperationException` was used directly (BCL type), same
class of "should have been caught earlier but wasn't" situation as
`TagTable.Get`'s `KeyNotFoundException` for an undefined tag name. No
CONTROL_LOGIC-load-time cross-validation exists yet that a compare/math
operand names a numeric tag — that gap would belong to DATA-IN-103 territory
if it ever gets tightened.
