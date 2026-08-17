using System.Net;
using System.Net.Sockets;
using PlcEmulator.Config;
using PlcEmulator.Core;
using PlcEmulator.Network;

namespace PlcEmulator.Tests;

/// <summary>
/// Regression coverage for OUT-400 (docs/RTVM.md TP-400, already
/// <c>Verified</c>) under an OUT-403-style free-running broadcast load.
/// </summary>
/// <remarks>
/// Test Engineer's OUT-403 hand-off (issue #30) found that a
/// free-running scan loop calling <see cref="TcpJsonServer.Broadcast"/>
/// as fast as possible, with no delay between iterations, starved
/// <see cref="TcpJsonServer"/>'s accept thread of the lock it needed to
/// reject a second connection attempt — TP-400's "additional connect
/// attempts refused" behavior stalled indefinitely instead of rejecting
/// promptly. This test reproduces that load pattern directly (a tight
/// broadcast loop on a background thread, no <c>PlcController.RunScan</c>
/// needed since only the write path is under test) so the fix can't
/// silently regress again.
/// </remarks>
[TestClass]
public sealed class TcpJsonServerSingleClientTests
{
    private static PlcController BuildEmptyController()
    {
        return new PlcController(
            new ControlLogicDef { Tags = Array.Empty<TagDef>(), Rungs = Array.Empty<RungDef>() },
            new NetworkDef { Components = Array.Empty<NetworkComponentConfig>() },
            static driverType => throw new InvalidOperationException($"no NETWORK components in this test; unexpected driver type '{driverType}'"));
    }

    /// <summary>
    /// TP-400 regression: a second connection must still be rejected
    /// (accepted at the TCP layer, then closed immediately) promptly
    /// even while another thread is broadcasting as fast as possible
    /// with no delay between messages, matching the OUT-403 scan loop's
    /// actual cadence.
    /// </summary>
    [TestMethod]
    public void SecondConnection_StillRejectedPromptly_UnderFreeRunningBroadcastLoad()
    {
        var controller = BuildEmptyController();
        var server = new TcpJsonServer(controller);
        server.Start(0);

        using var stopSignal = new CancellationTokenSource();
        var broadcastThread = new Thread(() =>
        {
            var snapshot = controller.GetSnapshot();
            while (!stopSignal.IsCancellationRequested)
            {
                server.Broadcast(snapshot); // free-running, no delay — same cadence as Program.RunScanLoop
            }
        })
        {
            IsBackground = true,
            Name = "Test.FreeRunningBroadcast",
        };

        Thread? drainThread = null;
        try
        {
            using var client1 = new TcpClient();
            client1.Connect(IPAddress.Loopback, server.Port);
            var client1Stream = client1.GetStream();

            // client1 must actively drain its socket, or the server's
            // Write would eventually block on a full TCP send buffer —
            // masking the real bug (a tight, never-blocking lock
            // acquire/release loop starving a contended waiter) behind
            // a different, uninteresting one (a blocked writer holding
            // the lock). A discarded read loop matches how the OUT-403
            // hand-off's manual repro kept up ~877,000 msgs/2s.
            drainThread = new Thread(() =>
            {
                var buffer = new byte[4096];
                try
                {
                    while (client1Stream.Read(buffer, 0, buffer.Length) > 0)
                    {
                        // discard — this thread only exists to keep the
                        // server's writes from blocking on a full buffer
                    }
                }
                catch (Exception)
                {
                    // Stream torn down by client1's disposal at test end — expected.
                }
            })
            {
                IsBackground = true,
                Name = "Test.Client1Drain",
            };
            drainThread.Start();

            broadcastThread.Start();

            // Give the broadcast loop a moment to reach full, sustained
            // speed before the second connection attempt — the failure
            // mode under test only shows up once contention is real.
            Thread.Sleep(200);

            using var client2 = new TcpClient();
            client2.Connect(IPAddress.Loopback, server.Port);
            var client2Stream = client2.GetStream();
            client2Stream.ReadTimeout = 5000;

            // A rejected connection is closed by the server, so reading
            // from it returns 0 (EOF) rather than blocking or throwing.
            var buffer = new byte[16];
            var bytesRead = client2Stream.Read(buffer, 0, buffer.Length);

            Assert.AreEqual(0, bytesRead, "a second connection must be closed (EOF) promptly, even under a free-running broadcast load");
        }
        finally
        {
            stopSignal.Cancel();
            broadcastThread.Join(TimeSpan.FromSeconds(5));
            server.Stop(); // tears down client1's connection too, unblocking the drain thread's read
            drainThread?.Join(TimeSpan.FromSeconds(5));
        }
    }
}
