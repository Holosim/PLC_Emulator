namespace PlcEmulator.Core.Drivers;

/// <summary>
/// Resolves a NETWORK-declared driver type name (<see cref="Config.NetworkComponentConfig.DriverType"/>,
/// e.g. <c>"DiscreteSensor"</c>) to a freshly constructed <see cref="IDriver"/>
/// instance, one call per NETWORK component (CORE-209).
/// </summary>
/// <remarks>
/// Declared in <c>PlcEmulator.Core</c> alongside <see cref="IDriver"/> so
/// <see cref="PlcController"/> can accept one without a project reference
/// to <c>PlcEmulator.Drivers</c> (see <see cref="IDriver"/>'s remarks for
/// why that reference can't exist). The concrete resolution logic —
/// mapping type names to the built-in <c>DiscreteSensorDriver</c>,
/// <c>RelayDriver</c>, etc. — lives in <c>PlcEmulator.Drivers.DriverFactory</c>
/// and is supplied to <see cref="PlcController"/>'s constructor by the
/// Host composition root, which is the only layer that references every
/// project (see docs/SDD.md, Coding Standards / Architecture). Throws a
/// descriptive exception (caught once at the Host boundary, per UI-003)
/// for an unrecognized driver type name.
/// </remarks>
public delegate IDriver DriverResolver(string driverType);
