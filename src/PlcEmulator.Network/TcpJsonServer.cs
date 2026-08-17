using PlcEmulator.Core;

namespace PlcEmulator.Network;

/// <summary>
/// Wraps exactly one <see cref="PlcController"/> reference in v1.0 and
/// enforces the single-client constraint (OUT-400) at the listener,
/// not inside the controller. One TCP connection, one
/// newline-delimited JSON message per line, UTF-8 (see docs/SDD.md,
/// Interface Control Document).
/// </summary>
public sealed class TcpJsonServer
{
    private readonly PlcController _controller;

    public TcpJsonServer(PlcController controller)
    {
        _controller = controller;
    }

    /// <summary>Starts listening on <paramref name="port"/> for the single v1.0 client (OUT-400).</summary>
    public void Start(int port)
    {
        throw new NotImplementedException("TcpJsonServer.Start is scaffolding only.");
    }

    /// <summary>
    /// Broadcasts a <c>tag_update</c> message to the connected client.
    /// The message text itself is fully implemented
    /// (<see cref="TagUpdateSerializer.Serialize"/>, DATA-OUT-301) —
    /// what remains scaffolding here is writing that text to an
    /// actual connected socket, which depends on the listener/
    /// connection state OUT-400 (issue #20) still needs to add.
    /// </summary>
    public void Broadcast(TagSnapshot snapshot)
    {
        _ = TagUpdateSerializer.Serialize(snapshot);
        throw new NotImplementedException("TcpJsonServer.Broadcast cannot transmit yet — no connected-client socket until OUT-400 (issue #20) lands.");
    }

    /// <summary>
    /// Handles one decoded client message: queues a <c>tag_write</c>
    /// (OUT-401) on <see cref="_controller"/>, or answers a
    /// <c>read_request</c> immediately.
    /// </summary>
    public void OnClientMessage(string rawJsonLine)
    {
        throw new NotImplementedException("TcpJsonServer.OnClientMessage is scaffolding only.");
    }
}
