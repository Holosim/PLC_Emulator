using PlcEmulator.Config;

namespace PlcEmulator.Core.Instructions;

/// <summary>Not-equal compare. Evaluation semantics land with CORE-207.</summary>
public sealed class Neq : CompareInstruction
{
    public Neq(OperandDef left, OperandDef right) : base(left, right)
    {
    }

    public override string Mnemonic => "NEQ";
}
