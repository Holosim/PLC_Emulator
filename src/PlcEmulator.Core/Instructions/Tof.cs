namespace PlcEmulator.Core.Instructions;

/// <summary>Timer-off-delay against <see cref="SingleTagInstruction.TagName"/>'s TIMER state (CORE-204).</summary>
public sealed class Tof : SingleTagInstruction
{
    public Tof(string tagName) : base(tagName, "CORE-204")
    {
    }

    public override string Mnemonic => "TOF";

    /// <summary>
    /// Action-type instruction (see <see cref="IInstruction.Evaluate"/>):
    /// while <paramref name="rungState"/> is true (enabled), <c>.DN</c>
    /// is true immediately and <c>.ACC</c> holds at 0; on disable,
    /// <c>.ACC</c> starts accumulating real elapsed time from 0 and
    /// <c>.DN</c> remains true until <c>.ACC &gt;= .PRE</c>, then goes
    /// false (CORE-204). Returns <paramref name="rungState"/> unchanged
    /// so power flow continues past this instruction to any further
    /// instructions on the rung.
    /// </summary>
    public override bool Evaluate(TagTable tags, bool rungState, TimeSpan elapsed)
    {
        var timer = RequireTimer(tags);

        if (rungState)
        {
            timer.En = true;
            timer.Acc = 0;
            timer.Dn = true;
        }
        else
        {
            timer.En = false;
            timer.Acc += (int)elapsed.TotalMilliseconds;
            timer.Dn = timer.Acc < timer.Pre;
        }

        return rungState;
    }
}
