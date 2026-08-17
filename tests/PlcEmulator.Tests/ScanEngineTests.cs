using PlcEmulator.Config;
using PlcEmulator.Core;
using PlcEmulator.Core.Instructions;

namespace PlcEmulator.Tests;

/// <summary>
/// Verifies CORE-200 (docs/RTVM.md TP-200-class behavior): the scan
/// loop evaluates every rung, in program order, once per scan,
/// threading power flow ("rung state") left to right within each rung
/// and updating tag values before the next scan begins.
/// </summary>
/// <remarks>
/// Real <c>XIC</c>/<c>OTE</c> evaluation semantics land with
/// CORE-201/202 (issue #10) — per issue #9, this loop is proven here
/// with trivial test-local <see cref="IInstruction"/> stubs instead,
/// exactly as the issue anticipates ("a trivial pass-through or stub
/// instruction is enough to prove the loop"). TP-200 itself gets
/// re-verified end-to-end once real XIC/OTE land.
/// </remarks>
[TestClass]
public sealed class ScanEngineTests
{
    /// <summary>Condition-type stand-in for a contact: ANDs a BOOL tag's value into rung state (rung-condition-in/out).</summary>
    private sealed class ReadTagInstruction : IInstruction
    {
        private readonly string _tagName;

        public ReadTagInstruction(string tagName) => _tagName = tagName;

        public string Mnemonic => "TEST_READ";

        public bool Evaluate(TagTable tags, bool rungState) => rungState && (bool)tags.Get(_tagName).Value!;
    }

    /// <summary>Action-type stand-in for a coil: writes a BOOL tag to the incoming rung state, unchanged pass-through.</summary>
    private sealed class WriteTagInstruction : IInstruction
    {
        private readonly string _tagName;

        public WriteTagInstruction(string tagName) => _tagName = tagName;

        public string Mnemonic => "TEST_WRITE";

        public bool Evaluate(TagTable tags, bool rungState)
        {
            tags.Set(_tagName, rungState);
            return rungState;
        }
    }

    /// <summary>Records the order instructions across every rung are evaluated in, to prove program order.</summary>
    private sealed class RecordingInstruction : IInstruction
    {
        private readonly List<string> _log;
        private readonly string _label;

        public RecordingInstruction(List<string> log, string label)
        {
            _log = log;
            _label = label;
        }

        public string Mnemonic => "TEST_RECORD";

        public bool Evaluate(TagTable tags, bool rungState)
        {
            _log.Add(_label);
            return rungState;
        }
    }

    /// <summary>Builds a TagTable of BOOL tags via the public DATA-IN-100 path (ControlLogicBuilder) — TagTable.Define is internal to Core, by design.</summary>
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

    /// <summary>TP-200-shape: rung [contact(A), coil(B)], A=true before scan. After 1 scan, B=true; after A=false and another scan, B=false.</summary>
    [TestMethod]
    public void Evaluate_ContactThenCoilStub_DrivesCoilFromContactEachScan()
    {
        var tags = BuildTagTable(("A", true), ("B", false));
        var rungs = new List<Rung>
        {
            new() { Instructions = new IInstruction[] { new ReadTagInstruction("A"), new WriteTagInstruction("B") } },
        };
        var engine = new ScanEngine();

        engine.Evaluate(rungs, tags);
        Assert.AreEqual(true, tags.Get("B").Value, "after scan 1 with A=true, B should be true");

        tags.Set("A", false);
        engine.Evaluate(rungs, tags);
        Assert.AreEqual(false, tags.Get("B").Value, "after scan 2 with A=false, B should be false");
    }

    /// <summary>Every rung is evaluated exactly once per call to Evaluate — no re-evaluation, no skipped rungs.</summary>
    [TestMethod]
    public void Evaluate_MultipleRungs_EachEvaluatedExactlyOncePerScan()
    {
        var tags = new TagTable();
        var log = new List<string>();
        var rungs = new List<Rung>
        {
            new() { Instructions = new IInstruction[] { new RecordingInstruction(log, "rung0") } },
            new() { Instructions = new IInstruction[] { new RecordingInstruction(log, "rung1") } },
            new() { Instructions = new IInstruction[] { new RecordingInstruction(log, "rung2") } },
        };
        var engine = new ScanEngine();

        engine.Evaluate(rungs, tags);

        CollectionAssert.AreEqual(new[] { "rung0", "rung1", "rung2" }, log);
    }

