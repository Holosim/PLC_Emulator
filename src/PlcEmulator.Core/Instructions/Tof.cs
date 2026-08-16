namespace PlcEmulator.Core.Instructions;

/// <summary>Timer-off-delay against <see cref="SingleTagInstruction.TagName"/>'s TIMER state. Evaluation semantics land with CORE-204.</summary>
public sealed class Tof : SingleTagInstruction
{
    public Tof(string tagName) : base(tagName, "CORE-204")
    {
    }

    public override string Mnemonic => "TOF";
}
