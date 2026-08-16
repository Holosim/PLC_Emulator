namespace PlcEmulator.Config;

/// <summary>
/// Immutable, parsed representation of a NETWORK JSON document
/// (DATA-IN-102): the set of control-network components to
/// instantiate, each bound to one or more CONTROL_LOGIC tags. No PLC
/// logic is embedded here — a component only names a driver type and
/// the tag(s) it binds to; behavior lives entirely in the driver
/// implementation (see <c>PlcEmulator.Core.Drivers.IDriver</c>).
/// </summary>
/// <remarks>
/// Produced by <see cref="ConfigLoader.LoadNetwork"/> and never
/// mutated afterward. Cross-referencing each <see cref="NetworkComponentConfig"/>'s
/// tag binding(s) against a loaded <see cref="ControlLogicDef"/> is
/// DATA-IN-103 (<see cref="ConfigLoader.Validate"/>), not this type's
/// concern — this model is valid to construct in isolation from
/// CONTROL_LOGIC (TP-102).
/// </remarks>
public sealed class NetworkDef
{
    /// <summary>
    /// The network's components, in the order they appeared in the
    /// NETWORK JSON document.
    /// </summary>
    public required IReadOnlyList<NetworkComponentConfig> Components { get; init; }
}

/// <summary>
/// One NETWORK-defined component: a name, the driver type that
/// implements it, and the CONTROL_LOGIC tag(s) it binds to
/// (DATA-IN-102). Passed to <c>IDriver.Bind</c> at construction.
/// </summary>
public sealed class NetworkComponentConfig
{
    /// <summary>Component instance name, unique within the NETWORK document (e.g. <c>"ProxSensor1"</c>).</summary>
    public required string Name { get; init; }

    /// <summary>
    /// Driver type reference (e.g. <c>"DiscreteSensor"</c>, <c>"Relay"</c>)
    /// — resolved to a concrete <c>IDriver</c> implementation by the
    /// Host at PlcController construction time, not by the Config
    /// Loader itself.
    /// </summary>
    public required string DriverType { get; init; }

    /// <summary>
    /// The CONTROL_LOGIC tag name(s) this component binds to — one or
    /// more, per DATA-IN-102. Always at least one entry; the loader
    /// rejects a component with no tag binding.
    /// </summary>
    public required IReadOnlyList<string> Tags { get; init; }
}
