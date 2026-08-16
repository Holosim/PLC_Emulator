using PlcEmulator.Config;

namespace PlcEmulator.Core.Instructions;

/// <summary>Division. <c>DIV</c> by zero is a defined runtime fault, not a crash (CORE-208). Evaluation semantics land with CORE-208.</summary>
public sealed class Div : MathInstruction
{
    public Div(OperandDef left, OperandDef right, string destinationTag) : base(left, right, destinationTag)
    {
    }

    public override string Mnemonic => "DIV";
}
