namespace PlcEmulator.Core.Instructions;

/// <summary>
/// Count-down against <see cref="SingleTagInstruction.TagName"/>'s
/// COUNTER state (CORE-206): <c>.ACC</c> decrements by 1 on each
/// rising edge of its enable input; <c>.DN</c> is true when
/// <c>.ACC &lt;= 0</c>.
/// </summary>
public sealed class Ctd : SingleTagInstruction
{
    public Ctd(string tagName) : base(tagName, "CORE-206")
    {
    }

    public override string Mnemonic => "CTD";

    public override bool Evaluate(TagTable tags, bool rungState)
    {
        var counter = RequireCounter(tags);

        if (rungState && !counter.Cd)
        {
            // Rising edge of the enable input: count once.
            counter.Acc--;
        }

        counter.Cd = rungState;
        counter.Dn = counter.Acc <= 0;

        // Action-type instruction: consume rungState for the side
        // effect above, pass it through unchanged (docs/SDD.md,
        // Coding Standards / Instruction classes).
        return rungState;
    }
}
