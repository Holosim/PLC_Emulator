using PlcEmulator.Config;

namespace PlcEmulator.Core.Instructions;

/// <summary>
/// Base for the MVP compare instructions (<c>EQU</c>, <c>NEQ</c>,
/// <c>GRT</c>, <c>LES</c>, <c>GEQ</c>, <c>LEQ</c>): two operands (tag
/// or literal), producing a boolean rung-true/false result — no
/// destination tag. Parsing/shape is DATA-IN-101; evaluation semantics
/// (CORE-207) implemented here, shared by every subclass — each
/// subclass supplies only <see cref="Compare"/> and its
/// <see cref="Mnemonic"/>.
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

    /// <summary>
    /// The mnemonic-specific comparison over the two resolved numeric
    /// operand values (CORE-207) — the only thing that varies between
    /// <c>EQU</c>/<c>NEQ</c>/<c>GRT</c>/<c>LES</c>/<c>GEQ</c>/<c>LEQ</c>.
    /// </summary>
    protected abstract bool Compare(double left, double right);

    /// <summary>
    /// Condition-type evaluation (CORE-207): resolves both operands to
    /// numeric values, ANDs the mnemonic's comparison result into the
    /// incoming rung state, and returns it (rung-condition-out) — see
    /// <see cref="IInstruction.Evaluate"/>'s rung-power-flow contract
    /// (CORE-200).
    /// </summary>
    public bool Evaluate(TagTable tags, bool rungState) =>
        rungState && Compare(ResolveNumeric(Left, tags), ResolveNumeric(Right, tags));

    /// <summary>
    /// Resolves one operand (tag or literal) to a numeric value. A
    /// literal is always numeric; a tag operand must be
    /// <see cref="TagType.Dint"/> or <see cref="TagType.Real"/> — the
    /// "matching numeric type" CORE-207 calls for means both operands
    /// must resolve to a number, not that two tag operands must share
    /// the exact same DINT/REAL tag type (standard Rockwell compares
    /// allow DINT-vs-REAL with implicit numeric promotion; only a
    /// non-numeric tag — BOOL/TIMER/COUNTER — is rejected).
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// The operand names a tag whose type is not DINT/REAL. This
    /// mirrors <see cref="TagTable.Get"/>'s existing "should have been
    /// caught earlier, but wasn't" runtime-exception precedent — there
    /// is no CONTROL_LOGIC-load-time check yet that a compare/math
    /// operand tag is numeric.
    /// </exception>
    private double ResolveNumeric(OperandDef operand, TagTable tags)
    {
        if (!operand.IsTagReference)
        {
            return operand.Literal!.Value;
        }

        var tag = tags.Get(operand.TagName!);
        return tag.Type switch
        {
            TagType.Dint or TagType.Real => Convert.ToDouble(tag.Value),
            _ => throw new InvalidOperationException(
                $"{Mnemonic}: operand tag '{operand.TagName}' is {tag.Type}, but compare instructions " +
                "require a numeric (DINT/REAL) tag."),
        };
    }

    public override string ToString() => $"{Mnemonic}:{Left},{Right}";
}
