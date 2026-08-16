namespace PlcEmulator.Core.Instructions;

/// <summary>Output-energize coil: sets <see cref="SingleTagInstruction.TagName"/>'s BOOL value to the rung's evaluated logic each scan (non-latching, CORE-202).</summary>
public sealed class Ote : SingleTagInstruction
{
    public Ote(string tagName) : base(tagName, "CORE-202")
    {
    }

    public override string Mnemonic => "OTE";

    /// <summary>Action-type: writes the incoming rung state to the tag and returns it unchanged, so power flow continues past this coil (CORE-202).</summary>
    public override bool Evaluate(TagTable tags, bool rungState)
    {
        WriteBoolTag(tags, rungState);
        return rungState;
    }
}
