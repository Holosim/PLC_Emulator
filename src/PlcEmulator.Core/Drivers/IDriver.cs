using PlcEmulator.Config;

namespace PlcEmulator.Core.Drivers;

/// <summary>
/// Common contract every NETWORK-defined component driver implements
/// (CORE-209). Adding a new component type means implementing this
/// interface; it never requires touching <see cref="ScanEngine"/> or
/// instruction classes.
/// </summary>
/// <remarks>
/// Deliberately declared in <c>PlcEmulator.Core</c> rather than
/// <c>PlcEmulator.Drivers</c>: <see cref="PlcController"/> holds
/// <c>IDriver[]</c> instances and needs the contract type without a
/// project reference to <c>PlcEmulator.Drivers</c>, which itself must
/// reference <c>PlcEmulator.Core</c> for <see cref="TagTable"/> —
/// defining the interface here (the consumer) rather than alongside
/// the built-in implementations (the producer) avoids a circular
/// project reference. Concrete drivers still live in
/// <c>PlcEmulator.Drivers</c> and implement this interface. Flagged to
/// the Systems Engineer for docs/SDD.md's Coding Standards wording,
/// which currently groups "IDriver + built-in drivers" under one bullet.
/// </remarks>
public interface IDriver
{
    /// <summary>
    /// Binds this driver instance to its owning controller's tag table
    /// and its NETWORK configuration entry, at construction time —
    /// never to a global registry.
    /// </summary>
    void Bind(TagTable tags, NetworkComponentConfig config);

    /// <summary>
    /// Called once per scan, after tag values settle, for drivers that
    /// need to react to state changes (e.g. a sensor driver
    /// recomputing a derived reading).
    /// </summary>
    void OnScanComplete();
}
