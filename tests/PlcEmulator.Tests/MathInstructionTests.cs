using PlcEmulator.Config;
using PlcEmulator.Core;
using PlcEmulator.Core.Instructions;

namespace PlcEmulator.Tests;

/// <summary>
/// Verifies CORE-208 (docs/RTVM.md TP-208): <c>ADD</c>/<c>SUB</c>/
/// <c>MUL</c>/<c>DIV</c> compute a result from two operands (tag or
/// literal) and write it to a destination tag, and <c>DIV</c> by zero
/// is a defined runtime error (a fault flag on the destination tag),
/// not a crash.
/// </summary>
[TestClass]
public sealed class MathInstructionTests
{
    /// <summary>Builds a TagTable of DINT/REAL tags via the public DATA-IN-100 path (ControlLogicBuilder) — TagTable.Define is internal to Core, by design.</summary>
    private static TagTable BuildTagTable(params (string Name, TagTypeDef Type, object InitialValue)[] tags)
    {
        var controlLogic = new ControlLogicDef
        {
            Tags = tags
                .Select(t => new TagDef { Name = t.Name, Type = t.Type, InitialValue = t.InitialValue })
                .ToArray(),
            Rungs = Array.Empty<RungDef>(),
        };

        return ControlLogicBuilder.BuildTagTable(controlLogic);
    }

    /// <summary>TP-208: ADD(A,B,Dest) with A=4, B=3 -> Dest=7.</summary>
    [TestMethod]
    public void Add_TwoTagOperands_WritesSumToDestination()
    {
        var tags = BuildTagTable(("A", TagTypeDef.Dint, 4), ("B", TagTypeDef.Dint, 3), ("Dest", TagTypeDef.Dint, 0));
        var add = new Add(OperandDef.Tag("A"), OperandDef.Tag("B"), "Dest");

        var rungStateOut = add.Evaluate(tags, rungState: true, elapsed: TimeSpan.Zero);

        Assert.AreEqual(7, tags.Get("Dest").Value);
        Assert.IsNull(tags.Get("Dest").Fault);
        Assert.IsTrue(rungStateOut, "math is an action-type instruction — must return rungState unchanged");
    }

    [TestMethod]
    public void Sub_TwoTagOperands_WritesDifferenceToDestination()
    {
        var tags = BuildTagTable(("A", TagTypeDef.Dint, 10), ("B", TagTypeDef.Dint, 3), ("Dest", TagTypeDef.Dint, 0));
        var sub = new Sub(OperandDef.Tag("A"), OperandDef.Tag("B"), "Dest");

        sub.Evaluate(tags, rungState: true, elapsed: TimeSpan.Zero);

        Assert.AreEqual(7, tags.Get("Dest").Value);
    }

    [TestMethod]
    public void Mul_TagAndLiteralOperand_WritesProductToDestination()
    {
        var tags = BuildTagTable(("A", TagTypeDef.Dint, 6), ("Dest", TagTypeDef.Dint, 0));
        var mul = new Mul(OperandDef.Tag("A"), OperandDef.Number(7), "Dest");

        mul.Evaluate(tags, rungState: true, elapsed: TimeSpan.Zero);

        Assert.AreEqual(42, tags.Get("Dest").Value);
    }

    [TestMethod]
    public void Div_NonZeroDivisor_WritesQuotientToDestination()
    {
        var tags = BuildTagTable(("A", TagTypeDef.Dint, 8), ("B", TagTypeDef.Dint, 2), ("Dest", TagTypeDef.Dint, 0));
        var div = new Div(OperandDef.Tag("A"), OperandDef.Tag("B"), "Dest");

        div.Evaluate(tags, rungState: true, elapsed: TimeSpan.Zero);

        Assert.AreEqual(4, tags.Get("Dest").Value);
        Assert.IsNull(tags.Get("Dest").Fault);
    }

