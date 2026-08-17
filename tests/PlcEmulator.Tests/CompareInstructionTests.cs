using PlcEmulator.Config;
using PlcEmulator.Core;
using PlcEmulator.Core.Instructions;

namespace PlcEmulator.Tests;

/// <summary>
/// Verifies CORE-207 (docs/RTVM.md TP-207): the six compare
/// instructions (<c>EQU</c>, <c>NEQ</c>, <c>GRT</c>, <c>LES</c>,
/// <c>GEQ</c>, <c>LEQ</c>) evaluate two operands (tag or literal) of
/// matching numeric type and AND their boolean result into rung state.
/// </summary>
[TestClass]
public sealed class CompareInstructionTests
{
    /// <summary>Builds a TagTable with one DINT tag and one REAL tag, via the public DATA-IN-100 path (ControlLogicBuilder).</summary>
    private static TagTable BuildTagTable()
    {
        var controlLogic = new ControlLogicDef
        {
            Tags = new[]
            {
                new TagDef { Name = "Preset_Count", Type = TagTypeDef.Dint, InitialValue = 0 },
                new TagDef { Name = "Setpoint", Type = TagTypeDef.Real, InitialValue = 0.0 },
                new TagDef { Name = "Running", Type = TagTypeDef.Bool, InitialValue = false },
            },
            Rungs = Array.Empty<RungDef>(),
        };

        return ControlLogicBuilder.BuildTagTable(controlLogic);
    }

    /// <summary>TP-207: GRT(Preset_Count, 5) with Preset_Count=6 → true, Preset_Count=4 → false.</summary>
    [TestMethod]
    public void Tp207_Grt_TagVsLiteral_MatchesExpectedTruthTable()
    {
        var tags = BuildTagTable();
        var grt = new Grt(OperandDef.Tag("Preset_Count"), OperandDef.Number(5));

        tags.Set("Preset_Count", 6);
        Assert.IsTrue(grt.Evaluate(tags, rungState: true, elapsed: TimeSpan.Zero), "Preset_Count=6 should be > 5");

        tags.Set("Preset_Count", 4);
        Assert.IsFalse(grt.Evaluate(tags, rungState: true, elapsed: TimeSpan.Zero), "Preset_Count=4 should not be > 5");
    }

    [TestMethod]
    public void Equ_TagVsLiteral_TrueOnlyWhenEqual()
    {
        var tags = BuildTagTable();
        var equ = new Equ(OperandDef.Tag("Preset_Count"), OperandDef.Number(5));

        tags.Set("Preset_Count", 5);
        Assert.IsTrue(equ.Evaluate(tags, rungState: true, elapsed: TimeSpan.Zero));

        tags.Set("Preset_Count", 6);
        Assert.IsFalse(equ.Evaluate(tags, rungState: true, elapsed: TimeSpan.Zero));
    }

    [TestMethod]
    public void Neq_TagVsLiteral_TrueOnlyWhenDifferent()
    {
        var tags = BuildTagTable();
        var neq = new Neq(OperandDef.Tag("Preset_Count"), OperandDef.Number(5));

        tags.Set("Preset_Count", 5);
        Assert.IsFalse(neq.Evaluate(tags, rungState: true, elapsed: TimeSpan.Zero));

        tags.Set("Preset_Count", 6);
        Assert.IsTrue(neq.Evaluate(tags, rungState: true, elapsed: TimeSpan.Zero));
    }

    [TestMethod]
    public void Les_TagVsLiteral_TrueOnlyWhenStrictlyLess()
    {
        var tags = BuildTagTable();
        var les = new Les(OperandDef.Tag("Preset_Count"), OperandDef.Number(5));

        tags.Set("Preset_Count", 4);
        Assert.IsTrue(les.Evaluate(tags, rungState: true, elapsed: TimeSpan.Zero));

        tags.Set("Preset_Count", 5);
        Assert.IsFalse(les.Evaluate(tags, rungState: true, elapsed: TimeSpan.Zero));
    }

    [TestMethod]
    public void Geq_TagVsLiteral_TrueWhenGreaterOrEqual()
    {
        var tags = BuildTagTable();
        var geq = new Geq(OperandDef.Tag("Preset_Count"), OperandDef.Number(5));

        tags.Set("Preset_Count", 5);
        Assert.IsTrue(geq.Evaluate(tags, rungState: true, elapsed: TimeSpan.Zero));

        tags.Set("Preset_Count", 4);
        Assert.IsFalse(geq.Evaluate(tags, rungState: true, elapsed: TimeSpan.Zero));
    }

    [TestMethod]
    public void Leq_TagVsLiteral_TrueWhenLessOrEqual()
    {
        var tags = BuildTagTable();
        var leq = new Leq(OperandDef.Tag("Preset_Count"), OperandDef.Number(5));

        tags.Set("Preset_Count", 5);
        Assert.IsTrue(leq.Evaluate(tags, rungState: true, elapsed: TimeSpan.Zero));

        tags.Set("Preset_Count", 6);
        Assert.IsFalse(leq.Evaluate(tags, rungState: true, elapsed: TimeSpan.Zero));
    }

    /// <summary>Both operands may be tags, of DINT vs REAL numeric types — compares numerically rather than requiring identical tag type.</summary>
    [TestMethod]
    public void Grt_TagVsTag_DintAndReal_ComparesNumerically()
    {
        var tags = BuildTagTable();
        tags.Set("Preset_Count", 6);
        tags.Set("Setpoint", 5.5);
        var grt = new Grt(OperandDef.Tag("Preset_Count"), OperandDef.Tag("Setpoint"));

        Assert.IsTrue(grt.Evaluate(tags, rungState: true, elapsed: TimeSpan.Zero));
    }

    /// <summary>A false incoming rung state short-circuits to false regardless of the comparison result (power-flow contract, CORE-200).</summary>
    [TestMethod]
    public void Evaluate_FalseIncomingRungState_ResultIsAlwaysFalse()
    {
        var tags = BuildTagTable();
        tags.Set("Preset_Count", 6);
        var grt = new Grt(OperandDef.Tag("Preset_Count"), OperandDef.Number(5));

        Assert.IsFalse(grt.Evaluate(tags, rungState: false, elapsed: TimeSpan.Zero));
    }

    /// <summary>A non-numeric (BOOL) tag operand is rejected — "matching numeric type" requires both operands to resolve to a number.</summary>
    [TestMethod]
    public void Evaluate_BoolTagOperand_ThrowsInvalidOperationException()
    {
        var tags = BuildTagTable();
        var equ = new Equ(OperandDef.Tag("Running"), OperandDef.Number(1));

        Assert.ThrowsException<InvalidOperationException>(() => equ.Evaluate(tags, rungState: true, elapsed: TimeSpan.Zero));
    }

    /// <summary>Two numeric literals (no tag operands at all) compare directly.</summary>
    [TestMethod]
    public void Evaluate_LiteralVsLiteral_ComparesDirectly()
    {
        var tags = BuildTagTable();
        var grt = new Grt(OperandDef.Number(10), OperandDef.Number(5));

        Assert.IsTrue(grt.Evaluate(tags, rungState: true, elapsed: TimeSpan.Zero));
    }
}
