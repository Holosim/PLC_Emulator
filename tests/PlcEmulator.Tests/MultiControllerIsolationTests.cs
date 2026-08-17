using PlcEmulator.Config;
using PlcEmulator.Core;
using PlcEmulator.Core.Drivers;

namespace PlcEmulator.Tests;

/// <summary>
/// Verifies NFR-500 (docs/RTVM.md TP-500): two <see cref="PlcController"/>
/// instances, constructed side by side in the same process from distinct
/// CONTROL_LOGIC/NETWORK configurations, share no mutable/static state —
/// each holds and scans its own tag/runtime state independently.
/// </summary>
/// <remarks>
/// This is a verification pass, not new functional code (see issue #23):
/// <see cref="PlcController"/>, <see cref="TagTable"/>, <see cref="ScanEngine"/>,
/// <see cref="WriteQueue"/> and the built-in drivers were all already
/// documented and implemented as instance-owned state with no
/// static/singleton fields (CORE-209/#15, DATA-OUT-300/#18). This test
/// class is the concrete unit-test artifact TP-500 calls for, proving
/// that inspection conclusion by construction rather than by reading
/// the source alone. It deliberately uses two configs that share the
/// exact same tag names — the strongest form of the check, since any
/// accidental global/static registry keyed by tag name would cross-talk
/// here even though instance-distinct configs would not expose it.
/// </remarks>
[TestClass]
public sealed class MultiControllerIsolationTests
{
    private static ControlLogicDef OneBoolTagWithOteRung(string tagName, bool initialValue) => new()
    {
        Tags = new[] { new TagDef { Name = tagName, Type = TagTypeDef.Bool, InitialValue = initialValue } },
        Rungs = new[]
        {
            new RungDef
            {
                Instructions = new[]
                {
                    new InstructionDef { Mnemonic = "OTE", Operands = new[] { OperandDef.Tag(tagName) } },
                },
            },
        },
    };

    private static NetworkDef OneComponentNetwork(string componentName, string driverType, string tagName) => new()
    {
        Components = new[]
        {
            new NetworkComponentConfig { Name = componentName, DriverType = driverType, Tags = new[] { tagName } },
        },
    };

    /// <summary>Records which <see cref="TagTable"/> instance it was bound to, so tests can assert two controllers never bind drivers to the same table.</summary>
    private sealed class RecordingDriver : IDriver
    {
        public TagTable? BoundTags { get; private set; }

        public void Bind(TagTable tags, NetworkComponentConfig config) => BoundTags = tags;

        public void OnScanComplete()
        {
        }
    }

    /// <summary>
    /// TP-500: constructing two controllers from configs that reuse the
    /// same tag name does not let a write on one controller's tag table
    /// leak into the other's — each owns its own <see cref="TagTable"/>.
    /// </summary>
    [TestMethod]
    public void TwoControllers_SameTagName_RunScanOnOneDoesNotAffectTheOther()
    {
        // Controller A's rung unconditionally energizes (OTE with no
        // preceding contacts -> rung state starts true), so a scan
        // drives Shared_Bit to true.
        var controllerA = new PlcController(
            OneBoolTagWithOteRung("Shared_Bit", initialValue: false),
            new NetworkDef { Components = Array.Empty<NetworkComponentConfig>() },
            static driverType => throw new InvalidOperationException($"no NETWORK components in this test; unexpected driver type '{driverType}'"));

        // Controller B starts with the same tag name at the same initial
        // value, but has no rungs at all -> a scan must leave it false.
        var controllerB = new PlcController(
            new ControlLogicDef
            {
                Tags = new[] { new TagDef { Name = "Shared_Bit", Type = TagTypeDef.Bool, InitialValue = false } },
                Rungs = Array.Empty<RungDef>(),
            },
            new NetworkDef { Components = Array.Empty<NetworkComponentConfig>() },
            static driverType => throw new InvalidOperationException($"no NETWORK components in this test; unexpected driver type '{driverType}'"));

        controllerA.RunScan();

        Assert.AreEqual(true, controllerA.GetSnapshot().Values["Shared_Bit"], "Controller A's own rung should have energized its tag.");
        Assert.AreEqual(false, controllerB.GetSnapshot().Values["Shared_Bit"], "Controller B must not observe controller A's scan result for a same-named tag.");

        controllerB.RunScan();

        Assert.AreEqual(false, controllerB.GetSnapshot().Values["Shared_Bit"], "Controller B has no rungs driving Shared_Bit, so it must stay false even after its own scan.");
    }

