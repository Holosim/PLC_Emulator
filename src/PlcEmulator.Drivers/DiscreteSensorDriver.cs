namespace PlcEmulator.Drivers;

/// <summary>
/// Built-in driver for a discrete (on/off) sensor NETWORK component
/// (referenced as an example in docs/RTVM.md TP-102, TP-209).
/// </summary>
public sealed class DiscreteSensorDriver : SingleTagDriverBase
{
    protected override string DriverTypeName => "DiscreteSensor";
}
