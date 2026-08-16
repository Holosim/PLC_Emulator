namespace PlcEmulator.Config;

/// <summary>
/// Immutable, parsed representation of a NETWORK JSON document
/// (DATA-IN-102): the set of control-network components to
/// instantiate, each bound to a CONTROL_LOGIC tag.
/// </summary>
public sealed class NetworkDef
{
    // TODO: ordered collection of NetworkComponentConfig entries lands
    // here as scaffolding gives way to real parsing (DATA-IN-102).
}

/// <summary>
/// One NETWORK-defined component: a name, the driver type that
/// implements it, and the CONTROL_LOGIC tag(s) it binds to
/// (DATA-IN-102). Passed to <c>IDriver.Bind</c> at construction.
/// </summary>
public sealed class NetworkComponentConfig
{
    // TODO: Name, DriverType, and tag binding(s) land here (DATA-IN-102).
}
