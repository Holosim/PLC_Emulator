namespace PlcEmulator.Core.Instructions;

/// <summary>
/// Base for every MVP instruction whose CONTROL_LOGIC operand list is
/// exactly one tag reference: contacts (<c>XIC</c>, <c>XIO</c>), coil
/// (<c>OTE</c>), timers (<c>TON</c>, <c>TOF</c>), and counters
/// (<c>CTU</c>, <c>CTD</c>, <c>RES</c>). Parsing/shape is DATA-IN-101;
/// evaluation semantics land per-mnemonic with the CORE item named at
/// construction (see docs/RTVM.md).
/// </summary>
public abstract class SingleTagInstruction : IInstruction
{
    private readonly string _coreItem;

    protected SingleTagInstruction(string tagName, string coreItem)
    {
        TagName = tagName;
        _coreItem = coreItem;
    }

    /// <summary>The single tag this instruction operates on.</summary>
    public string TagName { get; }

    public abstract string Mnemonic { get; }

    /// <summary>
    /// Default stub for mnemonics whose evaluation semantics haven't
    /// landed yet — overridden per-mnemonic (e.g. <see cref="Ton"/>,
    /// <see cref="Tof"/>) as each one's CORE item is implemented.
    /// </summary>
    public virtual bool Evaluate(TagTable tags, bool rungState, TimeSpan elapsed) =>
        throw new NotImplementedException($"{Mnemonic}.Evaluate lands with {_coreItem}.");

    public override string ToString() => $"{Mnemonic}:{TagName}";

    /// <summary>
    /// Resolves <see cref="TagName"/>'s TIMER sub-state, for <see cref="Ton"/>/<see cref="Tof"/> (CORE-203/204).
    /// </summary>
    /// <exception cref="InvalidOperationException"><see cref="TagName"/> is not a TIMER-typed tag.</exception>
    protected TimerState RequireTimer(TagTable tags) =>
        tags.Get(TagName).Timer
        ?? throw new InvalidOperationException(
            $"Tag '{TagName}' is not a TIMER tag — {Mnemonic} requires a TIMER-typed operand (DATA-IN-100/101).");
}
