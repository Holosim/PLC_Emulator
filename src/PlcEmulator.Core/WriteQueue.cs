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
    // TODO: thread-safe enqueue (called from the network thread) and
    // drain-all (called at scan start) land here (OUT-401).

    /// <summary>Enqueues a pending write, safe to call from any thread.</summary>
    public void Enqueue(string tagName, object value)
    {
        throw new NotImplementedException("WriteQueue.Enqueue is scaffolding only.");
    }

    /// <summary>Atomically drains and returns all pending writes.</summary>
    public IReadOnlyList<(string TagName, object Value)> DrainAll()
    {
        throw new NotImplementedException("WriteQueue.DrainAll is scaffolding only.");
    }
}
