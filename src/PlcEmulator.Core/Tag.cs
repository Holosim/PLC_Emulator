namespace PlcEmulator.Core;

/// <summary>
/// One entry in a <see cref="TagTable"/>: a value plus optional
/// structured sub-elements for timer/counter tags (DATA-IN-100).
/// Tag names are preserved verbatim from CONTROL_LOGIC JSON rather
/// than translated (see docs/SDD.md, Coding Standards / Naming).
/// </summary>
public sealed class Tag
{
    public required string Name { get; init; }
    public required TagType Type { get; init; }

    /// <summary>
    /// Current scalar value (<c>bool</c>, <c>int</c>, or <c>double</c>)
    /// for <see cref="TagType.Bool"/>/<see cref="TagType.Dint"/>/
    /// <see cref="TagType.Real"/> tags. Unused (always <see langword="null"/>)
    /// for <see cref="TagType.Timer"/>/<see cref="TagType.Counter"/>
    /// tags — those carry their state in <see cref="Timer"/>/<see cref="Counter"/> instead.
    /// </summary>
    public object? Value { get; set; }

    /// <summary>Populated only for <see cref="TagType.Timer"/> tags (driven by a <c>TON</c>/<c>TOF</c> instruction).</summary>
    public TimerState? Timer { get; set; }

    /// <summary>Populated only for <see cref="TagType.Counter"/> tags (driven by a <c>CTU</c>/<c>CTD</c> instruction).</summary>
    public CounterState? Counter { get; set; }

    /// <summary>
    /// A description of the last defined runtime error this tag was the
    /// destination of, or <see langword="null"/> if it isn't currently
    /// faulted. This is how the Scan Engine surfaces "expected runtime
    /// conditions" such as <c>DIV</c>-by-zero (CORE-208) without
    /// throwing (docs/SDD.md, Coding Standards / Error handling): the
    /// offending instruction sets this instead of raising an exception,
    /// leaving <see cref="Value"/> at its last good value. Cleared
    /// (<see langword="null"/>) the next time the same destination is
    /// written successfully, so a fault is not permanent once the
    /// condition that caused it goes away.
    /// </summary>
    public string? Fault { get; set; }
}
