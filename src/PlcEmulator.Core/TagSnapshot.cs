namespace PlcEmulator.Core;

/// <summary>
/// A point-in-time, read-only copy of a <see cref="TagTable"/>'s
/// externally-relevant tag values (DATA-OUT-300), suitable for
/// serialization to a <c>tag_update</c> message (DATA-OUT-301).
/// Structured timer/counter sub-elements are not included — only
/// their parent tag's value, per the ICD (docs/SDD.md) — so only
/// <see cref="TagType.Bool"/>/<see cref="TagType.Dint"/>/
/// <see cref="TagType.Real"/> tags appear here in v1.0.
/// </summary>
public sealed class TagSnapshot
{
    private readonly IReadOnlyDictionary<string, object> _values;

    /// <summary>
    /// Wraps an already-built name-to-value map. Built only by
    /// <see cref="PlcController.GetSnapshot"/> from its own
    /// <see cref="TagTable"/> — never constructed directly against a
    /// <see cref="TagTable"/> owned by a different controller
    /// (NFR-500).
    /// </summary>
    public TagSnapshot(IReadOnlyDictionary<string, object> values)
    {
        _values = values;
    }

    /// <summary>Every externally-relevant tag name mapped to its current value, at the moment the snapshot was taken.</summary>
    public IReadOnlyDictionary<string, object> Values => _values;

    /// <summary>Looks up one tag's value in this snapshot without throwing if it is absent.</summary>
    public bool TryGetValue(string tagName, out object? value)
    {
        if (_values.TryGetValue(tagName, out var found))
        {
            value = found;
            return true;
        }

        value = null;
        return false;
    }
}
