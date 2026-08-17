using PlcEmulator.Config;

namespace PlcEmulator.Core.Instructions;

/// <summary>
/// Base for the MVP math instructions (<c>ADD</c>, <c>SUB</c>,
/// <c>MUL</c>, <c>DIV</c>): two source operands (tag or literal) and a
/// destination tag (CORE-208). Action-type instruction per the
/// rung-power-flow contract (docs/SDD.md, Coding Standards): only
/// applies its side effect — computing and writing the destination —
/// while the incoming <c>rungState</c> is energized, and always
/// returns it unchanged so power flow continues to any further
/// instructions on the same rung.
/// </summary>
public abstract class MathInstruction : IInstruction
{
    protected MathInstruction(OperandDef left, OperandDef right, string destinationTag)
    {
        Left = left;
        Right = right;
        DestinationTag = destinationTag;
    }

    public OperandDef Left { get; }

    public OperandDef Right { get; }

    public string DestinationTag { get; }

    public abstract string Mnemonic { get; }

    public bool Evaluate(TagTable tags, bool rungState, TimeSpan elapsed)
    {
        if (rungState)
        {
            var destination = tags.Get(DestinationTag);
            var left = ReadOperand(tags, Left);
            var right = ReadOperand(tags, Right);

            if (TryCompute(left, right, out var result, out var fault))
            {
                destination.Value = destination.Type == TagType.Real ? result : (object)(int)result;
                destination.Fault = null;
            }
            else
            {
                // Defined runtime error (e.g. CORE-208 DIV-by-zero): set the
                // fault flag and leave the destination's last good Value in
                // place rather than throwing — the Scan Engine never throws
                // for expected runtime conditions (docs/SDD.md).
                destination.Fault = fault;
            }
        }

        return rungState;
    }

    /// <summary>
    /// Computes this instruction's result from its two already-resolved
    /// operands. Returns <see langword="false"/> (with
    /// <paramref name="fault"/> describing why, and
    /// <paramref name="result"/> unspecified) for a defined runtime
    /// error such as CORE-208's divide-by-zero — never throws.
    /// </summary>
    protected abstract bool TryCompute(double left, double right, out double result, out string? fault);

    /// <summary>Resolves an operand to a number: its literal value, or its referenced tag's current value.</summary>
    private static double ReadOperand(TagTable tags, OperandDef operand) =>
        operand.IsTagReference ? Convert.ToDouble(tags.Get(operand.TagName!).Value) : operand.Literal!.Value;

    public override string ToString() => $"{Mnemonic}:{Left},{Right}->{DestinationTag}";
}
