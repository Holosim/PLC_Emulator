using PlcEmulator.Config;

namespace PlcEmulator.Core.Instructions;

/// <summary>Equal-to compare. Evaluation semantics land with CORE-207.</summary>
public sealed class Equ : CompareInstruction
{
    public Equ(OperandDef left, OperandDef right) : base(left, right)
    {
    }

    public override string Mnemonic => "EQU";
}
