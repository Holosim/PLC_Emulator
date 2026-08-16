namespace PlcEmulator.Core.Instructions;

/// <summary>Normally-open contact: true when <see cref="SingleTagInstruction.TagName"/>'s BOOL value is true (CORE-201).</summary>
public sealed class Xic : SingleTagInstruction
{
    public Xic(string tagName) : base(tagName, "CORE-201")
    {
    }

    public override string Mnemonic => "XIC";

    /// <summary>Condition-type: ANDs the tag's current value into the incoming rung state and returns the result (CORE-201).</summary>
    public override bool Evaluate(TagTable tags, bool rungState) => rungState && ReadBoolTag(tags);
}
