using PlcEmulator.Config;
using PlcEmulator.Core;
using PlcEmulator.Core.Instructions;

namespace PlcEmulator.Tests;

/// <summary>
/// Verifies DATA-IN-100 (tag data model) and DATA-IN-101 (rung/
/// instruction list) per docs/RTVM.md TP-100/TP-101, plus a few
/// structural-validation cases inherent to "parses ... into an
/// internal in-memory model ... rejecting invalid input with a
/// descriptive error" (see docs/SDD.md, Architecture / Config Loader).
/// </summary>
[TestClass]
public sealed class ControlLogicSchemaTests
{
    private static string WriteTempControlLogic(string json)
    {
        var path = Path.Combine(Path.GetTempPath(), $"control-logic-{Guid.NewGuid():N}.json");
        File.WriteAllText(path, json);
        return path;
    }

    /// <summary>TP-100: tags Start_PB:BOOL=false, Motor_Run:BOOL=false, Preset_Count:DINT=0 → internal tag table has exactly 3 entries with those names/types/initial values.</summary>
    [TestMethod]
    public void Tp100_InternalTagTable_HasExpectedEntries()
    {
        var path = WriteTempControlLogic("""
            {
              "tags": [
                { "name": "Start_PB", "type": "BOOL", "initialValue": false },
                { "name": "Motor_Run", "type": "BOOL", "initialValue": false },
                { "name": "Preset_Count", "type": "DINT", "initialValue": 0 }
              ],
              "rungs": []
            }
            """);

        var controlLogic = ConfigLoader.LoadControlLogic(path);
        var tagTable = ControlLogicBuilder.BuildTagTable(controlLogic);

        Assert.AreEqual(3, tagTable.Count);

        var startPb = tagTable.Get("Start_PB");
        Assert.AreEqual(TagType.Bool, startPb.Type);
        Assert.AreEqual(false, startPb.Value);

        var motorRun = tagTable.Get("Motor_Run");
        Assert.AreEqual(TagType.Bool, motorRun.Type);
        Assert.AreEqual(false, motorRun.Value);

        var presetCount = tagTable.Get("Preset_Count");
        Assert.AreEqual(TagType.Dint, presetCount.Type);
        Assert.AreEqual(0, presetCount.Value);
    }

    /// <summary>DATA-IN-100: a TIMER tag carries a structured .PRE/.ACC/.DN/.EN state, not a scalar value.</summary>
    [TestMethod]
    public void TimerTag_PopulatesStructuredState()
    {
        var path = WriteTempControlLogic("""
            {
              "tags": [ { "name": "MyTimer", "type": "TIMER", "preset": 3000 } ],
              "rungs": []
            }
            """);

        var tagTable = ControlLogicBuilder.BuildTagTable(ConfigLoader.LoadControlLogic(path));
        var tag = tagTable.Get("MyTimer");

        Assert.AreEqual(TagType.Timer, tag.Type);
        Assert.IsNull(tag.Value);
        Assert.IsNotNull(tag.Timer);
        Assert.AreEqual(3000, tag.Timer!.Pre);
        Assert.AreEqual(0, tag.Timer.Acc);
        Assert.IsFalse(tag.Timer.Dn);
        Assert.IsFalse(tag.Timer.En);
    }

    /// <summary>DATA-IN-100: a COUNTER tag carries a structured .PRE/.ACC/.DN state, not a scalar value.</summary>
    [TestMethod]
    public void CounterTag_PopulatesStructuredState()
    {
        var path = WriteTempControlLogic("""
            {
              "tags": [ { "name": "MyCounter", "type": "COUNTER", "preset": 5 } ],
              "rungs": []
            }
            """);

        var tagTable = ControlLogicBuilder.BuildTagTable(ConfigLoader.LoadControlLogic(path));
        var tag = tagTable.Get("MyCounter");

        Assert.AreEqual(TagType.Counter, tag.Type);
        Assert.IsNull(tag.Value);
        Assert.IsNotNull(tag.Counter);
        Assert.AreEqual(5, tag.Counter!.Pre);
        Assert.AreEqual(0, tag.Counter.Acc);
        Assert.IsFalse(tag.Counter.Dn);
    }

    /// <summary>TP-101: rung XIC(Start_PB) OTE(Motor_Run) → parsed rung has instruction sequence [XIC:Start_PB, OTE:Motor_Run] in order.</summary>
    [TestMethod]
    public void Tp101_ParsedRung_HasExpectedInstructionSequence()
    {
        var path = WriteTempControlLogic("""
            {
              "tags": [
                { "name": "Start_PB", "type": "BOOL", "initialValue": false },
                { "name": "Motor_Run", "type": "BOOL", "initialValue": false }
              ],
              "rungs": [
                { "instructions": [
                    { "op": "XIC", "operands": ["Start_PB"] },
                    { "op": "OTE", "operands": ["Motor_Run"] }
                ] }
              ]
            }
            """);

        var controlLogic = ConfigLoader.LoadControlLogic(path);
        var rungs = ControlLogicBuilder.BuildRungs(controlLogic);

        Assert.AreEqual(1, rungs.Count);
        var instructions = rungs[0].Instructions;
        Assert.AreEqual(2, instructions.Count);

        CollectionAssert.AreEqual(
            new[] { "XIC:Start_PB", "OTE:Motor_Run" },
            instructions.Select(i => i.ToString()).ToArray());

        Assert.IsInstanceOfType(instructions[0], typeof(Xic));
        Assert.AreEqual("XIC", instructions[0].Mnemonic);
        Assert.AreEqual("Start_PB", ((Xic)instructions[0]).TagName);

        Assert.IsInstanceOfType(instructions[1], typeof(Ote));
        Assert.AreEqual("OTE", instructions[1].Mnemonic);
        Assert.AreEqual("Motor_Run", ((Ote)instructions[1]).TagName);
    }

