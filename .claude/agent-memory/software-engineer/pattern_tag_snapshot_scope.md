---
name: pattern-tag-snapshot-scope
description: TagSnapshot/GetSnapshot() scope decision (scalar tags only, no timer/counter sub-elements) — reuse when DATA-OUT-301/OUT-401 land
metadata:
  type: project
---

Established on issue #18 (DATA-OUT-300, branch `issue-18`), but the
underlying design call was actually already baked into the codebase
from the original issue #5 scaffold (`TagSnapshot`'s doc comment and
docs/SDD.md ICD lines ~404-410) — issue #18 just implemented it, and
confirmed the interpretation rather than inventing it.

**The decision:** `PlcController.GetSnapshot()` / `TagSnapshot.Values`
is a flat `IReadOnlyDictionary<string, object>` of tag name -> value,
covering only `TagType.Bool`/`Dint`/`Real` tags. `Timer`/`Counter`
tags are *excluded entirely* — not even their `.DN` bit is surfaced —
because docs/SDD.md explicitly defers "externally-relevant value for a
structured tag" as an unresolved v1.0 non-gap ("Flagged in case a
future version needs sub-element visibility over the wire — not a
v1.0 gap, since no MVP scope item calls for it").

**Why this matters for DATA-OUT-300 specifically:** the requirement
text says the model "holds current values for every tag (including
timer/counter sub-elements)" — that clause is satisfied by
`TagTable`/`Tag`/`TimerState`/`CounterState` themselves (DATA-IN-100,
issue #6), which already store full `.PRE`/`.ACC`/`.DN`/`.EN` state
internally. `GetSnapshot()` is the *externally/rest-of-system-facing*
query surface, deliberately narrower — don't conflate the two when
reading the requirement literally.

**Reuse for:**
- DATA-OUT-301 (TCP/JSON `tag_update` serialization) — same
  `TagSnapshot.Values` map is exactly what should get JSON-serialized;
  no new "what fields go on the wire" decision needed there.
- OUT-401 (`tag_write` handling) — `PlcController.QueueWrite` is still
  `NotImplementedException` scaffolding; issue #18 deliberately left
  it alone since it's OUT-401's own scope, not DATA-OUT-300's.

**Test approach used (again):** TP-300 wants `Motor_Run`/
`Preset_Count` driven to their expected values *by a scan cycle*, but
real `XIC`/`OTE` (CORE-201/202, issue #10) still throw
`NotImplementedException`. Followed [[pattern-scan-engine-rung-power-flow]]'s
precedent: seed a rung-free `TagTable` with the values TP-300 expects
*after* the scan, run a no-op scan, then assert `GetSnapshot()`. Full
TP-300 needs re-verification once #10 lands.
