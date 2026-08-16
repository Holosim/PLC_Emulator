namespace PlcEmulator.Core;

/// <summary>
/// Holds pending <c>tag_write</c> requests (OUT-401) between the time
/// they arrive on the network I/O thread and the start of the owning
/// <see cref="PlcController"/>'s next scan, where they are drained
/// atomically. This is what keeps scan evaluation single-threaded and
/// keeps the network thread from ever mutating <see cref="TagTable"/>
/// directly (see docs/SDD.md, Architecture / write path note).
/// </summary>
public sealed class WriteQueue
{
    private readonly object _gate = new();
    private List<(string TagName, object Value)> _pending = new();

    /// <summary>
    /// Enqueues a pending write, safe to call from any thread — this is
    /// the only way the network I/O thread is ever allowed to record a
    /// tag_write (OUT-401); it never touches <see cref="TagTable"/>
    /// directly (see docs/SDD.md, Architecture / write path note).
    /// </summary>
    public void Enqueue(string tagName, object value)
    {
        lock (_gate)
        {
            _pending.Add((tagName, value));
        }
    }

    /// <summary>
    /// Atomically drains and returns all writes queued since the last
    /// call, in the order they were enqueued. Called once per scan, at
    /// scan start (CORE-200), so writes are applied between scans —
    /// never mid-scan.
    /// </summary>
    public IReadOnlyList<(string TagName, object Value)> DrainAll()
    {
        lock (_gate)
        {
            if (_pending.Count == 0)
            {
                return Array.Empty<(string, object)>();
            }

            var drained = _pending;
            _pending = new List<(string TagName, object Value)>();
            return drained;
        }
    }
}
