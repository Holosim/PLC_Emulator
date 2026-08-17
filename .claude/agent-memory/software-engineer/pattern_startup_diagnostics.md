---
name: pattern_startup_diagnostics
description: UI-002 structured startup diagnostics in Program.cs — literal-text matching for TP-003, printed before TCP listener startup
metadata:
  type: project
---

**What (issue #17):** `Program.cs` gained `PrintStartupDiagnostics`,
called from `Main`'s success path right after `ConfigLoader.Validate`
+ `PlcController` construction, replacing the old single-line load
summary from [[pattern_host_cli_startup]] (issue #16). Prints:
- `plcemu: {N} tags loaded from '{path}':` then one
  `plcemu:   {name} ({TYPE})` line per tag — type rendered via
  `TagTypeDef.ToString().ToUpperInvariant()` (`BOOL`/`DINT`/`REAL`/
  `TIMER`/`COUNTER`), matching CONTROL_LOGIC JSON's own casing rather
  than .NET enum-member casing.
- `plcemu: {M} components loaded from '{path}':` then one
  `plcemu:   {name} ({driver})` line per component.

**Why plural-always ("3 tags loaded", not "3 tag(s) loaded"):**
TP-003's expected-result text is the literal substring `3 tags loaded`
/ `2 components loaded`. Matched that verbatim (always plural noun,
even for a hypothetical count of 1) rather than adding singular/plural
grammar the RTVM text doesn't ask for — same "match the requirement
text, don't embellish" call as [[pattern_compare_instruction_numeric_matching]].

**Scope call:** requirement text says "lists each tag name/type and
component name/driver" — deliberately did *not* also list each
component's bound tag name(s), even though that data is available on
`NetworkComponentConfig.Tags` and would be easy/useful to add. Stayed
literal to the spec per "keep it scoped to what the issue asks."

**Still-open gap this ran into (not this issue's problem):**
`TcpJsonServer.Start` is still `NotImplementedException` (OUT-400,
issue #20 not yet landed as of 2026-08-17) — diagnostics print
successfully, then the process exits 1 right after. TP-003 only
covers the load/diagnostics phase, which completes before that point,
so it's unaffected. Same situation as UI-001/UI-003 in issue #16 —
worth remembering this gap will keep resurfacing on every UI-0xx-class
issue until #20 lands.

**No Host-level automated test project exists yet.** Process-level CLI
verification (TP-00x class) has been done directly by the Test
Engineer both in issue #16 and now #17, not via an SE-authored test
project — consistent pattern, not an oversight.
