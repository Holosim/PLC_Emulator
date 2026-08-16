namespace PlcEmulator.Config;

/// <summary>
/// One parsed CONTROL_LOGIC tag definition (DATA-IN-100): a name, a
/// type, and either a scalar initial value (<c>BOOL</c>/<c>DINT</c>/
/// <c>REAL</c>) or a preset (<c>TIMER</c>/<c>COUNTER</c>).
/// </summary>
public sealed class TagDef
{
    public required string Name { get; init; }
    public required TagTypeDef Type { get; init; }

    /// <summary>
    /// Scalar initial value for <c>BOOL</c> (<see cref="bool"/>),
    /// <c>DINT</c> (<see cref="int"/>), or <c>REAL</c> (<see cref="double"/>)
    /// tags. Always <see langword="null"/> for <c>TIMER</c>/<c>COUNTER</c>
    /// tags, which use <see cref="Preset"/> instead.
    /// </summary>
    public object? InitialValue { get; init; }

    /// <summary>
    /// The <c>.PRE</c> value for <c>TIMER</c>/<c>COUNTER</c> tags.
    /// Always <see langword="null"/> for scalar tags.
    /// </summary>
    public int? Preset { get; init; }
}
