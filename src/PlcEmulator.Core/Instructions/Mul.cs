using PlcEmulator.Config;

namespace PlcEmulator.Core.Instructions;

/// <summary>Multiplication. Evaluation semantics land with CORE-208.</summary>
public sealed class Mul : MathInstruction
{
    public Mul(OperandDef left, OperandDef right, string destinationTag) : base(left, right, destinationTag)
    {
    }

    public override string Mnemonic => "MUL";
}
