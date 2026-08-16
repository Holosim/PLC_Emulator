namespace PlcEmulator.Core;

/// <summary>
/// A point-in-time, read-only copy of a <see cref="TagTable"/>'s
/// externally-relevant tag values (DATA-OUT-300), suitable for
/// serialization to a <c>tag_update</c> message (DATA-OUT-301).
/// Structured timer/counter sub-elements are not included — only
/// their parent tag's value, per the ICD (docs/SDD.md).
/// </summary>
public sealed class TagSnapshot
{
    // TODO: IReadOnlyDictionary&lt;string, object&gt; of tag name -> value
    // lands here (DATA-OUT-300/301).
}
