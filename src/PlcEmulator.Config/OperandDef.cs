namespace PlcEmulator.Config;

/// <summary>
/// One instruction operand (DATA-IN-101): either a reference to
/// another tag by name, or a numeric literal — the "tag or literal"
/// operand shape CORE-207/CORE-208 call for on compare/math
/// instructions. A JSON string operand parses as a tag reference; a
/// JSON number operand parses as a literal (see
/// <see cref="ConfigLoader.LoadControlLogic"/>).
/// </summary>
public sealed class OperandDef
{
    private OperandDef(string? tagName, double? literal)
    {
        TagName = tagName;
        Literal = literal;
    }

    /// <summary>The referenced tag's name, or <see langword="null"/> if this operand is a literal.</summary>
    public string? TagName { get; }

    /// <summary>The literal value, or <see langword="null"/> if this operand is a tag reference.</summary>
    public double? Literal { get; }

    public bool IsTagReference => TagName is not null;

    public static OperandDef Tag(string tagName) => new(tagName, null);

    public static OperandDef Number(double literal) => new(null, literal);

    public override string ToString() =>
        IsTagReference ? TagName! : Literal!.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
}
