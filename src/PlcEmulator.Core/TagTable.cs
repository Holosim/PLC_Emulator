namespace PlcEmulator.Core;

/// <summary>
/// The internal runtime state model: current values for every tag,
/// including timer/counter sub-elements (DATA-OUT-300). Owned by
/// exactly one <see cref="PlcController"/> — never shared, never a
/// static/singleton field (NFR-500).
/// </summary>
public sealed class TagTable
{
    // TODO: backing store (Dictionary&lt;string, Tag&gt;) lands with
    // real Get/Set semantics (DATA-OUT-300).

    /// <summary>Looks up a tag by name.</summary>
    public Tag Get(string name)
    {
        throw new NotImplementedException("TagTable.Get is scaffolding only.");
    }

    /// <summary>Sets a tag's scalar value.</summary>
    public void Set(string name, object value)
    {
        throw new NotImplementedException("TagTable.Set is scaffolding only.");
    }
}
