using PlcEmulator.Config;
using PlcEmulator.Core;
using PlcEmulator.Network;

namespace PlcEmulator.Tests;

/// <summary>
/// Verifies DATA-OUT-301 (docs/RTVM.md TP-301): a <see cref="TagSnapshot"/>
/// (DATA-OUT-300) serializes to the exact <c>tag_update</c> TCP/JSON
/// wire text specified by docs/SDD.md's Interface Control Document.
/// </summary>
/// <remarks>
/// TP-301 reuses TP-300's runtime state
/// (<c>Start_PB=false, Motor_Run=true, Preset_Count=5</c>) — see
/// <see cref="PlcControllerSnapshotTests"/> for how that state is
/// produced from a rung-free scan pending real <c>XIC</c>/<c>OTE</c>
/// (CORE-201/202). This class covers only the serialization step
/// itself, independent of the TCP transport (OUT-400, issue #20,
/// still on hold) that will eventually call
/// <see cref="TagUpdateSerializer.Serialize"/> from
/// <see cref="TcpJsonServer.Broadcast"/>.
/// </remarks>
[TestClass]
public sealed class TagUpdateSerializerTests
{
    private static PlcController BuildController(params TagDef[] tags)
    {
        var controlLogic = new ControlLogicDef { Tags = tags, Rungs = Array.Empty<RungDef>() };
        return new PlcController(
            controlLogic,
            new NetworkDef { Components = Array.Empty<NetworkComponentConfig>() },
            static driverType => throw new InvalidOperationException($"no NETWORK components in this test; unexpected driver type '{driverType}'"));
    }

    /// <summary>TP-301: matches `{"type":"tag_update","tags":{"Start_PB":false,"Motor_Run":true,"Preset_Count":5}}` exactly.</summary>
    [TestMethod]
    public void Serialize_MatchesIcdWireFormat()
    {
        var controller = BuildController(
            new TagDef { Name = "Start_PB", Type = TagTypeDef.Bool, InitialValue = false },
            new TagDef { Name = "Motor_Run", Type = TagTypeDef.Bool, InitialValue = true },
            new TagDef { Name = "Preset_Count", Type = TagTypeDef.Dint, InitialValue = 5 });

        controller.RunScan();
        var snapshot = controller.GetSnapshot();

        var json = TagUpdateSerializer.Serialize(snapshot);

        Assert.AreEqual(
            "{\"type\":\"tag_update\",\"tags\":{\"Start_PB\":false,\"Motor_Run\":true,\"Preset_Count\":5}}",
            json);
    }

    /// <summary>A REAL-typed tag serializes as a JSON number, not a string, per the ICD.</summary>
    [TestMethod]
    public void Serialize_RealTag_EmitsJsonNumber()
    {
        var controller = BuildController(new TagDef { Name = "Speed", Type = TagTypeDef.Real, InitialValue = 3.5 });

        var json = TagUpdateSerializer.Serialize(controller.GetSnapshot());

        Assert.AreEqual("{\"type\":\"tag_update\",\"tags\":{\"Speed\":3.5}}", json);
    }

    /// <summary>Timer/counter tags never appear in the wire text — the snapshot already excludes them (DATA-OUT-300).</summary>
    [TestMethod]
    public void Serialize_ExcludesTimerAndCounterTags()
    {
        var controller = BuildController(
            new TagDef { Name = "Start_PB", Type = TagTypeDef.Bool, InitialValue = false },
            new TagDef { Name = "DelayTimer", Type = TagTypeDef.Timer, Preset = 1000 });

        var json = TagUpdateSerializer.Serialize(controller.GetSnapshot());

        Assert.AreEqual("{\"type\":\"tag_update\",\"tags\":{\"Start_PB\":false}}", json);
    }

    /// <summary>An empty snapshot still serializes to a well-formed (empty) `tags` object, not `null` or an error.</summary>
    [TestMethod]
    public void Serialize_EmptySnapshot_EmitsEmptyTagsObject()
    {
        var controller = BuildController();

        var json = TagUpdateSerializer.Serialize(controller.GetSnapshot());

        Assert.AreEqual("{\"type\":\"tag_update\",\"tags\":{}}", json);
    }
}
