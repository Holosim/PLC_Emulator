namespace PlcEmulator.Core.Instructions;

/// <summary>Count-up against <see cref="SingleTagInstruction.TagName"/>'s COUNTER state. Evaluation semantics land with CORE-205.</summary>
public sealed class Ctu : SingleTagInstruction
{
    public Ctu(string tagName) : base(tagName, "CORE-205")
    {
    }

    public override string Mnemonic => "CTU";
}