    /// <summary>DATA-IN-101: the full MVP instruction set parses into the matching concrete instruction types.</summary>
    [TestMethod]
    public void AllMvpMnemonics_ParseToExpectedInstructionType()
    {
        var path = WriteTempControlLogic("""
            {
              "tags": [
                { "name": "A", "type": "BOOL", "initialValue": false },
                { "name": "B", "type": "BOOL", "initialValue": false },
                { "name": "N", "type": "DINT", "initialValue": 0 },
                { "name": "T", "type": "TIMER", "preset": 100 },
                { "name": "C", "type": "COUNTER", "preset": 5 }
              ],
              "rungs": [
                { "instructions": [
                    { "op": "XIC", "operands": ["A"] },
                    { "op": "XIO", "operands": ["A"] },
                    { "op": "OTE", "operands": ["B"] },
                    { "op": "TON", "operands": ["T"] },
                    { "op": "TOF", "operands": ["T"] },
                    { "op": "CTU", "operands": ["C"] },
                    { "op": "CTD", "operands": ["C"] },
                    { "op": "RES", "operands": ["C"] },
                    { "op": "EQU", "operands": ["N", 0] },
                    { "op": "NEQ", "operands": ["N", 0] },
                    { "op": "GRT", "operands": ["N", 0] },
                    { "op": "LES", "operands": ["N", 0] },
                    { "op": "GEQ", "operands": ["N", 0] },
                    { "op": "LEQ", "operands": ["N", 0] },
                    { "op": "ADD", "operands": ["N", 1, "N"] },
                    { "op": "SUB", "operands": ["N", 1, "N"] },
                    { "op": "MUL", "operands": ["N", 1, "N"] },
                    { "op": "DIV", "operands": ["N", 1, "N"] }
                ] }
              ]
            }
            """);

        var rungs = ControlLogicBuilder.BuildRungs(ConfigLoader.LoadControlLogic(path));
        var instructions = rungs[0].Instructions;

        var expectedTypes = new Type[]
        {
            typeof(Xic), typeof(Xio), typeof(Ote), typeof(Ton), typeof(Tof),
            typeof(Ctu), typeof(Ctd), typeof(Res),
            typeof(Equ), typeof(Neq), typeof(Grt), typeof(Les), typeof(Geq), typeof(Leq),
            typeof(Add), typeof(Sub), typeof(Mul), typeof(Div),
        };

        Assert.AreEqual(expectedTypes.Length, instructions.Count);
        for (var i = 0; i < expectedTypes.Length; i++)
        {
            Assert.IsInstanceOfType(instructions[i], expectedTypes[i], $"instruction {i} ({instructions[i].Mnemonic})");
        }
    }

    [TestMethod]
    public void DuplicateTagName_ThrowsConfigValidationException()
    {
        var path = WriteTempControlLogic("""
            {
              "tags": [
                { "name": "Start_PB", "type": "BOOL", "initialValue": false },
                { "name": "Start_PB", "type": "BOOL", "initialValue": true }
              ],
              "rungs": []
            }
            """);

        Assert.ThrowsException<ConfigValidationException>(() => ConfigLoader.LoadControlLogic(path));
    }

    [TestMethod]
    public void UnrecognizedTagType_ThrowsConfigValidationException()
    {
        var path = WriteTempControlLogic("""
            { "tags": [ { "name": "X", "type": "STRING", "initialValue": "hi" } ], "rungs": [] }
            """);

        Assert.ThrowsException<ConfigValidationException>(() => ConfigLoader.LoadControlLogic(path));
    }

    [TestMethod]
    public void UnrecognizedMnemonic_ThrowsConfigValidationException()
    {
        var path = WriteTempControlLogic("""
            {
              "tags": [ { "name": "A", "type": "BOOL", "initialValue": false } ],
              "rungs": [ { "instructions": [ { "op": "NOPE", "operands": ["A"] } ] } ]
            }
            """);

        var controlLogic = ConfigLoader.LoadControlLogic(path);
        Assert.ThrowsException<ConfigValidationException>(() => ControlLogicBuilder.BuildRungs(controlLogic));
    }

    [TestMethod]
    public void WrongOperandCount_ThrowsConfigValidationException()
    {
        var path = WriteTempControlLogic("""
            {
              "tags": [ { "name": "A", "type": "BOOL", "initialValue": false } ],
              "rungs": [ { "instructions": [ { "op": "XIC", "operands": ["A", "A"] } ] } ]
            }
            """);

        var controlLogic = ConfigLoader.LoadControlLogic(path);
        Assert.ThrowsException<ConfigValidationException>(() => ControlLogicBuilder.BuildRungs(controlLogic));
    }

    [TestMethod]
    public void MalformedJson_ThrowsConfigValidationException()
    {
        var path = WriteTempControlLogic("{ this is not json");

        Assert.ThrowsException<ConfigValidationException>(() => ConfigLoader.LoadControlLogic(path));
    }
}
