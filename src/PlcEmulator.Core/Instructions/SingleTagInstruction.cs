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
    /// Default is an unimplemented stub — overridden per-mnemonic as
    /// each one's owning CORE item lands (see <see cref="Ctu"/>,
    /// <see cref="Ctd"/>, <see cref="Res"/> for CORE-205/206).
    /// </summary>
    public virtual bool Evaluate(TagTable tags, bool rungState) =>
        throw new NotImplementedException($"{Mnemonic}.Evaluate lands with {_coreItem}.");

    /// <summary>Looks up this instruction's tag and requires it to carry <see cref="CounterState"/> (i.e. be a <see cref="TagType.Counter"/> tag) — used by <see cref="Ctu"/>/<see cref="Ctd"/>/<see cref="Res"/>.</summary>
    protected CounterState RequireCounter(TagTable tags)
    {
        var tag = tags.Get(TagName);
        return tag.Counter ?? throw new InvalidOperationException(
            $"{Mnemonic}('{TagName}') requires a COUNTER-typed tag, but '{TagName}' is {tag.Type}.");
    }

    public override string ToString() => $"{Mnemonic}:{TagName}";
}
