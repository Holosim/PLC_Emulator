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
