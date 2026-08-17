using PlcEmulator.Config;

namespace PlcEmulator.Core.Instructions;

/// <summary>
/// Division: <c>Dest = Left / Right</c> (CORE-208). Division by zero is
/// a defined runtime error, not a crash: <see cref="TryCompute"/>
/// returns <see langword="false"/> so <see cref="MathInstruction.Evaluate"/>
/// sets a fault flag on the destination tag instead of writing a
/// result (docs/RTVM.md TP-208).
/// </summary>
public sealed class Div : MathInstruction
{
    public Div(OperandDef left, OperandDef right, string destinationTag) : base(left, right, destinationTag)
    {
    }

    public override string Mnemonic => "DIV";

    protected override bool TryCompute(double left, double right, out double result, out string? fault)
    {
        if (right == 0)
        {
            result = 0;
            fault = $"DIV by zero: {left} / {right}.";
            return false;
        }

        result = left / right;
        fault = null;
        return true;
    }
}
