using PlcEmulator.Config;
using PlcEmulator.Core;
using PlcEmulator.Core.Drivers;

namespace PlcEmulator.Drivers;

/// <summary>
/// Built-in driver for a relay NETWORK component (referenced as an
/// example in docs/SDD.md, Architecture / Driver layer).
/// </summary>
public sealed class RelayDriver : IDriver
{
    public void Bind(TagTable tags, NetworkComponentConfig config)
    {
        // TODO: bind to the configured tag (CORE-209).
        throw new NotImplementedException("RelayDriver.Bind is scaffolding only.");
    }

    public void OnScanComplete()
    {
        throw new NotImplementedException("RelayDriver.OnScanComplete is scaffolding only.");
    }
}
