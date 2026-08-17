using PlcEmulator.Config;
using PlcEmulator.Core;
using PlcEmulator.Core.Drivers;

namespace PlcEmulator.Tests;

/// <summary>
/// Covers CORE-209 / TP-209 from <see cref="PlcController"/>'s side of
/// the driver architecture: given a <see cref="NetworkDef"/> and a
/// <see cref="DriverResolver"/>, the controller instantiates one driver
/// per component, binds it to its own <see cref="TagTable"/>, and calls
/// <see cref="IDriver.OnScanComplete"/> once per <see cref="PlcController.RunScan"/>
/// — all without <see cref="PlcEmulator.Core"/> referencing any concrete
/// driver type (see <see cref="PlcEmulator.Drivers.DriverFactoryTests"/>
/// for the built-in drivers themselves).
/// </summary>
/// <remarks>
/// Deliberately uses a test-local <see cref="IDriver"/> stub rather than
/// the built-in drivers, exactly as <see cref="ScanEngineTests"/> uses
/// stub instructions rather than real <c>XIC</c>/<c>OTE</c> (issue #9) —
/// this proves the architecture from <see cref="PlcEmulator.Core"/>'s
/// side alone, which is the point of TP-209 ("no changes to core
/// scan/instruction code").
/// </remarks>
[TestClass]
public sealed class PlcControllerDriverTests
{
    /// <summary>Records Bind/OnScanComplete calls so tests can assert wiring order and arguments.</summary>
    private sealed class RecordingDriver : IDriver
    {
        public TagTable? BoundTags { get; private set; }
        public NetworkComponentConfig? BoundConfig { get; private set; }
        public int OnScanCompleteCallCount { get; private set; }

        public void Bind(TagTable tags, NetworkComponentConfig config)
        {
            BoundTags = tags;
            BoundConfig = config;
        }

        public void OnScanComplete() => OnScanCompleteCallCount++;
    }

    private static ControlLogicDef OneBoolTagControlLogic(string tagName) => new()
    {
        Tags = new[] { new TagDef { Name = tagName, Type = TagTypeDef.Bool, InitialValue = false } },
        Rungs = Array.Empty<RungDef>(),
    };

    /// <summary>TP-209: constructing a controller resolves each NETWORK component's driver type and binds it to the controller's own tag table.</summary>
    [TestMethod]
    public void Constructor_OneNetworkComponent_ResolvesAndBindsOneDriver()
    {
        var controlLogic = OneBoolTagControlLogic("Start_PB");
        var component = new NetworkComponentConfig { Name = "ProxSensor1", DriverType = "DiscreteSensor", Tags = new[] { "Start_PB" } };
        var network = new NetworkDef { Components = new[] { component } };
        var driver = new RecordingDriver();
        var resolvedTypes = new List<string>();

        _ = new PlcController(controlLogic, network, driverType =>
        {
            resolvedTypes.Add(driverType);
            return driver;
        });

        CollectionAssert.AreEqual(new[] { "DiscreteSensor" }, resolvedTypes);
        Assert.AreSame(component, driver.BoundConfig);
        Assert.IsNotNull(driver.BoundTags);
        Assert.AreEqual(false, driver.BoundTags!.Get("Start_PB").Value);
    }

    /// <summary>Multiple NETWORK components each get their own resolved driver instance, in declaration order.</summary>
    [TestMethod]
    public void Constructor_MultipleNetworkComponents_ResolvesOneDriverPerComponentInOrder()
    {
        var controlLogic = new ControlLogicDef
        {
            Tags = new[]
            {
                new TagDef { Name = "Start_PB", Type = TagTypeDef.Bool, InitialValue = false },
                new TagDef { Name = "Motor_Run", Type = TagTypeDef.Bool, InitialValue = false },
            },
            Rungs = Array.Empty<RungDef>(),
        };
        var network = new NetworkDef
        {
            Components = new[]
            {
                new NetworkComponentConfig { Name = "ProxSensor1", DriverType = "DiscreteSensor", Tags = new[] { "Start_PB" } },
                new NetworkComponentConfig { Name = "Relay1", DriverType = "Relay", Tags = new[] { "Motor_Run" } },
            },
        };
        var drivers = new List<RecordingDriver>();

        _ = new PlcController(controlLogic, network, _ =>
        {
            var driver = new RecordingDriver();
            drivers.Add(driver);
            return driver;
        });

        Assert.AreEqual(2, drivers.Count);
        Assert.AreEqual("ProxSensor1", drivers[0].BoundConfig!.Name);
        Assert.AreEqual("Relay1", drivers[1].BoundConfig!.Name);
    }

    /// <summary>An empty NETWORK document never invokes the driver resolver and produces a controller with no drivers to notify.</summary>
    [TestMethod]
    public void Constructor_NoNetworkComponents_NeverInvokesDriverFactory()
    {
        var controlLogic = OneBoolTagControlLogic("Start_PB");
        var network = new NetworkDef { Components = Array.Empty<NetworkComponentConfig>() };
        var factoryCalled = false;

        var controller = new PlcController(controlLogic, network, _ =>
        {
            factoryCalled = true;
            throw new InvalidOperationException("should never be called");
        });

        Assert.IsFalse(factoryCalled);
        Assert.IsNotNull(controller);
    }

    /// <summary>TP-209-class scan test: RunScan() calls every bound driver's OnScanComplete exactly once, after rung evaluation, with no change to ScanEngine/instruction code required to add the driver.</summary>
    [TestMethod]
    public void RunScan_WithBoundDriver_CallsOnScanCompleteExactlyOncePerScan()
    {
        var controlLogic = new ControlLogicDef
        {
            Tags = new[] { new TagDef { Name = "Start_PB", Type = TagTypeDef.Bool, InitialValue = true } },
            // Empty instruction list: a valid no-op rung. Rung evaluation
            // itself is CORE-200/CORE-201/202 and already covered by
            // ScanEngineTests — this test's only concern is that RunScan()
            // notifies drivers, so it deliberately avoids depending on
            // real XIC/OTE (still scaffolding as of this issue).
            Rungs = new[] { new RungDef { Instructions = Array.Empty<InstructionDef>() } },
        };
        var network = new NetworkDef
        {
            Components = new[]
            {
                new NetworkComponentConfig { Name = "ProxSensor1", DriverType = "DiscreteSensor", Tags = new[] { "Start_PB" } },
            },
        };
        var driver = new RecordingDriver();

        var controller = new PlcController(controlLogic, network, _ => driver);

        controller.RunScan();
        Assert.AreEqual(1, driver.OnScanCompleteCallCount);

        controller.RunScan();
        Assert.AreEqual(2, driver.OnScanCompleteCallCount);
    }

    /// <summary>An unresolvable driver type propagates the resolver's descriptive exception rather than being swallowed.</summary>
    [TestMethod]
    public void Constructor_DriverFactoryThrows_ExceptionPropagates()
    {
        var controlLogic = OneBoolTagControlLogic("Start_PB");
        var network = new NetworkDef
        {
            Components = new[]
            {
                new NetworkComponentConfig { Name = "Mystery1", DriverType = "NoSuchDriver", Tags = new[] { "Start_PB" } },
            },
        };

        var ex = Assert.ThrowsException<ConfigValidationException>(() =>
            new PlcController(controlLogic, network, driverType =>
                throw new ConfigValidationException($"Unrecognized NETWORK driver type '{driverType}'.")));

        StringAssert.Contains(ex.Message, "NoSuchDriver");
    }
}
