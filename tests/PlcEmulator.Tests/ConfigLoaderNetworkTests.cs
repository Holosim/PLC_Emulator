using PlcEmulator.Config;

namespace PlcEmulator.Tests;

/// <summary>
/// Covers DATA-IN-102 / TP-102: parsing the NETWORK JSON schema in
/// isolation (no CONTROL_LOGIC document required — cross-file tag
/// validation is DATA-IN-103, issue #8).
/// </summary>
[TestClass]
public sealed class ConfigLoaderNetworkTests
{
    private static string WriteTempNetworkFile(string json)
    {
        var path = Path.Combine(Path.GetTempPath(), $"network_{Guid.NewGuid():N}.json");
        File.WriteAllText(path, json);
        return path;
    }

    /// <summary>
    /// TP-102: <c>{"name":"ProxSensor1","driver":"DiscreteSensor","tag":"Start_PB"}</c>
    /// → 1 component instantiated with driver type <c>DiscreteSensor</c>,
    /// bound to tag <c>Start_PB</c>.
    /// </summary>
    [TestMethod]
    public void LoadNetwork_TP102_SingleComponentWithSingularTag_ParsesCorrectly()
    {
        var path = WriteTempNetworkFile("""
            {
              "components": [
                { "name": "ProxSensor1", "driver": "DiscreteSensor", "tag": "Start_PB" }
              ]
            }
            """);

        try
        {
            var network = ConfigLoader.LoadNetwork(path);

            Assert.AreEqual(1, network.Components.Count);
            var component = network.Components[0];
            Assert.AreEqual("ProxSensor1", component.Name);
            Assert.AreEqual("DiscreteSensor", component.DriverType);
            CollectionAssert.AreEqual(new[] { "Start_PB" }, component.Tags.ToList());
        }
        finally
        {
            File.Delete(path);
        }
    }

    [TestMethod]
    public void LoadNetwork_MultipleComponents_ParsesAllInOrder()
    {
        var path = WriteTempNetworkFile("""
            {
              "components": [
                { "name": "ProxSensor1", "driver": "DiscreteSensor", "tag": "Start_PB" },
                { "name": "Relay1", "driver": "Relay", "tag": "Motor_Run" }
              ]
            }
            """);

        try
        {
            var network = ConfigLoader.LoadNetwork(path);

            Assert.AreEqual(2, network.Components.Count);
            Assert.AreEqual("ProxSensor1", network.Components[0].Name);
            Assert.AreEqual("Relay1", network.Components[1].Name);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [TestMethod]
    public void LoadNetwork_PluralTagsArray_BindsToAllTags()
    {
        var path = WriteTempNetworkFile("""
            {
              "components": [
                { "name": "DualSensor", "driver": "DiscreteSensor", "tags": ["Start_PB", "Stop_PB"] }
              ]
            }
            """);

        try
        {
            var network = ConfigLoader.LoadNetwork(path);

            CollectionAssert.AreEqual(
                new[] { "Start_PB", "Stop_PB" },
                network.Components[0].Tags.ToList());
        }
        finally
        {
            File.Delete(path);
        }
    }

    [TestMethod]
    public void LoadNetwork_NoPlcLogicFieldsExist_OnlyNameDriverAndTagBinding()
    {
        // DATA-IN-102: "with no PLC logic embedded in the network
        // definition itself" — the parsed model exposes exactly name,
        // driver type, and tag binding(s); nothing else.
        var path = WriteTempNetworkFile("""
            {
              "components": [
                { "name": "ProxSensor1", "driver": "DiscreteSensor", "tag": "Start_PB" }
              ]
            }
            """);

        try
        {
            var network = ConfigLoader.LoadNetwork(path);
            var componentType = typeof(NetworkComponentConfig);

            var publicPropertyNames = componentType.GetProperties().Select(p => p.Name).OrderBy(n => n).ToArray();
            CollectionAssert.AreEqual(new[] { "DriverType", "Name", "Tags" }, publicPropertyNames);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [TestMethod]
    public void LoadNetwork_MissingName_ThrowsConfigValidationException()
    {
        var path = WriteTempNetworkFile("""
            { "components": [ { "driver": "DiscreteSensor", "tag": "Start_PB" } ] }
            """);

        try
        {
            var ex = Assert.ThrowsException<ConfigValidationException>(() => ConfigLoader.LoadNetwork(path));
            StringAssert.Contains(ex.Message, "name");
        }
        finally
        {
            File.Delete(path);
        }
    }

    [TestMethod]
    public void LoadNetwork_MissingDriver_ThrowsConfigValidationException()
    {
        var path = WriteTempNetworkFile("""
            { "components": [ { "name": "ProxSensor1", "tag": "Start_PB" } ] }
            """);

        try
        {
            var ex = Assert.ThrowsException<ConfigValidationException>(() => ConfigLoader.LoadNetwork(path));
            StringAssert.Contains(ex.Message, "driver");
        }
        finally
        {
            File.Delete(path);
        }
    }

    [TestMethod]
    public void LoadNetwork_NoTagBinding_ThrowsConfigValidationException()
    {
        var path = WriteTempNetworkFile("""
            { "components": [ { "name": "ProxSensor1", "driver": "DiscreteSensor" } ] }
            """);

        try
        {
            var ex = Assert.ThrowsException<ConfigValidationException>(() => ConfigLoader.LoadNetwork(path));
            StringAssert.Contains(ex.Message, "ProxSensor1");
        }
        finally
        {
            File.Delete(path);
        }
    }

    [TestMethod]
    public void LoadNetwork_NoComponents_ThrowsConfigValidationException()
    {
        var path = WriteTempNetworkFile("""{ "components": [] }""");

        try
        {
            Assert.ThrowsException<ConfigValidationException>(() => ConfigLoader.LoadNetwork(path));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [TestMethod]
    public void LoadNetwork_MalformedJson_ThrowsConfigValidationException()
    {
        var path = WriteTempNetworkFile("{ this is not json");

        try
        {
            Assert.ThrowsException<ConfigValidationException>(() => ConfigLoader.LoadNetwork(path));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [TestMethod]
    public void LoadNetwork_FileDoesNotExist_ThrowsConfigValidationException()
    {
        var path = Path.Combine(Path.GetTempPath(), $"missing_{Guid.NewGuid():N}.json");

        Assert.ThrowsException<ConfigValidationException>(() => ConfigLoader.LoadNetwork(path));
    }
}
