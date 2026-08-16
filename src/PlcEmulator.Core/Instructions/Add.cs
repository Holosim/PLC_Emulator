using PlcEmulator.Config;

namespace PlcEmulator.Core.Instructions;

/// <summary>Addition. Evaluation semantics land with CORE-208.</summary>
public sealed class Add : MathInstruction
{
    public Add(OperandDef left, OperandDef right, string destinationTag) : base(left, right, destinationTag)
    {
    }

    public override string Mnemonic => "ADD";
}
