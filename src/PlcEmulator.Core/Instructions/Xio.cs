namespace PlcEmulator.Core.Instructions;

/// <summary>Normally-closed contact: true when <see cref="SingleTagInstruction.TagName"/>'s BOOL value is false (CORE-201).</summary>
public sealed class Xio : SingleTagInstruction
{
    public Xio(string tagName) : base(tagName, "CORE-201")
    {
    }

    public override string Mnemonic => "XIO";

    /// <summary>Condition-type: ANDs the tag's negated current value into the incoming rung state and returns the result (CORE-201). Ignores <paramref name="elapsed"/> — not time-driven.</summary>
    public override bool Evaluate(TagTable tags, bool rungState, TimeSpan elapsed) => rungState && !ReadBoolTag(tags);
}
