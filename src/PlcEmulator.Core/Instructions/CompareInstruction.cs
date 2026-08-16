using PlcEmulator.Config;

namespace PlcEmulator.Core.Instructions;

/// <summary>
/// Base for the MVP compare instructions (<c>EQU</c>, <c>NEQ</c>,
/// <c>GRT</c>, <c>LES</c>, <c>GEQ</c>, <c>LEQ</c>): two operands (tag
/// or literal), producing a boolean rung-true/false result — no
/// destination tag. Parsing/shape is DATA-IN-101; evaluation semantics
/// land with CORE-207.
/// </summary>
public abstract class CompareInstruction : IInstruction
{
    protected CompareInstruction(OperandDef left, OperandDef right)
    {
        Left = left;
        Right = right;
    }

    public OperandDef Left { get; }

    public OperandDef Right { get; }

    public abstract string Mnemonic { get; }

    public bool Evaluate(TagTable tags) =>
        throw new NotImplementedException($"{Mnemonic}.Evaluate lands with CORE-207.");

    public override string ToString() => $"{Mnemonic}:{Left},{Right}";
}
