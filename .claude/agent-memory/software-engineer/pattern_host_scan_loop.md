---
name: pattern_host_scan_loop
description: OUT-403 (issue #30) free-running background scan loop in Program.cs — closes the gap flagged since #21/#22/#23
metadata:
  type: project
---

**What (issue #30):** `Program.cs`'s `Main` used to end with
`Thread.Sleep(Timeout.Infinite)` after `TcpJsonServer.Start()`
succeeded, with a comment claiming the scan loop belonged to
`TcpJsonServer` — that was never actually implemented anywhere (see
[[pattern_tag_write_queue]]'s "gap flagged" section, and
[[pattern_multi_controller_isolation_review]]/[[pattern_nfr501_consolidation_review]]
where it kept resurfacing as non-blocking). Replaced with a private
`RunScanLoop(PlcController, TcpJsonServer)` helper the Host owns:
`while (true) { controller.RunScan(); server.Broadcast(controller.GetSnapshot()); }`
with a per-iteration try/catch (logs to stderr, keeps looping — same
"one bad cycle doesn't kill the server" posture as
`TcpJsonServer.HandleClient`'s per-message try/catch). Runs on the
main thread itself (no new thread needed — it's what keeps the
process alive now, replacing the old sleep).

**Cadence — confirmed empirically, not just per the requirement
text:** genuinely "as fast as possible," no throttle. A live manual
TP-403 run (`--port 5099`, quickstart CONTROL_LOGIC/NETWORK from the
draft `docs/USER_GUIDE.md` on issue #29's branch) produced ~877,000
`tag_update` messages to one idle client in 2 seconds. This is
*correct* per the requirement (CORE-203/204's elapsed-time design
exists specifically because v1.0 has no fixed scan period) but is
worth flagging to Test Engineer: a naive test client that logs/parses
every line will be overwhelmed, so TP-403-style verification should
count-or-sample messages rather than process every single one, and a
future real driver/simulation client would likely want an idle
CPU/network profiling pass — not raised as a defect, just a
characteristic worth knowing going in.

**Confirmed no regression:** OUT-402 disconnect logging and OUT-400's
accept/broadcast behavior both still fire correctly with the loop
running continuously across a connect/disconnect cycle (manually
verified in the same live run). 119/119 automated tests unaffected —
none of them exercise `Program.Main` directly (they all drive
`PlcController`/`TcpJsonServer` in isolation), so this change had no
existing test surface to update.
