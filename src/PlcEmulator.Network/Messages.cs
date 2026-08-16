namespace PlcEmulator.Network;

/// <summary>
/// Wire schema for the TCP/JSON protocol's message <c>"type"</c>
/// discriminator (see docs/SDD.md, Interface Control Document).
/// </summary>
public static class MessageType
{
    public const string TagUpdate = "tag_update";
    public const string TagWrite = "tag_write";
    public const string ReadRequest = "read_request";
}

/// <summary>
/// Server-to-client message: <c>{"type":"tag_update","tags":{...}}</c>.
/// Sent on connect and after every scan cycle completes (DATA-OUT-301).
/// </summary>
public sealed class TagUpdateMessage
{
    public string Type { get; init; } = MessageType.TagUpdate;
    public required IReadOnlyDictionary<string, object> Tags { get; init; }
}

/// <summary>
/// Client-to-server message: <c>{"type":"tag_write","tags":{...}}</c>.
/// Applied at the start of the next scan (OUT-401).
/// </summary>
public sealed class TagWriteMessage
{
    public string Type { get; init; } = MessageType.TagWrite;
    public required IReadOnlyDictionary<string, object> Tags { get; init; }
}

/// <summary>
/// Client-to-server message: <c>{"type":"read_request"}</c>. A
/// one-shot client explicitly asking for a snapshot outside the
/// normal per-scan push.
/// </summary>
public sealed class ReadRequestMessage
{
    public string Type { get; init; } = MessageType.ReadRequest;
}
