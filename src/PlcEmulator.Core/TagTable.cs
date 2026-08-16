namespace PlcEmulator.Core;

/// <summary>
/// The internal runtime state model: current values for every tag,
/// including timer/counter sub-elements (DATA-OUT-300). Owned by
/// exactly one <see cref="PlcController"/> — never shared, never a
/// static/singleton field (NFR-500).
/// </summary>
public sealed class TagTable
{
    private readonly Dictionary<string, Tag> _tags = new(StringComparer.Ordinal);

    /// <summary>Number of tags defined in this table.</summary>
    public int Count => _tags.Count;

    /// <summary>Every tag currently defined, in no particular order.</summary>
    public IEnumerable<Tag> AllTags => _tags.Values;

    /// <summary>Looks up a tag by name.</summary>
    /// <exception cref="KeyNotFoundException">No tag named <paramref name="name"/> is defined.</exception>
    public Tag Get(string name)
    {
        if (!_tags.TryGetValue(name, out var tag))
        {
            throw new KeyNotFoundException($"Tag '{name}' is not defined in this controller's tag table.");
        }

        return tag;
    }

    /// <summary>Looks up a tag by name without throwing if it is undefined.</summary>
    public bool TryGet(string name, out Tag? tag) => _tags.TryGetValue(name, out tag);

    /// <summary>Sets a tag's scalar value.</summary>
    /// <exception cref="KeyNotFoundException">No tag named <paramref name="name"/> is defined.</exception>
    public void Set(string name, object value)
    {
        Get(name).Value = value;
    }

    /// <summary>
    /// Adds a tag definition to the table. Used only while building the
    /// table from a validated CONTROL_LOGIC definition (see
    /// <see cref="ControlLogicBuilder"/>) — never at scan time, and
    /// never for a name that already exists (CONTROL_LOGIC-level
    /// duplicate-name rejection already happened during parsing, in
    /// <c>PlcEmulator.Config.ConfigLoader</c>; a duplicate reaching
    /// here would be an internal bug, not bad user input).
    /// </summary>
    /// <exception cref="InvalidOperationException">A tag with this name is already defined.</exception>
    internal void Define(Tag tag)
    {
        if (!_tags.TryAdd(tag.Name, tag))
        {
            throw new InvalidOperationException(
                $"Tag '{tag.Name}' was already defined — ConfigLoader should have rejected the duplicate.");
        }
    }
}
