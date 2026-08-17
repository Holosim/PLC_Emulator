namespace PlcEmulator.Core.Instructions;

/// <summary>
/// Resets <see cref="SingleTagInstruction.TagName"/>'s COUNTER state
/// (<c>.ACC</c> to 0, <c>.DN</c> to false) whenever its own rung
/// condition is true (CORE-206) — a companion instruction to
/// <c>CTU</c>/<c>CTD</c>, not itself edge-triggered: it re-initializes
/// the counter for as long as it's energized, matching real
/// ladder-logic <c>RES</c> behavior. Also clears the internal
/// <see cref="CounterState.Cu"/>/<see cref="CounterState.Cd"/>
/// edge-detection bits (see <see cref="CounterState"/> remarks) so a
/// reset counter starts from a clean slate rather than carrying over
/// stale edge memory.
/// </summary>
public sealed class Res : SingleTagInstruction
{
    public Res(string tagName) : base(tagName, "CORE-206")
    {
    }

    public override string Mnemonic => "RES";

    /// <summary>Ignores <paramref name="elapsed"/> — not time-driven.</summary>
    public override bool Evaluate(TagTable tags, bool rungState, TimeSpan elapsed)
    {
        if (rungState)
        {
            var counter = RequireCounter(tags);
            counter.Acc = 0;
            counter.Dn = false;
            counter.Cu = false;
            counter.Cd = false;
        }

        // Action-type instruction: consume rungState for the side
        // effect above, pass it through unchanged (docs/SDD.md,
        // Coding Standards / Instruction classes).
        return rungState;
    }
}
