using PlcEmulator.Config;
using PlcEmulator.Core;

namespace PlcEmulator.Tests;

/// <summary>
/// Verifies OUT-401 (docs/RTVM.md TP-401) at the <see cref="PlcController"/>
/// level: <see cref="PlcController.QueueWrite"/> never mutates
/// <see cref="TagTable"/> directly — a queued write only takes effect
/// once <see cref="PlcController.RunScan"/> drains it at the start of
/// the next scan (docs/SDD.md, Architecture / write path note).
/// </summary>
[TestClass]
public sealed class PlcControllerWriteTests
{
    /// <summary>TP-401's exact scenario: rung `XIC(Start_PB) OTE(Motor_Run)` (TP-200 renamed).</summary>
    private static PlcController BuildTp401Controller()
    {
        var controlLogic = new ControlLogicDef
        {
            Tags = new[]
            {
                new TagDef { Name = "Start_PB", Type = TagTypeDef.Bool, InitialValue = false },
                new TagDef { Name = "Motor_Run", Type = TagTypeDef.Bool, InitialValue = false },
            },
            Rungs = new[]
            {
                new RungDef
                {
                    Instructions = new InstructionDef[]
                    {
                        new() { Mnemonic = "XIC", Operands = new[] { OperandDef.Tag("Start_PB") } },
                        new() { Mnemonic = "OTE", Operands = new[] { OperandDef.Tag("Motor_Run") } },
                    },
                },
            },
        };

        return new PlcController(
            controlLogic,
            new NetworkDef { Components = Array.Empty<NetworkComponentConfig>() },
            static driverType => throw new InvalidOperationException($"no NETWORK components in this test; unexpected driver type '{driverType}'"));
    }

    private static PlcController BuildController(params TagDef[] tags)
    {
        var controlLogic = new ControlLogicDef { Tags = tags, Rungs = Array.Empty<RungDef>() };
        return new PlcController(
            controlLogic,
            new NetworkDef { Components = Array.Empty<NetworkComponentConfig>() },
            static driverType => throw new InvalidOperationException($"no NETWORK components in this test; unexpected driver type '{driverType}'"));
    }

    /// <summary>
    /// TP-401: client queues `Start_PB=true`; before the next scan runs
    /// the tag table is untouched; after one <see cref="PlcController.RunScan"/>
    /// call, `Start_PB=true` and the rung has energized `Motor_Run=true`.
    /// </summary>
    [TestMethod]
    public void QueueWrite_AppliedAtStartOfNextScan_EnergizesDownstreamRungLogic()
    {
        var controller = BuildTp401Controller();

        controller.QueueWrite("Start_PB", true);

        // Not applied yet — QueueWrite must never touch TagTable directly.
        var beforeScan = controller.GetSnapshot();
        Assert.AreEqual(false, beforeScan.Values["Start_PB"]);
        Assert.AreEqual(false, beforeScan.Values["Motor_Run"]);

        controller.RunScan();

        var afterScan = controller.GetSnapshot();
        Assert.AreEqual(true, afterScan.Values["Start_PB"]);
        Assert.AreEqual(true, afterScan.Values["Motor_Run"]);
    }

    /// <summary>A queued write is drained exactly once — a second scan with no new writes doesn't reapply it or misbehave.</summary>
    [TestMethod]
    public void QueueWrite_DrainedOnce_SubsequentScanWithoutNewWritesIsStable()
    {
        var controller = BuildTp401Controller();
        controller.QueueWrite("Start_PB", true);

        controller.RunScan();
        controller.RunScan();

        var snapshot = controller.GetSnapshot();
        Assert.AreEqual(true, snapshot.Values["Start_PB"]);
        Assert.AreEqual(true, snapshot.Values["Motor_Run"]);
    }

    /// <summary>DINT/REAL tags are writable too, not just BOOL — matching the ICD's "DINT/REAL" note.</summary>
    [TestMethod]
    public void QueueWrite_DintAndRealTags_ApplyOnNextScan()
    {
        var controller = BuildController(
            new TagDef { Name = "Preset_Count", Type = TagTypeDef.Dint, InitialValue = 0 },
            new TagDef { Name = "Speed", Type = TagTypeDef.Real, InitialValue = 0.0 });

        controller.QueueWrite("Preset_Count", 5);
        controller.QueueWrite("Speed", 3.5);
        controller.RunScan();

        var snapshot = controller.GetSnapshot();
        Assert.AreEqual(5, snapshot.Values["Preset_Count"]);
        Assert.AreEqual(3.5, snapshot.Values["Speed"]);
    }

    /// <summary>Writing an undefined tag name throws rather than silently doing nothing.</summary>
    [TestMethod]
    public void QueueWrite_UndefinedTag_Throws()
    {
        var controller = BuildController(new TagDef { Name = "Start_PB", Type = TagTypeDef.Bool, InitialValue = false });

        Assert.ThrowsException<KeyNotFoundException>(() => controller.QueueWrite("Nonexistent_Tag", true));
    }

    /// <summary>A value whose CLR type doesn't match the tag's declared type (e.g. an int for a BOOL tag) is rejected, not silently coerced.</summary>
    [TestMethod]
    public void QueueWrite_MismatchedValueType_Throws()
    {
        var controller = BuildController(new TagDef { Name = "Start_PB", Type = TagTypeDef.Bool, InitialValue = false });

        Assert.ThrowsException<ArgumentException>(() => controller.QueueWrite("Start_PB", 1));
    }

    /// <summary>Timer/Counter tags have no externally-writable scalar value in v1.0 (docs/SDD.md ICD).</summary>
    [TestMethod]
    public void QueueWrite_TimerOrCounterTag_Throws()
    {
        var controller = BuildController(new TagDef { Name = "DelayTimer", Type = TagTypeDef.Timer, Preset = 1000 });

        Assert.ThrowsException<ArgumentException>(() => controller.QueueWrite("DelayTimer", true));
    }

    /// <summary><see cref="PlcController.GetTagType"/> is what lets a caller like the TCP/JSON server convert a raw JSON value before calling QueueWrite.</summary>
    [TestMethod]
    public void GetTagType_ReturnsDeclaredType()
    {
        var controller = BuildController(
            new TagDef { Name = "Start_PB", Type = TagTypeDef.Bool, InitialValue = false },
            new TagDef { Name = "Preset_Count", Type = TagTypeDef.Dint, InitialValue = 0 });

        Assert.AreEqual(TagType.Bool, controller.GetTagType("Start_PB"));
        Assert.AreEqual(TagType.Dint, controller.GetTagType("Preset_Count"));
        Assert.ThrowsException<KeyNotFoundException>(() => controller.GetTagType("Nonexistent_Tag"));
    }
}
