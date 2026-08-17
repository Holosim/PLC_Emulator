using PlcEmulator.Config;

namespace PlcEmulator.Core.Instructions;

/// <summary>Greater-than compare. Evaluation semantics: CORE-207.</summary>
public sealed class Grt : CompareInstruction
{
    public Grt(OperandDef left, OperandDef right) : base(left, right)
    {
    }

    public override string Mnemonic => "GRT";

    protected override bool Compare(double left, double right) => left > right;
}
