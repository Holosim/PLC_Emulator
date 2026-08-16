using PlcEmulator.Config;

namespace PlcEmulator.Core.Instructions;

/// <summary>Less-than-or-equal compare. Evaluation semantics land with CORE-207.</summary>
public sealed class Leq : CompareInstruction
{
    public Leq(OperandDef left, OperandDef right) : base(left, right)
    {
    }

    public override string Mnemonic => "LEQ";
}
