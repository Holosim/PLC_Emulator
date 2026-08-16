---
name: rtvm-conventions-plc-emulator
description: RTVM ID scheme, categories, and status conventions actually used on the PLC_Emulator project (issue #2)
metadata:
  type: project
---

For PLC_Emulator, the RTVM skeleton's default categories (UI, DATA-IN,
CORE, DATA-OUT, OUT, NFR, DELIV with the ranges already in
`docs/RTVM.md`) fit this project without modification — used as-is.
Category meaning as applied here: UI = CLI operator surface (no GUI in
v1), DATA-IN = CONTROL_LOGIC/NETWORK JSON schema + parse/validate, CORE
= scan-cycle engine + instruction set (XIC/XIO/OTE/TON/TOF/CTU/CTD/
compare/math + driver architecture), DATA-OUT = internal runtime tag
state model, OUT = TCP/JSON interface to the external sim client.

**Why:** the generic RTVM skeleton is meant to be reused verbatim when
it already fits — don't invent new categories reflexively.

**How to apply:** when returning to this project's RTVM (SDD issue,
Implementation Plan issue, or later `[RTVM-014]`-style feature issues),
reuse these category meanings rather than re-deriving them. See
[[requirements-traps-plc-domain]] for the specific ambiguities resolved
along the way.

**Status mapping for the `status:ready-for-rtvm-update` fast path
(added 2026-08-16, issue #6):** when Test Engineer's test passes and
hands back with `status:ready-for-rtvm-update`, set the RTVM item's
Status to `In Test` (not straight to `Verified`) and leave the
Commit(s) column blank. `Verified` + the commit SHA is only set later,
when CI/CD hands the issue back confirming the actual merge (see
"Receiving a commit confirmation from CI/CD" in
systems-engineer.md) — a passing test run isn't the same as a merged
commit, and the status vocabulary's `In Test` step exists to cover
exactly that gap.
