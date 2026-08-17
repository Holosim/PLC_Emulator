using PlcEmulator.Config;

namespace PlcEmulator.Tests;

/// <summary>
/// Covers DATA-IN-103 / TP-005 / TP-103: cross-file validation that
/// every NETWORK component's tag binding references a tag that
/// actually exists in CONTROL_LOGIC.
/// </summary>
[TestClass]
public sealed class ConfigLoaderValidateTests
{
    private static ControlLogicDef ControlLogicWithTags(params string[] names)
    {
        var tags = names
            .Select(name => new TagDef { Name = name, Type = TagTypeDef.Bool, InitialValue = false })
            .ToList();
        return new ControlLogicDef { Tags = tags, Rungs = new List<RungDef>() };
    }

    private static NetworkDef NetworkWithComponent(string componentName, params string[] tagNames)
    {
        var component = new NetworkComponentConfig
        {
            Name = componentName,
            DriverType = "DiscreteSensor",
            Tags = tagNames,
        };
        return new NetworkDef { Components = new List<NetworkComponentConfig> { component } };
    }

    [TestMethod]
    public void Validate_AllTagsDefined_DoesNotThrow()
    {
        var controlLogic = ControlLogicWithTags("Start_PB");
        var network = NetworkWithComponent("ProxSensor1", "Start_PB");

        ConfigLoader.Validate(controlLogic, network);
    }

    /// <summary>
    /// TP-005 / TP-103: NETWORK component
    /// <c>{"name":"ProxSensor1","driver":"DiscreteSensor","tag":"Undefined_Tag"}</c>
    /// where <c>Undefined_Tag</c> is not defined in CONTROL_LOGIC →
    /// descriptive error naming the component and the undefined tag.
    /// </summary>
    [TestMethod]
    public void Validate_UndefinedTagReference_ThrowsDescriptiveError()
    {
        var controlLogic = ControlLogicWithTags("Start_PB");
        var network = NetworkWithComponent("ProxSensor1", "Undefined_Tag");

        var ex = Assert.ThrowsException<ConfigValidationException>(
            () => ConfigLoader.Validate(controlLogic, network));

        StringAssert.Contains(ex.Message, "ProxSensor1");
        StringAssert.Contains(ex.Message, "Undefined_Tag");
    }

    [TestMethod]
    public void Validate_MultiTagComponent_OneUndefinedTag_ThrowsNamingThatTag()
    {
        var controlLogic = ControlLogicWithTags("Start_PB");
        var network = NetworkWithComponent("DualSensor", "Start_PB", "Stop_PB");

        var ex = Assert.ThrowsException<ConfigValidationException>(
            () => ConfigLoader.Validate(controlLogic, network));

        StringAssert.Contains(ex.Message, "DualSensor");
        StringAssert.Contains(ex.Message, "Stop_PB");
    }

    [TestMethod]
    public void Validate_NoComponents_DoesNotThrow()
    {
        var controlLogic = ControlLogicWithTags("Start_PB");
        var network = new NetworkDef { Components = new List<NetworkComponentConfig>() };

        ConfigLoader.Validate(controlLogic, network);
    }
}
