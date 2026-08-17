---
name: pattern_disconnect_logging
description: OUT-402 disconnect logging added to TcpJsonServer.HandleClient (issue #22) — completes the gap #20 explicitly deferred
metadata:
  type: project
---

**What (issue #22):** `TcpJsonServer.HandleClient`
(`src/PlcEmulator.Network/TcpJsonServer.cs`) now logs a
`plcemu:`-prefixed line to **stdout** (`Console.WriteLine`, not
stderr — a client disconnecting is a normal runtime event, matching
UI-002's diagnostics style, not an error) in its `finally` block,
after releasing the single-client slot but after `client.Close()`:
`plcemu: client disconnected ({remote}); listening for a new
connection.`. Covers both disconnect paths uniformly (clean FIN via
`ReadLine()` returning null, and the `IOException`-on-mid-read case)
since both fall through to the same `finally`.

**Remote-endpoint capture gotcha:** `client.Client.RemoteEndPoint`
throws once the socket is disposed, and by the time the log line is
written the socket may already be closed. Captured it into a `remote`
local *before* entering the try block (via a
`TryDescribeRemoteEndPoint` helper that swallows any exception and
falls back to `"unknown endpoint"`), not at log-time.

**No new architecture** — `AcceptLoop`'s slot-release/keep-accepting
behavior was already correct from OUT-400/#20
([[pattern_tcp_listener_single_client]]); this issue was purely "add
the log line" per that pattern file's explicit note that logging was
left for #22.

**Manually verified (TP-402), same Python-client technique as #20:**
connect client 1 → disconnect (close both socket and its `makefile()`
object, per the FIN gotcha already in
[[pattern_tcp_listener_single_client]]) → disconnect line appears →
connect client 2 on the *same still-running process* → succeeds, gets
its own initial `tag_update`, no restart needed.
