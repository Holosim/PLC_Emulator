namespace PlcEmulator.Core;

/// <summary>
/// Structured sub-elements of a counter tag (<c>CTU</c>/<c>CTD</c>),
/// per DATA-IN-100: <c>.PRE</c> (preset), <c>.ACC</c> (accumulated),
/// <c>.DN</c> (done bit).
/// </summary>
/// <remarks>
/// <c>Cu</c>/<c>Cd</c> below are an addition beyond DATA-IN-100's
/// documented 3-element model, needed by CORE-205/206 (issue #12):
/// <c>CTU</c>/<c>CTD</c> only increment/decrement on a <i>rising edge</i>
/// of their enable input, which requires remembering whether the
/// instruction was enabled on the <i>previous</i> scan — state that has
/// to live somewhere between <see cref="Instructions.IInstruction.Evaluate"/>
/// calls, since instructions themselves are documented stateless
/// (docs/SDD.md, Coding Standards / Instruction classes). This mirrors
/// the real Rockwell COUNTER data type, whose status word carries these
/// same two bits (<c>.CU</c>, <c>.CD</c>) alongside <c>.PRE</c>/<c>.ACC</c>/
/// <c>.DN</c> for exactly this reason — so it's a faithful extension of
/// the domain model, not an invented mechanism. Kept as two independent
/// bits (rather than one) because a <c>CTU</c> and a <c>CTD</c> can
/// legally target the same counter tag in the same program (an
/// up/down counter pair), and each needs its own edge memory.
/// Flagging for Systems Engineer sign-off / DATA-IN-100 and SDD.md
/// "Tag data model" update, same pattern as the <c>rungState</c>
/// addition to <c>IInstruction.Evaluate</c> in issue #9.
/// </remarks>
public sealed class CounterState
{
    public int Pre { get; set; }
    public int Acc { get; set; }
    public bool Dn { get; set; }

    /// <summary>Count-up enable bit: mirrors whether the <c>CTU</c> targeting this counter was enabled on the previous scan (rising-edge detection).</summary>
    public bool Cu { get; set; }

    /// <summary>Count-down enable bit: mirrors whether the <c>CTD</c> targeting this counter was enabled on the previous scan (rising-edge detection).</summary>
    public bool Cd { get; set; }
}
