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
    private IReadOnlyList<IDriver> _drivers = Array.Empty<IDriver>();

    /// <summary>
    /// Constructs a controller from a validated CONTROL_LOGIC/NETWORK
    /// definition pair, per the Config Loader/Validator step (DATA-IN-103).
    /// </summary>
    public PlcController(ControlLogicDef controlLogic, NetworkDef network)
    {
        _tags = ControlLogicBuilder.BuildTagTable(controlLogic);
        _rungs = ControlLogicBuilder.BuildRungs(controlLogic);

        // TODO: build the driver set from `network` (DATA-IN-102, CORE-209).
    }

    /// <summary>
    /// Drains any pending writes, then evaluates every rung once
    /// (CORE-200), then notifies drivers that the scan has completed.
    /// </summary>
    public void RunScan()
    {
        throw new NotImplementedException("PlcController.RunScan is scaffolding only.");
    }

    /// <summary>Returns a point-in-time snapshot of current tag values (DATA-OUT-300/301).</summary>
    public TagSnapshot GetSnapshot()
    {
        throw new NotImplementedException("PlcController.GetSnapshot is scaffolding only.");
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
