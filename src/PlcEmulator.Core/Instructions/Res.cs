namespace PlcEmulator.Core.Instructions;

/// <summary>Resets <see cref="SingleTagInstruction.TagName"/>'s COUNTER state (<c>.ACC</c> to 0, <c>.DN</c> to false). Evaluation semantics land with CORE-206.</summary>
public sealed class Res : SingleTagInstruction
{
    public Res(string tagName) : base(tagName, "CORE-206")
    {
    }

    public override string Mnemonic => "RES";
}
