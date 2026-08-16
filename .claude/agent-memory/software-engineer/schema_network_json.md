---
name: schema-network-json
description: NETWORK JSON wire shape and DTO/domain-model split chosen while implementing DATA-IN-102 (issue #7) — reuse this shape for DATA-IN-103 cross-file validation and don't redesign it
metadata:
  type: project
---

Implemented in `src/PlcEmulator.Config/{NetworkDef,NetworkFileDto}.cs`,
`ConfigLoader.LoadNetwork` (issue #7, DATA-IN-102).

**Wire shape** (not specified anywhere in docs/SDD.md or
docs/PROJECT_DEFINITION.md beyond the RTVM's TP-102/TP-005 single-
component examples — this was my own inference, flagged for Systems
Engineer sign-off in the issue #7 hand-off, not yet confirmed as of
2026-08-16):

```json
{
  "components": [
    { "name": "ProxSensor1", "driver": "DiscreteSensor", "tag": "Start_PB" }
  ]
}
```

Top-level object with a `"components"` array (not a bare array) —
chosen for consistency with the likely CONTROL_LOGIC shape (`{"tags":
[...], "rungs": [...]}`, DATA-IN-100/101, issue #6) and future
extensibility. `"tag"` (singular string) and `"tags"` (array) are both
accepted and merged into one ordered list, since DATA-IN-102's
requirement text says "one or more" tags but every RTVM example uses
the singular form.

**DTO/domain split:** `NetworkFileDto`/`NetworkComponentDto` (both
`internal`, nullable wire-format types, one-to-one with the JSON) are
deserialized first, then validated and mapped onto the public,
immutable `NetworkDef`/`NetworkComponentConfig` (non-nullable,
`required` properties, `IReadOnlyList`). Nothing outside
`PlcEmulator.Config` ever sees the DTO types. Reuse this same pattern
for `ControlLogicDef` parsing (DATA-IN-100/101) if issue #6 hasn't
already established its own — keep the two consistent rather than
inventing a second convention.

**Cross-file validation (DATA-IN-103, issue #8):** `NetworkComponentConfig.Tags`
is exactly what needs checking against a loaded `ControlLogicDef`'s tag
names — one check per tag per component, not per component.