    /// <summary>
    /// TP-500: two controllers built from configs that declare a
    /// same-named NETWORK component bound to a same-named tag still get
    /// two distinct driver instances, each bound to its own controller's
    /// <see cref="TagTable"/> — never a shared/global driver or table.
    /// </summary>
    [TestMethod]
    public void TwoControllers_SameComponentAndTagNames_GetDistinctDriverInstancesBoundToDistinctTagTables()
    {
        var driverA = new RecordingDriver();
        var driverB = new RecordingDriver();

        var controllerA = new PlcController(
            OneBoolTagWithOteRung("Sensor_Bit", initialValue: false),
            OneComponentNetwork("ProxSensor1", DriverResolverStub.DiscreteSensor, "Sensor_Bit"),
            _ => driverA);

        var controllerB = new PlcController(
            OneBoolTagWithOteRung("Sensor_Bit", initialValue: true),
            OneComponentNetwork("ProxSensor1", DriverResolverStub.DiscreteSensor, "Sensor_Bit"),
            _ => driverB);

        Assert.AreNotSame(driverA, driverB);
        Assert.IsNotNull(driverA.BoundTags);
        Assert.IsNotNull(driverB.BoundTags);
        Assert.AreNotSame(driverA.BoundTags, driverB.BoundTags, "Each controller must build and bind its own TagTable, never a shared one.");

        Assert.AreEqual(false, driverA.BoundTags!.Get("Sensor_Bit").Value);
        Assert.AreEqual(true, driverB.BoundTags!.Get("Sensor_Bit").Value);
    }

    /// <summary>
    /// TP-500: <see cref="PlcController.GetSnapshot"/> results for two
    /// independently constructed and independently scanned controllers
    /// never share the same backing values, even for identically named
    /// tags.
    /// </summary>
    [TestMethod]
    public void TwoControllers_IndependentScans_ProduceIndependentSnapshots()
    {
        var controllerA = new PlcController(
            OneBoolTagWithOteRung("Motor_Run", initialValue: false),
            new NetworkDef { Components = Array.Empty<NetworkComponentConfig>() },
            static driverType => throw new InvalidOperationException($"no NETWORK components in this test; unexpected driver type '{driverType}'"));

        var controllerB = new PlcController(
            new ControlLogicDef
            {
                Tags = new[] { new TagDef { Name = "Motor_Run", Type = TagTypeDef.Bool, InitialValue = false } },
                Rungs = Array.Empty<RungDef>(),
            },
            new NetworkDef { Components = Array.Empty<NetworkComponentConfig>() },
            static driverType => throw new InvalidOperationException($"no NETWORK components in this test; unexpected driver type '{driverType}'"));

        controllerA.RunScan();
        controllerB.RunScan();

        var snapshotA = controllerA.GetSnapshot();
        var snapshotB = controllerB.GetSnapshot();

        Assert.AreNotSame(snapshotA.Values, snapshotB.Values);
        Assert.AreEqual(true, snapshotA.Values["Motor_Run"]);
        Assert.AreEqual(false, snapshotB.Values["Motor_Run"]);
    }

    /// <summary>Named constant mirroring <c>PlcEmulator.Drivers.DriverFactory.DiscreteSensor</c>, kept test-local so <c>PlcEmulator.Tests</c> does not need a hard dependency on driver-type string literals living elsewhere for this architectural check.</summary>
    private static class DriverResolverStub
    {
        public const string DiscreteSensor = "DiscreteSensor";
    }
}
