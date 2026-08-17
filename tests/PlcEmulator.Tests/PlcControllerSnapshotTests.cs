using PlcEmulator.Config;
using PlcEmulator.Core;

namespace PlcEmulator.Tests;

/// <summary>
/// Verifies DATA-OUT-300 (docs/RTVM.md TP-300): the internal runtime
/// state model holds current values for every tag and is queryable via
/// <see cref="PlcController.GetSnapshot"/>, updated at the end of every
/// scan cycle.
/// </summary>
/// <remarks>
/// TP-300's scenario drives <c>Motor_Run</c>/<c>Preset_Count</c> to
/// their expected post-scan values via real rung logic; that logic
/// (<c>XIC</c>/<c>OTE</c>) is CORE-201/202 (issue #10), still
/// scaffolding-only, so these tests instead seed the tag table with
/// the same post-scan values TP-300 expects and run a rung-free scan
/// to prove <see cref="PlcController.GetSnapshot"/> reflects
/// <see cref="TagTable"/> state after <see cref="PlcController.RunScan"/>
/// completes. TP-300 gets re-verified end-to-end once real XIC/OTE
/// land, same pattern as CORE-200's own scaffolding note (issue #9).
/// </remarks>
[TestClass]
public sealed class PlcControllerSnapshotTests
{
    private static PlcController BuildController(params TagDef[] tags)
    {
        var controlLogic = new ControlLogicDef { Tags = tags, Rungs = Array.Empty<RungDef>() };
        return new PlcController(
            controlLogic,
            new NetworkDef { Components = Array.Empty<NetworkComponentConfig>() },
            static driverType => throw new InvalidOperationException($"no NETWORK components in this test; unexpected driver type '{driverType}'"));
    }

    /// <summary>TP-300 shape: after 1 scan cycle, snapshot returns {Start_PB:false, Motor_Run:true, Preset_Count:5}.</summary>
    [TestMethod]
    public void GetSnapshot_AfterScan_ReturnsCurrentScalarTagValues()
    {
        var controller = BuildController(
            new TagDef { Name = "Start_PB", Type = TagTypeDef.Bool, InitialValue = false },
            new TagDef { Name = "Motor_Run", Type = TagTypeDef.Bool, InitialValue = true },
            new TagDef { Name = "Preset_Count", Type = TagTypeDef.Dint, InitialValue = 5 });

        controller.RunScan();
        var snapshot = controller.GetSnapshot();

        Assert.AreEqual(3, snapshot.Values.Count);
        Assert.AreEqual(false, snapshot.Values["Start_PB"]);
        Assert.AreEqual(true, snapshot.Values["Motor_Run"]);
        Assert.AreEqual(5, snapshot.Values["Preset_Count"]);
    }

    /// <summary>A REAL-typed tag's value round-trips through the snapshot too, not just BOOL/DINT.</summary>
    [TestMethod]
    public void GetSnapshot_IncludesRealTags()
    {
        var controller = BuildController(new TagDef { Name = "Speed", Type = TagTypeDef.Real, InitialValue = 3.5 });

        var snapshot = controller.GetSnapshot();

        Assert.AreEqual(3.5, snapshot.Values["Speed"]);
    }

    /// <summary>
    /// Structured timer/counter sub-elements are not exposed through
    /// the snapshot (per TagSnapshot's ICD note) — only scalar tags
    /// appear, even though the underlying TagTable still holds the
    /// full timer/counter state (DATA-OUT-300's "including
    /// timer/counter sub-elements" requirement, satisfied by TagTable
    /// itself, not by this externally-facing snapshot).
    /// </summary>
    [TestMethod]
    public void GetSnapshot_ExcludesTimerAndCounterTags()
    {
        var controller = BuildController(
            new TagDef { Name = "Start_PB", Type = TagTypeDef.Bool, InitialValue = false },
            new TagDef { Name = "DelayTimer", Type = TagTypeDef.Timer, Preset = 1000 },
            new TagDef { Name = "PartCounter", Type = TagTypeDef.Counter, Preset = 10 });

        var snapshot = controller.GetSnapshot();

        Assert.AreEqual(1, snapshot.Values.Count);
        Assert.IsTrue(snapshot.Values.ContainsKey("Start_PB"));
        Assert.IsFalse(snapshot.Values.ContainsKey("DelayTimer"));
        Assert.IsFalse(snapshot.Values.ContainsKey("PartCounter"));
    }

    /// <summary>
    /// Each call returns its own independent copy, not a live view over
    /// the same backing map — two snapshots taken back to back must not
    /// be the same dictionary instance (point-in-time semantics).
    /// </summary>
    [TestMethod]
    public void GetSnapshot_ReturnsIndependentCopyEachCall()
    {
        var controller = BuildController(new TagDef { Name = "Start_PB", Type = TagTypeDef.Bool, InitialValue = false });

        var first = controller.GetSnapshot();
        var second = controller.GetSnapshot();

        Assert.AreNotSame(first.Values, second.Values);
        Assert.AreEqual(first.Values["Start_PB"], second.Values["Start_PB"]);
    }
}
