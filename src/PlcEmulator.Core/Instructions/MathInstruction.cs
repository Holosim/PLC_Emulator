using PlcEmulator.Config;

namespace PlcEmulator.Core.Instructions;

/// <summary>
/// Base for the MVP math instructions (<c>ADD</c>, <c>SUB</c>,
/// <c>MUL</c>, <c>DIV</c>): two source operands (tag or literal) and a
/// destination tag. Parsing/shape is DATA-IN-101; evaluation semantics
/// (including the CORE-208 divide-by-zero-is-a-fault-flag-not-a-crash
/// rule) land with CORE-208.
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

    public bool Evaluate(TagTable tags, bool rungState, TimeSpan elapsed) =>
        throw new NotImplementedException($"{Mnemonic}.Evaluate lands with CORE-208.");

    public override string ToString() => $"{Mnemonic}:{Left},{Right}->{DestinationTag}";
}
