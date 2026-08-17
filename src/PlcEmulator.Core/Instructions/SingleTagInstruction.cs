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
    /// Default: not yet implemented. Overridden by the mnemonics whose
    /// evaluation semantics have landed (currently <c>XIC</c>/<c>XIO</c>/
    /// <c>OTE</c> — CORE-201/202; <c>TON</c>/<c>TOF</c> — CORE-203/204;
    /// and <c>CTU</c>/<c>CTD</c>/<c>RES</c> — CORE-205/206).
    /// </summary>
    public virtual bool Evaluate(TagTable tags, bool rungState, TimeSpan elapsed) =>
        throw new NotImplementedException($"{Mnemonic}.Evaluate lands with {_coreItem}.");

    /// <summary>
    /// Reads <see cref="TagName"/>'s current value as a BOOL — used by
    /// the contact/coil mnemonics (<c>XIC</c>, <c>XIO</c>, <c>OTE</c>)
    /// that operate on <see cref="TagType.Bool"/> tags (CORE-201/202).
    /// </summary>
    /// <exception cref="KeyNotFoundException"><see cref="TagName"/> is not defined in <paramref name="tags"/>.</exception>
    /// <exception cref="InvalidOperationException"><see cref="TagName"/> is defined but is not a BOOL tag.</exception>
    protected bool ReadBoolTag(TagTable tags)
    {
        var tag = tags.Get(TagName);
        if (tag.Value is not bool value)
        {
            throw new InvalidOperationException(
                $"{Mnemonic}({TagName}) requires a BOOL tag, but '{TagName}' is {tag.Type}.");
        }

        return value;
    }

    /// <summary>Writes <paramref name="value"/> to <see cref="TagName"/>'s BOOL value (CORE-202).</summary>
    /// <exception cref="KeyNotFoundException"><see cref="TagName"/> is not defined in <paramref name="tags"/>.</exception>
    protected void WriteBoolTag(TagTable tags, bool value) => tags.Set(TagName, value);

    /// <summary>Looks up this instruction's tag and requires it to carry <see cref="CounterState"/> (i.e. be a <see cref="TagType.Counter"/> tag) — used by <see cref="Ctu"/>/<see cref="Ctd"/>/<see cref="Res"/>.</summary>
    protected CounterState RequireCounter(TagTable tags)
    {
        var tag = tags.Get(TagName);
        return tag.Counter ?? throw new InvalidOperationException(
            $"{Mnemonic}('{TagName}') requires a COUNTER-typed tag, but '{TagName}' is {tag.Type}.");
    }

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
