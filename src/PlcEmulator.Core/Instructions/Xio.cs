namespace PlcEmulator.Core.Instructions;

/// <summary>Normally-closed contact: true when <see cref="SingleTagInstruction.TagName"/>'s BOOL value is false. Evaluation semantics land with CORE-201.</summary>
public sealed class Xio : SingleTagInstruction
{
    public Xio(string tagName) : base(tagName, "CORE-201")
    {
    }

    public override string Mnemonic => "XIO";
}
