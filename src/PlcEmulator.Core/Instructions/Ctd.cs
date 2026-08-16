namespace PlcEmulator.Core.Instructions;

/// <summary>Count-down against <see cref="SingleTagInstruction.TagName"/>'s COUNTER state. Evaluation semantics land with CORE-206.</summary>
public sealed class Ctd : SingleTagInstruction
{
    public Ctd(string tagName) : base(tagName, "CORE-206")
    {
    }

    public override string Mnemonic => "CTD";
}
