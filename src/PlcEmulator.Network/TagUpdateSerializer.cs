using System.Text.Json;
using PlcEmulator.Core;

namespace PlcEmulator.Network;

/// <summary>
/// Converts a <see cref="TagSnapshot"/> (DATA-OUT-300) into the exact
/// wire-format JSON text for a <c>tag_update</c> message
/// (DATA-OUT-301), per docs/SDD.md's Interface Control Document:
/// <c>{"type":"tag_update","tags":{"&lt;name&gt;":&lt;value&gt;,...}}</c>,
/// with <c>tags</c> values as JSON <c>bool</c> for <c>BOOL</c> and
/// JSON <c>number</c> for <c>DINT</c>/<c>REAL</c> (matched here
/// because <see cref="Tag.Value"/> is always boxed as CLR
/// <see cref="bool"/>/<see cref="int"/>/<see cref="double"/> — see
/// <c>ConfigLoader.ParseInitialValue</c>). NDJSON framing (the
/// trailing <c>\n</c> that separates messages on the wire) is a
/// transport-layer concern applied by whoever writes this text to the
/// socket (OUT-400), not by this class.
/// </summary>
public static class TagUpdateSerializer
{
    /// <summary>
    /// Wire-format JSON options: camelCase property names, so the
    /// PascalCase DTO properties on <see cref="TagUpdateMessage"/>
    /// serialize as the ICD's lowercase <c>"type"</c>/<c>"tags"</c>
    /// keys without needing per-property <c>JsonPropertyName</c>
    /// attributes.
    /// </summary>
    private static readonly JsonSerializerOptions WireOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    /// <summary>
    /// Builds the <c>tag_update</c> JSON text for <paramref name="snapshot"/>
    /// (TP-301). <see cref="TagSnapshot.Values"/> already excludes
    /// timer/counter sub-elements (see its own remarks), so every
    /// entry here is emitted as-is.
    /// </summary>
    public static string Serialize(TagSnapshot snapshot)
    {
        var message = new TagUpdateMessage { Tags = snapshot.Values };
        return JsonSerializer.Serialize(message, WireOptions);
    }
}
