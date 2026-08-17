namespace PlcEmulator.Core.Instructions;

/// <summary>Timer-on-delay against <see cref="SingleTagInstruction.TagName"/>'s TIMER state (CORE-203).</summary>
public sealed class Ton : SingleTagInstruction
{
    public Ton(string tagName) : base(tagName, "CORE-203")
    {
    }

    public override string Mnemonic => "TON";

    /// <summary>
    /// Action-type instruction (see <see cref="IInstruction.Evaluate"/>):
    /// while <paramref name="rungState"/> is true (enabled), <c>.ACC</c>
    /// accumulates real elapsed time and <c>.DN</c> becomes true once
    /// <c>.ACC &gt;= .PRE</c>; while disabled, <c>.ACC</c> resets to 0
    /// and <c>.DN</c> to false (CORE-203). Returns
    /// <paramref name="rungState"/> unchanged so power flow continues
    /// past this instruction to any further instructions on the rung.
    /// </summary>
    public override bool Evaluate(TagTable tags, bool rungState, TimeSpan elapsed)
    {
        var timer = RequireTimer(tags);

        if (rungState)
        {
            timer.En = true;
            timer.Acc += (int)elapsed.TotalMilliseconds;
            timer.Dn = timer.Acc >= timer.Pre;
        }
        else
        {
            timer.En = false;
            timer.Acc = 0;
            timer.Dn = false;
        }

        return rungState;
    }
}
