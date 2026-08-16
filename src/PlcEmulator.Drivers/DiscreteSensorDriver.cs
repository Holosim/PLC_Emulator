using PlcEmulator.Config;
using PlcEmulator.Core;
using PlcEmulator.Core.Drivers;

namespace PlcEmulator.Drivers;

/// <summary>
/// Built-in driver for a discrete (on/off) sensor NETWORK component
/// (referenced as an example in docs/RTVM.md TP-102, TP-209).
/// </summary>
public sealed class DiscreteSensorDriver : IDriver
{
    public void Bind(TagTable tags, NetworkComponentConfig config)
    {
        // TODO: bind to the configured tag (CORE-209).
        throw new NotImplementedException("DiscreteSensorDriver.Bind is scaffolding only.");
    }

    public void OnScanComplete()
    {
        throw new NotImplementedException("DiscreteSensorDriver.OnScanComplete is scaffolding only.");
    }
}
