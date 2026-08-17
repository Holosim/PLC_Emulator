using PlcEmulator.Config;
using PlcEmulator.Core.Drivers;

namespace PlcEmulator.Core;

/// <summary>
/// The unit of isolation (NFR-500). Holds one controller's
/// <see cref="TagTable"/> (DATA-OUT-300), its parsed rung program, its
/// instantiated driver set, and its incoming-write queue. Two
/// <see cref="PlcController"/> instances constructed side by side in
/// the same process share no mutable state — each owns its own tag
/// table, driver instances, and scan state (see docs/SDD.md,
/// Architecture).
/// </summary>
public sealed class PlcController
{
    private readonly TagTable _tags;
    private readonly ScanEngine _scanEngine = new();
    private readonly WriteQueue _pendingWrites = new();
    private readonly IReadOnlyList<Rung> _rungs;
    private readonly IReadOnlyList<IDriver> _drivers;

    /// <summary>
    /// Constructs a controller from a validated CONTROL_LOGIC/NETWORK
    /// definition pair, per the Config Loader/Validator step (DATA-IN-103).
    /// Instantiates one driver per NETWORK component via
    /// <paramref name="driverFactory"/> (CORE-209) and binds each to this
    /// controller's own <see cref="TagTable"/> — never a global registry
    /// — so driver instances are as isolated per-controller as the tag
    /// table itself (NFR-500).
    /// </summary>
    /// <param name="controlLogic">Parsed CONTROL_LOGIC definition (DATA-IN-100/101).</param>
    /// <param name="network">Parsed NETWORK definition (DATA-IN-102).</param>
    /// <param name="driverFactory">
    /// Resolves each component's <see cref="NetworkComponentConfig.DriverType"/>
    /// to a concrete <see cref="IDriver"/> instance — supplied by the Host,
    /// which is the only layer that knows about built-in driver
    /// implementations (see <see cref="DriverResolver"/>'s remarks).
    /// </param>
    public PlcController(ControlLogicDef controlLogic, NetworkDef network, DriverResolver driverFactory)
    {
        _tags = ControlLogicBuilder.BuildTagTable(controlLogic);
        _rungs = ControlLogicBuilder.BuildRungs(controlLogic);
        _drivers = BuildDrivers(network, driverFactory, _tags);
    }

    private static IReadOnlyList<IDriver> BuildDrivers(NetworkDef network, DriverResolver driverFactory, TagTable tags)
    {
        if (network.Components.Count == 0)
        {
            return Array.Empty<IDriver>();
        }

        var drivers = new List<IDriver>(network.Components.Count);

        foreach (var component in network.Components)
        {
            var driver = driverFactory(component.DriverType);
            driver.Bind(tags, component);
            drivers.Add(driver);
        }

        return drivers;
    }

    /// <summary>
    /// Drains any pending writes, then evaluates every rung once
    /// (CORE-200), then notifies drivers that the scan has completed.
    /// </summary>
    public void RunScan()
    {
        foreach (var (tagName, value) in _pendingWrites.DrainAll())
        {
            _tags.Set(tagName, value);
        }

        _scanEngine.Evaluate(_rungs, _tags);

        foreach (var driver in _drivers)
        {
            driver.OnScanComplete();
        }
    }

    /// <summary>
    /// Returns a point-in-time snapshot of current tag values
    /// (DATA-OUT-300), queryable by the rest of the system (e.g. the
    /// TCP/JSON server, DATA-OUT-301). Only scalar
    /// (<see cref="TagType.Bool"/>/<see cref="TagType.Dint"/>/
    /// <see cref="TagType.Real"/>) tag values are included, per
    /// <see cref="TagSnapshot"/>'s ICD note — structured timer/counter
    /// sub-elements stay in this controller's own <see cref="TagTable"/>.
    /// </summary>
    public TagSnapshot GetSnapshot()
    {
        var values = new Dictionary<string, object>(StringComparer.Ordinal);

        foreach (var tag in _tags.AllTags)
        {
            if (tag.Value is not null)
            {
                values[tag.Name] = tag.Value;
            }
        }

        return new TagSnapshot(values);
    }

    /// <summary>
    /// Queues an input tag write (OUT-401), applied atomically at the
    /// start of the next scan — never mid-scan, and never called
    /// directly on <see cref="TagTable"/> from the network thread.
    /// </summary>
    public void QueueWrite(string tagName, object value)
    {
        throw new NotImplementedException("PlcController.QueueWrite is scaffolding only.");
    }
}
