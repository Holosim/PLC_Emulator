using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
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

    /// <summary>
    /// Guards every field below (listener, connected-client state) that
    /// the accept thread and the per-client read thread can both touch,
    /// so "is a client currently connected" is never read/written from
    /// two threads at once (see docs/SDD.md's network-thread note under
    /// Architecture).
    /// </summary>
    private readonly object _clientLock = new();

    private TcpListener? _listener;
    private TcpClient? _connectedClient;
    private NetworkStream? _clientStream;
    private Thread? _acceptThread;
    private volatile bool _stopping;

    public TcpJsonServer(PlcController controller)
    {
        _controller = controller;
    }

    /// <summary>
    /// The actual port bound by <see cref="Start"/>, once it has run —
    /// mainly useful for tests that request an OS-assigned port
    /// (<c>Start(0)</c>) to avoid colliding with other listeners.
    /// </summary>
    public int Port => _listener is { } listener ? ((IPEndPoint)listener.LocalEndpoint).Port : 0;

    /// <summary>
    /// Starts listening on <paramref name="port"/> for the single v1.0
    /// client (OUT-400). Binds and starts the listener synchronously —
    /// so a bind failure (e.g. the port is already in use) surfaces to
    /// the caller immediately, matching <c>Program.cs</c>'s fail-fast
    /// startup handling — then hands connection acceptance off to a
    /// background thread and returns; <c>plcemu</c> is a long-running
    /// server, so <see cref="Start"/> itself must not block.
    /// </summary>
    public void Start(int port)
    {
        if (_listener is not null)
        {
            throw new InvalidOperationException("TcpJsonServer.Start has already been called.");
        }

        var listener = new TcpListener(IPAddress.Any, port);
        listener.Start();
        _listener = listener;
        _stopping = false;

        _acceptThread = new Thread(AcceptLoop) { IsBackground = true, Name = "TcpJsonServer.Accept" };
        _acceptThread.Start();
    }

    /// <summary>
    /// Stops accepting new connections and closes the current client
    /// connection, if any. Not exercised by any v1.0 requirement
    /// directly (the CLI process runs until killed), but needed so
    /// tests that call <see cref="Start"/> can release the port
    /// afterward instead of leaking a listener thread per test.
    /// </summary>
    public void Stop()
    {
        _stopping = true;
        _listener?.Stop();

        lock (_clientLock)
        {
            _connectedClient?.Close();
            _connectedClient = null;
            _clientStream = null;
        }

        _listener = null;
    }

    /// <summary>
    /// Sends a <c>tag_update</c> message (DATA-OUT-301) to the
    /// currently connected client, if any. Silently drops the message
    /// when no client is connected right now — this is a live feed,
    /// not a mailbox with delivery guarantees; the client's own
    /// connect-time snapshot (see <see cref="AcceptLoop"/>) is what
    /// keeps a newly-connected client from missing state permanently.
    /// </summary>
    public void Broadcast(TagSnapshot snapshot)
    {
        SendLine(TagUpdateSerializer.Serialize(snapshot));
    }

    /// <summary>
    /// Handles one decoded client message: answers a
    /// <c>read_request</c> immediately with the current snapshot, or
    /// queues a <c>tag_write</c>'s tag values for the controller's next
    /// scan (OUT-401). Malformed JSON, an unrecognized <c>type</c>, an
    /// undefined tag name, or a value that doesn't match its tag's
    /// declared type also throws — callers (see <see cref="HandleClient"/>)
    /// catch per message so one bad line never takes the listener down.
    /// </summary>
    public void OnClientMessage(string rawJsonLine)
    {
        using var document = JsonDocument.Parse(rawJsonLine);
        var root = document.RootElement;

        if (!root.TryGetProperty("type", out var typeElement) || typeElement.ValueKind != JsonValueKind.String)
        {
            throw new FormatException("Client message is missing a string 'type' field.");
        }

        var type = typeElement.GetString();

        switch (type)
        {
            case MessageType.ReadRequest:
                // A one-shot client explicitly asking for a snapshot
                // outside the normal per-scan push (docs/SDD.md ICD) —
                // answered with the same tag_update wire format
                // (DATA-OUT-301) as that regular push.
                Broadcast(_controller.GetSnapshot());
                break;

            case MessageType.TagWrite:
                ApplyTagWrite(root);
                break;

            default:
                throw new FormatException($"Unrecognized client message type: '{type}'.");
        }
    }

    /// <summary>
    /// Extracts the <c>tags</c> object from a decoded <c>tag_write</c>
    /// message and queues each entry on <see cref="PlcController.QueueWrite"/>
    /// (OUT-401) — applied atomically at the start of the controller's
    /// next scan, never here on the network thread (see docs/SDD.md,
    /// Architecture / write path note). Each entry's JSON value is
    /// converted to the CLR type matching its tag's declared
    /// <see cref="TagType"/> (queried via <see cref="PlcController.GetTagType"/>),
    /// the same tag-type-driven conversion <c>ConfigLoader.ParseInitialValue</c>
    /// uses for CONTROL_LOGIC's <c>initialValue</c>. An undefined tag
    /// name or a value that doesn't match the declared type throws,
    /// which stops processing any remaining entries in the same message
    /// — entries already queued before the failing one still apply at
    /// the next scan; this is not treated as an all-or-nothing
    /// transaction across one message.
    /// </summary>
    private void ApplyTagWrite(JsonElement root)
    {
        if (!root.TryGetProperty("tags", out var tagsElement) || tagsElement.ValueKind != JsonValueKind.Object)
        {
            throw new FormatException("Client 'tag_write' message is missing an object 'tags' field.");
        }

        foreach (var tagEntry in tagsElement.EnumerateObject())
        {
            var tagName = tagEntry.Name;
            var tagType = _controller.GetTagType(tagName); // throws KeyNotFoundException for an undefined tag
            var value = ConvertWriteValue(tagType, tagEntry.Value, tagName);
            _controller.QueueWrite(tagName, value);
        }
    }

    /// <summary>
    /// Converts one <c>tag_write</c> entry's raw JSON value to the CLR
    /// type (<c>bool</c>/<c>int</c>/<c>double</c>) matching
    /// <paramref name="tagType"/>, per the ICD ("<c>tags</c> values are
    /// JSON <c>bool</c> for <c>BOOL</c>, JSON <c>number</c> for
    /// <c>DINT</c>/<c>REAL</c>"). Timer/Counter tags have no
    /// externally-writable scalar value in v1.0.
    /// </summary>
    private static object ConvertWriteValue(TagType tagType, JsonElement value, string tagName)
    {
        try
        {
            return tagType switch
            {
                TagType.Bool => value.GetBoolean(),
                TagType.Dint => value.GetInt32(),
                TagType.Real => value.GetDouble(),
                _ => throw new FormatException(
                    $"Tag '{tagName}' is a {tagType} tag and has no externally-writable scalar value " +
                    "(see docs/SDD.md ICD)."),
            };
        }
        catch (InvalidOperationException ex)
        {
            throw new FormatException(
                $"tag_write value for tag '{tagName}' does not match its declared type ({tagType}).", ex);
        }
    }

    /// <summary>
    /// Runs on a dedicated background thread for the lifetime of the
    /// server: accepts every incoming connection, but only the first
    /// one concurrently outstanding becomes "the" client — every other
    /// connection attempt is accepted at the TCP layer and then closed
    /// immediately (OUT-400's single-client constraint, matching the
    /// "Connected --&gt; Connected: additional connect attempts
    /// refused" transition in docs/SDD.md's connection-lifecycle
    /// diagram).
    /// </summary>
    private void AcceptLoop()
    {
        while (!_stopping)
        {
            TcpClient incoming;
            try
            {
                incoming = _listener!.AcceptTcpClient();
            }
            catch (Exception ex)
            {
                if (_stopping)
                {
                    return; // Stop() tore the listener down out from under a pending Accept — expected.
                }

                Console.Error.WriteLine($"plcemu: TCP accept loop stopped unexpectedly: {ex.Message}");
                return;
            }

            bool accepted;
            lock (_clientLock)
            {
                accepted = _connectedClient is null;
                if (accepted)
                {
                    _connectedClient = incoming;
                    _clientStream = incoming.GetStream();
                }
            }

            if (!accepted)
            {
                incoming.Close();
                continue;
            }

            // Serves as the client's initial snapshot (ICD: "Immediately on connect").
            Broadcast(_controller.GetSnapshot());

            var clientThread = new Thread(() => HandleClient(incoming))
            {
                IsBackground = true,
                Name = "TcpJsonServer.Client",
            };
            clientThread.Start();
        }
    }

    /// <summary>
    /// Reads newline-delimited JSON messages from one connected client
    /// until it disconnects, dispatching each through
    /// <see cref="OnClientMessage"/>. Releases the single-client slot
    /// on the way out either way, so a later connection attempt can
    /// succeed, and logs the disconnect (OUT-402, per UI-002's
    /// diagnostics style: a plain <c>plcemu:</c>-prefixed line to
    /// stdout, not stderr — a client disconnecting is a normal runtime
    /// event, not an error) before returning control to
    /// <see cref="AcceptLoop"/>, which keeps running scans and
    /// accepting new connections regardless (docs/SDD.md's
    /// connection-lifecycle diagram: <c>Connected -&gt; Listening</c>).
    /// </summary>
    private void HandleClient(TcpClient client)
    {
        // Captured up front: once the client disconnects, the socket
        // may already be torn down by the time the log line is
        // written, and RemoteEndPoint throws on a disposed socket.
        var remote = TryDescribeRemoteEndPoint(client);

        try
        {
            using var reader = new StreamReader(client.GetStream(), Encoding.UTF8);
            string? line;
            while ((line = reader.ReadLine()) is not null)
            {
                try
                {
                    OnClientMessage(line);
                }
                catch (Exception ex)
                {
                    // A malformed/unsupported message must not take the
                    // whole listener (or process) down.
                    Console.Error.WriteLine($"plcemu: error handling client message: {ex.Message}");
                }
            }
        }
        catch (IOException)
        {
            // Client dropped the connection mid-read; treated the same
            // as a clean disconnect by the cleanup below.
        }
        finally
        {
            lock (_clientLock)
            {
                if (ReferenceEquals(_connectedClient, client))
                {
                    _connectedClient = null;
                    _clientStream = null;
                }
            }

            client.Close();

            Console.WriteLine($"plcemu: client disconnected ({remote}); listening for a new connection.");
        }
    }

    /// <summary>
    /// Best-effort description of a client's remote endpoint for the
    /// OUT-402 disconnect log line. Falls back to a placeholder rather
    /// than letting a diagnostics-only lookup throw and mask the real
    /// disconnect handling.
    /// </summary>
    private static string TryDescribeRemoteEndPoint(TcpClient client)
    {
        try
        {
            return client.Client.RemoteEndPoint?.ToString() ?? "unknown endpoint";
        }
        catch (Exception)
        {
            return "unknown endpoint";
        }
    }

    /// <summary>
    /// Writes one NDJSON line (UTF-8, no BOM, <c>\n</c>-terminated per
    /// the ICD transport note) to the currently connected client, if
    /// any. Holds <see cref="_clientLock"/> for the whole write so a
    /// concurrent disconnect can't be torn out from under it mid-write.
    /// </summary>
    private void SendLine(string text)
    {
        var bytes = Encoding.UTF8.GetBytes(text + "\n");

        lock (_clientLock)
        {
            if (_clientStream is null)
            {
                return;
            }

            try
            {
                _clientStream.Write(bytes, 0, bytes.Length);
                _clientStream.Flush();
            }
            catch (IOException)
            {
                // Write raced a disconnect; HandleClient's own read-side
                // cleanup releases the slot.
            }
            catch (ObjectDisposedException)
            {
            }
        }
    }
}
