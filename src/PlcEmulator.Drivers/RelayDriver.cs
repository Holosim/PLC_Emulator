namespace PlcEmulator.Drivers;

/// <summary>
/// Built-in driver for a relay NETWORK component (referenced as an
/// example in docs/SDD.md, Architecture / Driver layer).
/// </summary>
public sealed class RelayDriver : SingleTagDriverBase
{
    protected override string DriverTypeName => "Relay";
}
