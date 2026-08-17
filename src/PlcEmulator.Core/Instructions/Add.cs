using PlcEmulator.Config;

namespace PlcEmulator.Core.Instructions;

/// <summary>Addition: <c>Dest = Left + Right</c> (CORE-208). Never faults.</summary>
public sealed class Add : MathInstruction
{
    public Add(OperandDef left, OperandDef right, string destinationTag) : base(left, right, destinationTag)
    {
    }

    public override string Mnemonic => "ADD";

    protected override bool TryCompute(double left, double right, out double result, out string? fault)
    {
        result = left + right;
        fault = null;
        return true;
    }
}
