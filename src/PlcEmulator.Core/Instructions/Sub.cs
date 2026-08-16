using PlcEmulator.Config;

namespace PlcEmulator.Core.Instructions;

/// <summary>Subtraction. Evaluation semantics land with CORE-208.</summary>
public sealed class Sub : MathInstruction
{
    public Sub(OperandDef left, OperandDef right, string destinationTag) : base(left, right, destinationTag)
    {
    }

    public override string Mnemonic => "SUB";
}
