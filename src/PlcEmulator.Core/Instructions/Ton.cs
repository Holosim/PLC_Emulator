namespace PlcEmulator.Core.Instructions;

/// <summary>Timer-on-delay against <see cref="SingleTagInstruction.TagName"/>'s TIMER state. Evaluation semantics land with CORE-203.</summary>
public sealed class Ton : SingleTagInstruction
{
    public Ton(string tagName) : base(tagName, "CORE-203")
    {
    }

    public override string Mnemonic => "TON";
}
