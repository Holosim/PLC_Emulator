---
name: pattern_scan_loop_lock_contention_fix
description: OUT-403 regression fix (issue #30) — free-running Broadcast starved TcpJsonServer's accept lock, breaking OUT-400/TP-400; split into a volatile stream field + a separate write-only lock
metadata:
  type: project
---

**What (issue #30, follow-up to [[pattern_host_cli_startup]]'s
free-running scan loop and [[pattern_tcp_listener_single_client]]'s
`_clientLock`):** Test Engineer's first OUT-403 pass found a real
regression against the already-`Verified` OUT-400/TP-400: once
`Program.RunScanLoop` calls `TcpJsonServer.Broadcast` in a tight,
unthrottled loop (hundreds of thousands of times/sec, confirmed by
both SE's and TE's manual runs), `SendLine`'s old implementation held
`_clientLock` for the entire socket write — the *same* lock
`AcceptLoop` needs to register or reject an incoming connection. Under
that contention rate, a second client would sit accepted-but-unclosed
indefinitely (TE waited 20s, twice, with no result) instead of getting
the immediate EOF TP-400 expects.

**Fix:** decoupled the two concerns onto separate synchronization,
rather than just shrinking the critical section:
- `_clientStream` is now `volatile` and read in `SendLine` with *no
  lock at all* — a plain reference read needs nothing stronger than
  volatile to be safe, and `AcceptLoop`/`HandleClient`/`Stop()` still
  write it under `_clientLock` as before.
- A new `_writeLock` (that `AcceptLoop` never touches) serializes the
  actual socket write against other concurrent writers (the scan
  loop's `Broadcast` vs. a client thread answering its own
  `read_request` synchronously).

Net effect: `AcceptLoop`'s only lock (`_clientLock`) is now completely
decoupled from broadcast rate — mathematically, no write cadence can
starve it again, not just "less likely to."

**Hidden second bug the fix's own regression test exposed:**
`HandleClient`'s read loop only caught `IOException` around
`reader.ReadLine()`. The new test's `server.Stop()` (called in
`finally`, closing a still-connected client) can race a background
`HandleClient` thread blocked in `ReadLine()`, which then throws
`ObjectDisposedException` instead of `IOException` — uncaught, this
crashes the **entire test host process** (not just fails one test),
which is how it was caught (full suite went 119→108-then-crash when
this test file was added, but the same test passed fine every time
run in isolation — a strong signal to run the *whole* suite together,
not just the new file, before treating a test as done). Fixed the same
way `SendLine` already handled this exact race: catch
`ObjectDisposedException` right alongside `IOException`.

**A cautionary note on regression-test timing sensitivity:** a
straightforward port of TE's manual repro into an automated unit test
(tight `Broadcast` loop on a background thread + a second connection
attempt) did **not** reproduce the pre-fix starvation in this sandbox
— it passed instantly (single-digit ms) against the *old, buggy* code
across many repeated runs, even with a longer `ReadTimeout` and a
dedicated drain thread for client 1 (needed so the writer doesn't
instead block on a full TCP send buffer, which is a different,
uninteresting failure mode). The real repro only manifested via an
actual standalone `plcemu` process + a real Python TCP client
(confirmed post-fix: ~252k msgs/sec free-running, second client
rejected in 0.000s) — see [[pattern_host_cli_startup]] for the
`--control-logic`/`--network`/`--port` CLI shape used to launch it
manually, and note CONTROL_LOGIC's instruction JSON key is `"op"` with
a flat `"operands"` array of strings/numbers (**not** `"mnemonic"`/
`{"tag": ...}` objects — see `ConfigLoader.LoadControlLogic`'s XML doc
for the exact schema) and NETWORK's `"components"` array must be
non-empty (a `Relay`/`DiscreteSensor`-driven dummy component is enough
to satisfy `LoadNetwork`'s validation). The committed unit test is
kept anyway as a genuine positive-behavior regression net (it exercises
the same code path under the same load shape and could catch a coarser
reintroduction of the bug on a differently-scheduled CI runner), but
its pass on old code in *this* sandbox means it should not be treated
as proof the fix works — the real-process manual run is what actually
confirmed it here, and is worth reaching for whenever a lock-fairness
/ scheduler-timing bug needs verifying, not just a smaller in-process
repro.