    /// <summary>TP-208: DIV(A,B,Dest) with A=4, B=0 -> a defined runtime error/fault flag is raised, the destination is left unchanged, and evaluation does not throw.</summary>
    [TestMethod]
    public void Div_ByZero_SetsFaultFlagAndDoesNotThrow()
    {
        var tags = BuildTagTable(("A", TagTypeDef.Dint, 4), ("B", TagTypeDef.Dint, 0), ("Dest", TagTypeDef.Dint, -1));
        var div = new Div(OperandDef.Tag("A"), OperandDef.Tag("B"), "Dest");

        var rungStateOut = div.Evaluate(tags, rungState: true, elapsed: TimeSpan.Zero);

        Assert.AreEqual(-1, tags.Get("Dest").Value, "destination's last good value must be left in place on fault");
        Assert.IsNotNull(tags.Get("Dest").Fault, "a fault flag must be raised on the destination tag");
        Assert.IsTrue(rungStateOut, "a fault must not crash the scan or break rung power flow");
    }

    /// <summary>A math instruction whose destination previously faulted clears the fault flag on the next successful evaluation.</summary>
    [TestMethod]
    public void Div_RecoversFromPriorFault_ClearsFaultFlagOnNextSuccess()
    {
        var tags = BuildTagTable(("A", TagTypeDef.Dint, 4), ("B", TagTypeDef.Dint, 0), ("Dest", TagTypeDef.Dint, -1));
        var div = new Div(OperandDef.Tag("A"), OperandDef.Tag("B"), "Dest");
        div.Evaluate(tags, rungState: true, elapsed: TimeSpan.Zero);
        Assert.IsNotNull(tags.Get("Dest").Fault);

        tags.Set("B", 2);
        div.Evaluate(tags, rungState: true, elapsed: TimeSpan.Zero);

        Assert.AreEqual(2, tags.Get("Dest").Value);
        Assert.IsNull(tags.Get("Dest").Fault);
    }

    /// <summary>Math instructions are action-type: gated by incoming rungState, and must not run (or fault) when the rung is de-energized.</summary>
    [TestMethod]
    public void Evaluate_RungStateFalse_DoesNotComputeOrFault()
    {
        var tags = BuildTagTable(("A", TagTypeDef.Dint, 4), ("B", TagTypeDef.Dint, 0), ("Dest", TagTypeDef.Dint, -1));
        var div = new Div(OperandDef.Tag("A"), OperandDef.Tag("B"), "Dest");

        var rungStateOut = div.Evaluate(tags, rungState: false, elapsed: TimeSpan.Zero);

        Assert.AreEqual(-1, tags.Get("Dest").Value);
        Assert.IsNull(tags.Get("Dest").Fault);
        Assert.IsFalse(rungStateOut);
    }

    [TestMethod]
    public void Add_RealDestination_WritesFractionalResult()
    {
        var tags = BuildTagTable(("A", TagTypeDef.Real, 1.5), ("B", TagTypeDef.Real, 2.25), ("Dest", TagTypeDef.Real, 0.0));
        var add = new Add(OperandDef.Tag("A"), OperandDef.Tag("B"), "Dest");

        add.Evaluate(tags, rungState: true, elapsed: TimeSpan.Zero);

        Assert.AreEqual(3.75, tags.Get("Dest").Value);
    }

    /// <summary>Rung-level integration: contact(A) -> ADD(B,C,Dest), matching the InstructionFactory/ControlLogicBuilder path end to end.</summary>
    [TestMethod]
    public void ScanEngine_AddViaInstructionFactory_ComputesThroughFullRungPipeline()
    {
        var controlLogic = new ControlLogicDef
        {
            Tags = new[]
            {
                new TagDef { Name = "A", Type = TagTypeDef.Bool, InitialValue = true },
                new TagDef { Name = "B", Type = TagTypeDef.Dint, InitialValue = 4 },
                new TagDef { Name = "C", Type = TagTypeDef.Dint, InitialValue = 3 },
                new TagDef { Name = "Dest", Type = TagTypeDef.Dint, InitialValue = 0 },
            },
            Rungs = new[]
            {
                new RungDef
                {
                    Instructions = new[]
                    {
                        new InstructionDef
                        {
                            Mnemonic = "ADD",
                            Operands = new[] { OperandDef.Tag("B"), OperandDef.Tag("C"), OperandDef.Tag("Dest") },
                        },
                    },
                },
            },
        };

        var tags = ControlLogicBuilder.BuildTagTable(controlLogic);
        var rungs = ControlLogicBuilder.BuildRungs(controlLogic);
        var engine = new ScanEngine();

        engine.Evaluate(rungs, tags);

        Assert.AreEqual(7, tags.Get("Dest").Value);
    }
}
