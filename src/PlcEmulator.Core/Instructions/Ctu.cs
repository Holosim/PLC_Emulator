namespace PlcEmulator.Core.Instructions;

/// <summary>
/// Count-up against <see cref="SingleTagInstruction.TagName"/>'s
/// COUNTER state (CORE-205): <c>.ACC</c> increments by 1 on each
/// rising edge of <paramref name="rungState"/>-equivalent enable
/// input; <c>.DN</c> becomes (and stays) true once <c>.ACC &gt;= .PRE</c>.
/// </summary>
public sealed class Ctu : SingleTagInstruction
{
    public Ctu(string tagName) : base(tagName, "CORE-205")
    {
    }

    public override string Mnemonic => "CTU";

    public override bool Evaluate(TagTable tags, bool rungState)
    {
        var counter = RequireCounter(tags);

        if (rungState && !counter.Cu)
        {
            // Rising edge of the enable input: count once.
            counter.Acc++;
        }

        counter.Cu = rungState;
        counter.Dn = counter.Acc >= counter.Pre;

        // Action-type instruction: consume rungState for the side
        // effect above, pass it through unchanged (docs/SDD.md,
        // Coding Standards / Instruction classes).
        return rungState;
    }
}
