using PlcEmulator.Config;
using PlcEmulator.Core;
using PlcEmulator.Core.Instructions;

namespace PlcEmulator.Tests;

/// <summary>
/// Verifies CORE-201/CORE-202 (docs/RTVM.md TP-201/TP-202): the real
/// <c>XIC</c>/<c>XIO</c>/<c>OTE</c> instruction classes, exercised
/// through <see cref="ScanEngine"/> against a <see cref="TagTable"/>
/// built the public DATA-IN-100 way (<see cref="ControlLogicBuilder"/>).
/// </summary>
[TestClass]
public sealed class XicXioOteTests
{
    private static TagTable BuildTagTable(params (string Name, bool Value)[] boolTags)
    {
        var controlLogic = new ControlLogicDef
        {
            Tags = boolTags
                .Select(t => new TagDef { Name = t.Name, Type = TagTypeDef.Bool, InitialValue = t.Value })
                .ToArray(),
            Rungs = Array.Empty<RungDef>(),
        };

        return ControlLogicBuilder.BuildTagTable(controlLogic);
    }

    /// <summary>TP-201: XIC(C) is true when C=true and false when C=false.</summary>
    [TestMethod]
    [DataRow(true, true)]
    [DataRow(false, false)]
    public void Xic_Evaluate_MatchesTagValue(bool tagValue, bool expected)
    {
        var tags = BuildTagTable(("C", tagValue));
        var xic = new Xic("C");

        Assert.AreEqual(expected, xic.Evaluate(tags, rungState: true));
    }

    /// <summary>TP-201: XIO(C) is true when C=false and false when C=true (negation of XIC).</summary>
    [TestMethod]
    [DataRow(true, false)]
    [DataRow(false, true)]
    public void Xio_Evaluate_IsNegationOfTagValue(bool tagValue, bool expected)
    {
        var tags = BuildTagTable(("C", tagValue));
        var xio = new Xio("C");

        Assert.AreEqual(expected, xio.Evaluate(tags, rungState: true));
    }

    /// <summary>A false incoming rung state (an earlier contact was open) keeps XIC/XIO de-energized regardless of their own tag's value.</summary>
    [TestMethod]
    public void XicAndXio_Evaluate_DeEnergizedRungStateStaysFalse()
    {
        var tags = BuildTagTable(("C", true));

        Assert.IsFalse(new Xic("C").Evaluate(tags, rungState: false));
        Assert.IsFalse(new Xio("C").Evaluate(tags, rungState: false));
    }

    /// <summary>CORE-202: OTE writes the rung's evaluated (non-latching) logic to its tag, and this is re-evaluated fresh every scan.</summary>
    [TestMethod]
    public void Ote_Evaluate_WritesIncomingRungStateToTag_NonLatching()
    {
        var tags = BuildTagTable(("B", false));
        var ote = new Ote("B");

        Assert.IsTrue(ote.Evaluate(tags, rungState: true));
        Assert.AreEqual(true, tags.Get("B").Value);

        Assert.IsFalse(ote.Evaluate(tags, rungState: false));
        Assert.AreEqual(false, tags.Get("B").Value, "OTE must not latch — a false rung state must clear the coil");
    }

    /// <summary>TP-202: rung XIC(A) XIC(B) OTE(C) (series AND). A=true,B=true -> C=true; A=true,B=false -> C=false.</summary>
    [TestMethod]
    [DataRow(true, true, true)]
    [DataRow(true, false, false)]
    public void SeriesAndRung_XicXicOte_MatchesExpectedCoilResult(bool a, bool b, bool expectedC)
    {
        var tags = BuildTagTable(("A", a), ("B", b), ("C", false));
        var rungs = new List<Rung>
        {
            new() { Instructions = new IInstruction[] { new Xic("A"), new Xic("B"), new Ote("C") } },
        };
        var engine = new ScanEngine();

        engine.Evaluate(rungs, tags);

        Assert.AreEqual(expectedC, tags.Get("C").Value);
    }

    /// <summary>XIC/XIO/OTE round-trip through InstructionFactory (DATA-IN-101) exercise the same real classes, not test-local stand-ins.</summary>
    [TestMethod]
    public void InstructionFactory_BuildsRealXicXioOte_ThatEvaluateCorrectly()
    {
        var tags = BuildTagTable(("A", true), ("B", false));

        var xic = InstructionFactory.Create(new InstructionDef { Mnemonic = "XIC", Operands = new[] { OperandDef.Tag("A") } });
        var xio = InstructionFactory.Create(new InstructionDef { Mnemonic = "XIO", Operands = new[] { OperandDef.Tag("A") } });
        var ote = InstructionFactory.Create(new InstructionDef { Mnemonic = "OTE", Operands = new[] { OperandDef.Tag("B") } });

        Assert.IsInstanceOfType(xic, typeof(Xic));
        Assert.IsInstanceOfType(xio, typeof(Xio));
        Assert.IsInstanceOfType(ote, typeof(Ote));

        Assert.IsTrue(xic.Evaluate(tags, rungState: true));
        Assert.IsFalse(xio.Evaluate(tags, rungState: true));
        Assert.IsTrue(ote.Evaluate(tags, rungState: true));
        Assert.AreEqual(true, tags.Get("B").Value);
    }
}
