namespace PlcEmulator.Core.Instructions;

/// <summary>Normally-open contact: true when <see cref="SingleTagInstruction.TagName"/>'s BOOL value is true. Evaluation semantics land with CORE-201.</summary>
public sealed class Xic : SingleTagInstruction
{
    public Xic(string tagName) : base(tagName, "CORE-201")
    {
    }

    public override string Mnemonic => "XIC";
}
