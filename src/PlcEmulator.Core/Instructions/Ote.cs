namespace PlcEmulator.Core.Instructions;

/// <summary>Output-energize coil: sets <see cref="SingleTagInstruction.TagName"/>'s BOOL value to the rung's evaluated logic each scan (non-latching). Evaluation semantics land with CORE-202.</summary>
public sealed class Ote : SingleTagInstruction
{
    public Ote(string tagName) : base(tagName, "CORE-202")
    {
    }

    public override string Mnemonic => "OTE";
}
