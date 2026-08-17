---
name: pattern-tag-update-serialization
description: DATA-OUT-301 scope split (serialize vs. transmit) and the camelCase JsonSerializerOptions needed to match the ICD's lowercase wire keys — reuse for OUT-400/OUT-401
metadata:
  type: project
---

Established on issue #19 (DATA-OUT-301, branch `issue-19`).

**The scope call:** DATA-OUT-301's requirement text and TP-301 are
about *format conversion only* — "serialize to the TCP/JSON output
schema" — not actual socket transmission. Split cleanly from OUT-400
(issue #20, the TCP listener/socket, `status:on-hold` at the time of
#19): `PlcEmulator.Network.TagUpdateSerializer.Serialize(TagSnapshot)`
is a pure function (`TagSnapshot` in, wire-format JSON `string` out),
fully unit-testable with no socket/listener involved.
`TcpJsonServer.Broadcast` stays `NotImplementedException` — it has
nowhere to write the bytes to yet — but its doc comment now names the
serializer as the intended call site once OUT-400 adds connection
state. Don't let "for transmission to the connected client" in the
requirement text pull socket work into a serialization-scoped issue.

**The JSON-options gotcha:** the existing `TagUpdateMessage`/
`TagWriteMessage` DTOs (`PlcEmulator.Network/Messages.cs`, scaffolded
since issue #5) have plain PascalCase properties (`Type`, `Tags`) with
no `[JsonPropertyName]` attributes. `ConfigLoader`'s `WireOptions`
(`PropertyNameCaseInsensitive = true`) only helps *deserializing*
lowercase input keys into PascalCase properties — it does nothing for
*serializing* PascalCase properties back out to lowercase keys. For
the output direction, `TagUpdateSerializer` uses its own
`JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }`
so `Type`/`Tags` emit as `"type"`/`"tags"` matching the ICD exactly.
Reuse this same options instance/pattern for any other
Network-project DTO that gets serialized *out* (e.g. if OUT-401 ever
needs to echo a `tag_write` ack, or a future outbound message type).

**Value-type confirmation:** `Tag.Value` is always boxed as CLR
`bool`/`int`/`double` (never `JsonElement` or string) —
`ConfigLoader.ParseInitialValue` converts at parse time via
`element.GetBoolean()/GetInt32()/GetDouble()`. That's why
`System.Text.Json` serializing the `object`-typed dictionary values in
[[pattern-tag-snapshot-scope]]'s `TagSnapshot.Values` produces correct
JSON `bool`/`number` output with zero extra conversion code.

**Test approach:** same rung-free-seed-and-scan pattern as
`PlcControllerSnapshotTests` (issue #18) — asserted exact JSON string
equality against TP-301's literal expected message, plus REAL-tag,
timer/counter-exclusion, and empty-snapshot edge cases.
