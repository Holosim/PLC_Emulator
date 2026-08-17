---
name: pattern_tcp_listener_single_client
description: TcpJsonServer.Start/Broadcast/OnClientMessage implementation shape for OUT-400's single-client TCP listener (issue #20)
metadata:
  type: project
---

**What (issue #20):** `TcpJsonServer.Start(port)` binds a `TcpListener`
synchronously (so a bind failure like "port in use" surfaces to
`Program.cs`'s existing try/catch immediately) then hands off to a
background accept-loop thread and returns — `Start` itself must not
block, since `Program.cs` calls it then blocks forever itself via
`Thread.Sleep(Timeout.Infinite)` (see
[[pattern_host_cli_startup]]). Single-client enforcement (OUT-400)
happens by *accepting* every connection at the TCP layer but only
keeping the first concurrently-outstanding one as "the" client —
every other connection attempt is accepted then immediately
`.Close()`d, which matches the SDD's connection-lifecycle diagram
("Connected → Connected: additional connect attempts refused", i.e.
still accepted at the socket level, refused at the app level). A
`_clientLock` guards the shared `_connectedClient`/`_clientStream`
fields since the accept thread and each per-client read thread
(one at a time, in v1.0) both touch them.

**Broadcast/OnClientMessage split:** `Broadcast(TagSnapshot)` (stub
left by DATA-OUT-301/#19) now actually writes
`TagUpdateSerializer.Serialize(...)` + `"\n"` to the connected
client's `NetworkStream` under `_clientLock`, silently dropping the
message if no client is connected (a live feed, not a mailbox).
`OnClientMessage` dispatches by `"type"`: `read_request` is answered
immediately via `Broadcast(_controller.GetSnapshot())` (this is what
TP-400 actually exercises — "send a read request"); `tag_write`
deliberately still `throw`s `NotImplementedException` pointing at
OUT-401/#21, since `PlcController.QueueWrite` is untouched scaffolding
and applying writes is squarely #21's scope, not #20's. Every
per-message dispatch is wrapped in its own try/catch in the read loop
so one malformed/unsupported line never takes the whole listener (or
process) down — general robustness, not literal OUT-402/#22 disconnect
*logging*, which is left for that issue.

**Confirmed empirically (manual TP-400 run against the real process,
`--port 5050`, real TCP client via Python):** initial `tag_update` on
connect, `read_request` reply, second connection rejected while first
stays alive and functional, malformed line logged not crashed. One
test-script gotcha hit along the way and worth remembering for anyone
writing socket-level manual/automated tests: Python's
`socket.makefile()` dups the file descriptor, so calling
`.close()` on the raw socket *without* also closing the file object
does not send FIN — looked exactly like a "disconnect not detected"
bug in the server until both were closed together.

**Left deliberately untouched:** `PlcController.QueueWrite` (still
throws — OUT-401/#21) and any "log the disconnect" text (OUT-402/#22).
Added a `Stop()` method and a `Port` property to `TcpJsonServer` purely
for test ergonomics (release the listener between test runs; support
`Start(0)`-then-read-actual-port to dodge CI port collisions) — not
tied to any RTVM item, just hygiene for whoever writes the automated
TP-400/401/402 tests next.