    /// <summary>Rungs are evaluated in program (declaration) order, and instructions within a rung are evaluated left to right.</summary>
    [TestMethod]
    public void Evaluate_RungsAndInstructions_RunInProgramOrder()
    {
        var tags = new TagTable();
        var log = new List<string>();
        var rungs = new List<Rung>
        {
            new()
            {
                Instructions = new IInstruction[]
                {
                    new RecordingInstruction(log, "rung0.instr0"),
                    new RecordingInstruction(log, "rung0.instr1"),
                },
            },
            new() { Instructions = new IInstruction[] { new RecordingInstruction(log, "rung1.instr0") } },
        };
        var engine = new ScanEngine();

        engine.Evaluate(rungs, tags);

        CollectionAssert.AreEqual(
            new[] { "rung0.instr0", "rung0.instr1", "rung1.instr0" },
            log);
    }

    /// <summary>Each rung's power flow starts fresh (energized) — a false result on one rung must not leak into the next rung's evaluation.</summary>
    [TestMethod]
    public void Evaluate_RungState_DoesNotLeakAcrossRungs()
    {
        var tags = BuildTagTable(("A", false), ("B", false), ("C", false));
        var rungs = new List<Rung>
        {
            // Rung 0: contact(A=false) -> coil(B). B should end up false.
            new() { Instructions = new IInstruction[] { new ReadTagInstruction("A"), new WriteTagInstruction("B") } },
            // Rung 1: coil(C) with no preceding contact -> starts energized, so C should end up true.
            new() { Instructions = new IInstruction[] { new WriteTagInstruction("C") } },
        };
        var engine = new ScanEngine();

        engine.Evaluate(rungs, tags);

        Assert.AreEqual(false, tags.Get("B").Value);
        Assert.AreEqual(true, tags.Get("C").Value);
    }

    /// <summary>An empty rung program is a no-op scan — no exception, no tag changes.</summary>
    [TestMethod]
    public void Evaluate_NoRungs_IsNoOp()
    {
        var tags = BuildTagTable(("A", true));
        var engine = new ScanEngine();

        engine.Evaluate(Array.Empty<Rung>(), tags);

        Assert.AreEqual(true, tags.Get("A").Value);
    }

    /// <summary>PlcController.RunScan() drives its owned ScanEngine against its own tag table, end to end, once per call.</summary>
    [TestMethod]
    public void PlcController_RunScan_EvaluatesOwnRungsAgainstOwnTagTable()
    {
        var controlLogic = new ControlLogicDef
        {
            Tags = new[]
            {
                new TagDef { Name = "A", Type = TagTypeDef.Bool, InitialValue = true },
                new TagDef { Name = "B", Type = TagTypeDef.Bool, InitialValue = false },
            },
            Rungs = new[]
            {
                new RungDef
                {
                    Instructions = new[]
                    {
                        new InstructionDef { Mnemonic = "XIC", Operands = new[] { OperandDef.Tag("A") } },
                        new InstructionDef { Mnemonic = "OTE", Operands = new[] { OperandDef.Tag("B") } },
                    },
                },
            },
        };

        var controller = new PlcController(
            controlLogic,
            new NetworkDef { Components = Array.Empty<NetworkComponentConfig>() },
            static driverType => throw new InvalidOperationException($"no NETWORK components in this test; unexpected driver type '{driverType}'"));

        // XIC/OTE evaluation itself is CORE-201/202 (issue #10) and still
        // throws — this only confirms RunScan() reaches the Scan Engine
        // and attempts to evaluate every configured rung, not that XIC/OTE
        // produce a result yet.
        Assert.ThrowsException<NotImplementedException>(controller.RunScan);
    }
}
