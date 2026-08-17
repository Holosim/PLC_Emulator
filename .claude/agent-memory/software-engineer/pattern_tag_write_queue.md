---
name: pattern_tag_write_queue
description: OUT-401 (issue #21) QueueWrite/GetTagType design — JSON-type-conversion ownership split between Network and Core, plus the "no free-running scan loop" gap it surfaced
metadata:
  type: project
---

**What (issue #21):** `PlcController.QueueWrite(tagName, value)` (was
scaffolding since #16) now validates the tag exists (`KeyNotFoundException`
if not), is a scalar type (Bool/Dint/Real — `ArgumentException` for
Timer/Counter, which have no externally-writable value in v1.0 per the
ICD), and that `value`'s CLR type matches the tag's declared
`TagType` (`ArgumentException` on mismatch, e.g. `int` for a `BOOL`
tag) before enqueuing on the existing `WriteQueue` (`RunScan` already
drained it atomically at scan start since the WriteQueue scaffolding
landed — issue #21 only had to fill in `QueueWrite` and
`TcpJsonServer`'s dispatch, not `WriteQueue`/`RunScan` themselves).

**Conversion ownership split:** added `PlcController.GetTagType(tagName)`
so `TcpJsonServer` (Network layer, already depends on Core) can look
up a tag's declared type and convert the raw `JsonElement` itself
(`GetBoolean`/`GetInt32`/`GetDouble`, mirroring
`ConfigLoader.ParseInitialValue`) *before* calling `QueueWrite` with a
plain CLR `bool`/`int`/`double`. Deliberately kept `System.Text.Json`
types out of `PlcController`/`Core` entirely — Core's contract stays
"already-typed CLR values in, `KeyNotFoundException`/`ArgumentException`
out," matching the existing invariant from
[[pattern-tag-update-serialization]] that `Tag.Value` is always a
boxed CLR `bool`/`int`/`double`, never `JsonElement`.

**Multi-tag message semantics:** a `tag_write` message's `tags` object
is processed key-by-key, not as one all-or-nothing transaction — if
entry N fails (undefined tag or type mismatch), entries before it in
the same message are still queued; the exception just stops processing
the rest of that message's remaining entries and is caught/logged by
`HandleClient`'s existing per-line try/catch (same "one bad line never
crashes the listener" property from [[pattern_tcp_listener_single_client]]).

**Test synchronization trick worth reusing:** to prove "queued but not
yet applied" vs. "applied after RunScan" over a *real* socket
deterministically (no sleep/poll), send the `tag_write` line
immediately followed by a `read_request` line on the same connection,
then block on reading the `read_request`'s reply. Since
`TcpJsonServer` processes one client's messages strictly in the order
received (single read-loop thread), receiving that reply proves the
prior `tag_write` already finished server-side processing — a clean
ordering barrier without timing-based flakiness.

**Gap flagged (not blocking, told Test Engineer directly in the #21
hand-off rather than escalating to Systems Engineer since it didn't
block OUT-401 itself):** there is no background/free-running scan-loop
anywhere in the Host — `Program.cs` starts `TcpJsonServer` then blocks
forever; nothing calls `PlcController.RunScan()` automatically at any
cadence. No RTVM item assigns this, and `docs/IMPLEMENTATION_PLAN.md`'s
"the scan loop (#5)" phrase refers to the CORE-200 evaluate-once
algorithm, not a timer thread. TP-200/300/401 all test this by calling
`RunScan()` explicitly (test-harness-driven "next scan cycle"), which
is fine for automated verification but means a real running `plcemu`
process never actually re-scans on its own after startup. Worth a
Systems Engineer follow-up issue if a live end-to-end demo (not a test
harness) is ever required.
