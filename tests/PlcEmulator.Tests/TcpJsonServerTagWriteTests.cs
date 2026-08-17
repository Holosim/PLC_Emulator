using System.Net;
using System.Net.Sockets;
using System.Text;
using PlcEmulator.Config;
using PlcEmulator.Core;
using PlcEmulator.Network;

namespace PlcEmulator.Tests;

/// <summary>
/// Verifies OUT-401 (docs/RTVM.md TP-401) end-to-end over the real
/// TCP/JSON transport (OUT-400): a connected client's <c>tag_write</c>
/// message is queued by <see cref="TcpJsonServer"/> and only takes
/// effect once the owning <see cref="PlcController"/>'s next scan runs
/// — never applied directly from the network thread (docs/SDD.md,
/// Architecture / write path note).
/// </summary>
/// <remarks>
/// v1.0 has no free-running background scan-loop thread anywhere in
/// the Host (see the issue #21/OUT-401 hand-off note) — "the next scan
/// cycle" is driven explicitly here by calling <see cref="PlcController.RunScan"/>
/// directly, the same way TP-200/TP-300's own tests do. Ordering
/// between "the write was queued" and "RunScan runs" is guaranteed
/// without a sleep/poll by sending a <c>read_request</c> right after
/// the <c>tag_write</c> on the same connection and waiting for its
/// reply: <see cref="TcpJsonServer"/> processes one client's messages
/// strictly in the order received (docs/SDD.md ICD, "Ordering
/// guarantees"), so receiving the reply proves the prior
/// <c>tag_write</c> already finished processing server-side.
/// </remarks>
[TestClass]
public sealed class TcpJsonServerTagWriteTests
{
    /// <summary>TP-401's exact scenario: rung `XIC(Start_PB) OTE(Motor_Run)` (TP-200 renamed).</summary>
    private static PlcController BuildTp401Controller()
    {
        var controlLogic = new ControlLogicDef
        {
            Tags = new[]
            {
                new TagDef { Name = "Start_PB", Type = TagTypeDef.Bool, InitialValue = false },
                new TagDef { Name = "Motor_Run", Type = TagTypeDef.Bool, InitialValue = false },
            },
            Rungs = new[]
            {
                new RungDef
                {
                    Instructions = new InstructionDef[]
                    {
                        new() { Mnemonic = "XIC", Operands = new[] { OperandDef.Tag("Start_PB") } },
                        new() { Mnemonic = "OTE", Operands = new[] { OperandDef.Tag("Motor_Run") } },
                    },
                },
            },
        };

        return new PlcController(
            controlLogic,
            new NetworkDef { Components = Array.Empty<NetworkComponentConfig>() },
            static driverType => throw new InvalidOperationException($"no NETWORK components in this test; unexpected driver type '{driverType}'"));
    }

    private static (TcpClient Client, StreamReader Reader, StreamWriter Writer) Connect(TcpJsonServer server)
    {
        var client = new TcpClient();
        client.Connect(IPAddress.Loopback, server.Port);
        var stream = client.GetStream();
        stream.ReadTimeout = 5000;
        var reader = new StreamReader(stream, Encoding.UTF8);
        var writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true, NewLine = "\n" };
        return (client, reader, writer);
    }

    /// <summary>TP-401: tag_write over the real socket is queued, not applied, until the controller's next scan.</summary>
    [TestMethod]
    public void TagWrite_OverRealSocket_QueuedThenAppliedOnNextScan()
    {
        var controller = BuildTp401Controller();
        var server = new TcpJsonServer(controller);
        server.Start(0);
        try
        {
            var (client, reader, writer) = Connect(server);
            using var _ = client;

            var onConnect = reader.ReadLine(); // initial tag_update (OUT-400 ICD)
            Assert.IsNotNull(onConnect);

            writer.WriteLine("{\"type\":\"tag_write\",\"tags\":{\"Start_PB\":true}}");
            writer.WriteLine("{\"type\":\"read_request\"}");

            // Reaching this reply proves the tag_write above already
            // finished processing server-side (strictly-ordered
            // per-client dispatch) — yet Start_PB must still read false,
            // since a queued write only takes effect at the next scan.
            var beforeScan = reader.ReadLine();
            Assert.IsNotNull(beforeScan);
            StringAssert.Contains(beforeScan, "\"Start_PB\":false");
            StringAssert.Contains(beforeScan, "\"Motor_Run\":false");

            controller.RunScan(); // "next scan cycle"

            writer.WriteLine("{\"type\":\"read_request\"}");
            var afterScan = reader.ReadLine();
            Assert.IsNotNull(afterScan);
            StringAssert.Contains(afterScan, "\"Start_PB\":true");
            StringAssert.Contains(afterScan, "\"Motor_Run\":true");
        }
        finally
        {
            server.Stop();
        }
    }

    /// <summary>An unrecognized/undefined tag name in a tag_write is reported (server-side log) but must not crash the connection — subsequent messages still work.</summary>
    [TestMethod]
    public void TagWrite_UndefinedTag_DoesNotCrashConnection()
    {
        var controller = BuildTp401Controller();
        var server = new TcpJsonServer(controller);
        server.Start(0);
        try
        {
            var (client, reader, writer) = Connect(server);
            using var _ = client;

            reader.ReadLine(); // initial tag_update

            writer.WriteLine("{\"type\":\"tag_write\",\"tags\":{\"Nonexistent_Tag\":true}}");
            writer.WriteLine("{\"type\":\"read_request\"}");

            var reply = reader.ReadLine();
            Assert.IsNotNull(reply, "connection must survive an undefined-tag tag_write and keep answering later messages");
            StringAssert.Contains(reply, "\"type\":\"tag_update\"");
        }
        finally
        {
            server.Stop();
        }
    }

    /// <summary>A value whose JSON kind doesn't match the target tag's declared type is rejected without crashing the connection.</summary>
    [TestMethod]
    public void TagWrite_TypeMismatch_DoesNotCrashConnection_AndIsNotApplied()
    {
        var controller = BuildTp401Controller();
        var server = new TcpJsonServer(controller);
        server.Start(0);
        try
        {
            var (client, reader, writer) = Connect(server);
            using var _ = client;

            reader.ReadLine(); // initial tag_update

            writer.WriteLine("{\"type\":\"tag_write\",\"tags\":{\"Start_PB\":1}}"); // BOOL tag, JSON number instead of bool
            writer.WriteLine("{\"type\":\"read_request\"}");

            var reply = reader.ReadLine();
            Assert.IsNotNull(reply);

            controller.RunScan();
            Assert.AreEqual(false, controller.GetSnapshot().Values["Start_PB"], "a rejected tag_write must not be queued");
        }
        finally
        {
            server.Stop();
        }
    }
}
