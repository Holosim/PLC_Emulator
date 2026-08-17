using PlcEmulator.Config;

namespace PlcEmulator.Core.Instructions;

/// <summary>Subtraction: <c>Dest = Left - Right</c> (CORE-208). Never faults.</summary>
public sealed class Sub : MathInstruction
{
    public Sub(OperandDef left, OperandDef right, string destinationTag) : base(left, right, destinationTag)
    {
    }

    public override string Mnemonic => "SUB";

    protected override bool TryCompute(double left, double right, out double result, out string? fault)
    {
        result = left - right;
        fault = null;
        return true;
    }
}
