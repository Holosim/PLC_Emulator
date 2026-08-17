using PlcEmulator.Config;
using PlcEmulator.Core.Drivers;

namespace PlcEmulator.Drivers;

/// <summary>
/// Resolves a NETWORK-declared driver type name to a new built-in
/// <see cref="IDriver"/> instance (CORE-209). This is the concrete
/// implementation the Host passes as a <see cref="DriverResolver"/> to
/// <c>PlcController</c>'s constructor; it lives here — a leaf project —
/// rather than inside <c>PlcEmulator.Core</c>, since <c>Core</c> cannot
/// reference <c>PlcEmulator.Drivers</c> (see docs/SDD.md, Coding
/// Standards, and <see cref="IDriver"/>'s remarks).
/// </summary>
/// <remarks>
/// Adding a new built-in component type means adding one case here plus
/// one new <see cref="IDriver"/> implementation — it never requires
/// touching <c>PlcEmulator.Core</c>'s scan engine or instruction
/// classes, which is exactly what TP-209 demonstrates.
/// </remarks>
public static class DriverFactory
{
    /// <summary>Driver type name for <see cref="DiscreteSensorDriver"/>, as it appears in NETWORK JSON's <c>"driver"</c> field.</summary>
    public const string DiscreteSensor = "DiscreteSensor";

    /// <summary>Driver type name for <see cref="RelayDriver"/>, as it appears in NETWORK JSON's <c>"driver"</c> field.</summary>
    public const string Relay = "Relay";

    /// <summary>
    /// Creates a new driver instance for <paramref name="driverType"/>.
    /// Suitable to pass directly as a <see cref="DriverResolver"/>
    /// (matches its delegate signature).
    /// </summary>
    /// <exception cref="ConfigValidationException">
    /// <paramref name="driverType"/> does not name a built-in driver.
    /// </exception>
    public static IDriver Create(string driverType) => driverType switch
    {
        DiscreteSensor => new DiscreteSensorDriver(),
        Relay => new RelayDriver(),
        _ => throw new ConfigValidationException(
            $"Unrecognized NETWORK driver type '{driverType}'. Must be one of the built-in driver types: " +
            $"{DiscreteSensor}, {Relay} (CORE-209)."),
    };
}
