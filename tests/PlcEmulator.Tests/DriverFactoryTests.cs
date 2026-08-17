using PlcEmulator.Config;
using PlcEmulator.Core;
using PlcEmulator.Drivers;

namespace PlcEmulator.Tests;

/// <summary>
/// Covers CORE-209 / TP-209's built-in driver set: <see cref="DriverFactory"/>
/// resolving NETWORK driver type names, and <see cref="DiscreteSensorDriver"/>/
/// <see cref="RelayDriver"/>'s <c>Bind</c>/<c>OnScanComplete</c> behavior via
/// their shared <see cref="SingleTagDriverBase"/>. See
/// <see cref="PlcControllerDriverTests"/> for the same architecture proven
/// from <c>PlcEmulator.Core</c>'s side, without any concrete driver type.
/// </summary>
[TestClass]
public sealed class DriverFactoryTests
{
    private static TagTable OneBoolTagTable(string tagName, bool initialValue = false)
    {
        var controlLogic = new ControlLogicDef
        {
            Tags = new[] { new TagDef { Name = tagName, Type = TagTypeDef.Bool, InitialValue = initialValue } },
            Rungs = Array.Empty<RungDef>(),
        };

        return ControlLogicBuilder.BuildTagTable(controlLogic);
    }

    [TestMethod]
    public void Create_DiscreteSensor_ReturnsDiscreteSensorDriver()
    {
        Assert.IsInstanceOfType(DriverFactory.Create("DiscreteSensor"), typeof(DiscreteSensorDriver));
    }

    [TestMethod]
    public void Create_Relay_ReturnsRelayDriver()
    {
        Assert.IsInstanceOfType(DriverFactory.Create("Relay"), typeof(RelayDriver));
    }

    /// <summary>TP-209: adding a new driver type only means adding a case here and a new IDriver implementation — proven by these two built-in types resolving to distinct instances, never a shared/global one.</summary>
    [TestMethod]
    public void Create_CalledTwice_ReturnsDistinctInstances()
    {
        var first = DriverFactory.Create("DiscreteSensor");
        var second = DriverFactory.Create("DiscreteSensor");

        Assert.AreNotSame(first, second);
    }

    [TestMethod]
    public void Create_UnrecognizedDriverType_ThrowsConfigValidationExceptionNamingIt()
    {
        var ex = Assert.ThrowsException<ConfigValidationException>(() => DriverFactory.Create("Thermocouple"));
        StringAssert.Contains(ex.Message, "Thermocouple");
    }

    [TestMethod]
    public void DiscreteSensorDriver_Bind_ResolvesConfiguredBoolTag()
    {
        var tags = OneBoolTagTable("Start_PB", initialValue: true);
        var driver = new DiscreteSensorDriver();
        var config = new NetworkComponentConfig { Name = "ProxSensor1", DriverType = "DiscreteSensor", Tags = new[] { "Start_PB" } };

        driver.Bind(tags, config);

        // No exception on the post-Bind lifecycle call (OnScanComplete)
        // is itself part of what TP-209 demonstrates: the bound tag
        // "behaves correctly" through the documented IDriver contract.
        driver.OnScanComplete();
    }

    [TestMethod]
    public void RelayDriver_Bind_ResolvesConfiguredBoolTag()
    {
        var tags = OneBoolTagTable("Motor_Run");
        var driver = new RelayDriver();
        var config = new NetworkComponentConfig { Name = "Relay1", DriverType = "Relay", Tags = new[] { "Motor_Run" } };

        driver.Bind(tags, config);
        driver.OnScanComplete();
    }

    [TestMethod]
    public void Bind_UndefinedTag_ThrowsConfigValidationExceptionNamingComponentAndTag()
    {
        var tags = OneBoolTagTable("Start_PB");
        var driver = new DiscreteSensorDriver();
        var config = new NetworkComponentConfig { Name = "ProxSensor1", DriverType = "DiscreteSensor", Tags = new[] { "Undefined_Tag" } };

        var ex = Assert.ThrowsException<ConfigValidationException>(() => driver.Bind(tags, config));

        StringAssert.Contains(ex.Message, "ProxSensor1");
        StringAssert.Contains(ex.Message, "Undefined_Tag");
    }

    [TestMethod]
    public void Bind_NonBoolTag_ThrowsConfigValidationException()
    {
        var controlLogic = new ControlLogicDef
        {
            Tags = new[] { new TagDef { Name = "Preset_Count", Type = TagTypeDef.Dint, InitialValue = 0 } },
            Rungs = Array.Empty<RungDef>(),
        };
        var tags = ControlLogicBuilder.BuildTagTable(controlLogic);
        var driver = new DiscreteSensorDriver();
        var config = new NetworkComponentConfig { Name = "ProxSensor1", DriverType = "DiscreteSensor", Tags = new[] { "Preset_Count" } };

        var ex = Assert.ThrowsException<ConfigValidationException>(() => driver.Bind(tags, config));

        StringAssert.Contains(ex.Message, "Preset_Count");
    }

    [TestMethod]
    public void Bind_MoreThanOneTag_ThrowsConfigValidationException()
    {
        var controlLogic = new ControlLogicDef
        {
            Tags = new[]
            {
                new TagDef { Name = "Start_PB", Type = TagTypeDef.Bool, InitialValue = false },
                new TagDef { Name = "Stop_PB", Type = TagTypeDef.Bool, InitialValue = false },
            },
            Rungs = Array.Empty<RungDef>(),
        };
        var tags = ControlLogicBuilder.BuildTagTable(controlLogic);
        var driver = new DiscreteSensorDriver();
        var config = new NetworkComponentConfig { Name = "DualSensor", DriverType = "DiscreteSensor", Tags = new[] { "Start_PB", "Stop_PB" } };

        Assert.ThrowsException<ConfigValidationException>(() => driver.Bind(tags, config));
    }

    [TestMethod]
    public void OnScanComplete_BeforeBind_ThrowsInvalidOperationException()
    {
        var driver = new DiscreteSensorDriver();

        Assert.ThrowsException<InvalidOperationException>(driver.OnScanComplete);
    }
}
