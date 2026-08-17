using PlcEmulator.Config;

namespace PlcEmulator.Core.Instructions;

/// <summary>Greater-than-or-equal compare. Evaluation semantics: CORE-207.</summary>
public sealed class Geq : CompareInstruction
{
    public Geq(OperandDef left, OperandDef right) : base(left, right)
    {
    }

    public override string Mnemonic => "GEQ";

    protected override bool Compare(double left, double right) => left >= right;
}
