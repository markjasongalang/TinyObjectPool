using System.Diagnostics;

namespace TinyObjectPool;

public class EvictionManager<T> : IDisposable where T : class
{
    private readonly TimeSpan _idleTimeout;
    private readonly TimeProvider _timeProvider;
    
    private readonly Dictionary<T, ObjectMetadata> _metadata;
    private readonly object _lock = new();

    /// <remarks>
    /// <para>
    /// <see href="https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/volatile">volatile</see>
    /// - indicates that a field might be modified by multiple threads that are executing at the same time.
    /// </para>
    /// <para>
    /// CLR will enforce specific memory ordering rules around reads and writes of this field.
    /// </para>
    /// <para>
    /// A read of this field always reads from main memory (not from a register or stale cache).
    /// </para>
    /// <para>
    /// A write operation is immediately visible to other threads that subsequently read this field.
    /// </para>
    /// <para>
    /// Refer to <see href="https://dotnetdigest.com/why-and-when-to-use-the-volatile-keyword-in-net"/>
    /// </para>
    /// </remarks>
    private volatile bool _disposed;

    /// <summary>
    /// Creates a new eviction manager instance.
    /// </summary>
    /// <param name="idleTimeout">The maximum time period that an unused object can stay in the pool.</param>
    /// <param name="timeProvider">The <see cref="TimeProvider"/> instance in coordination with an <see cref="ObjectPool{T}"/> instance.</param>
    public EvictionManager(TimeSpan idleTimeout, TimeProvider timeProvider)
    {
        _idleTimeout = idleTimeout;
        _timeProvider = timeProvider;
        
        _metadata = new Dictionary<T, ObjectMetadata>();
    }

    /// <summary>
    /// Determines if an object of type <typeparamref name="T"/> from <paramref name="objects"/> will be evicted based 
    /// on its idle time.
    /// </summary>
    /// <param name="objects">The collection of objects to be checked for eviction.</param>
    /// <param name="removeAction">The delegate which handles the removal of the object from the pool.</param>
    public void RunEviction(IEnumerable<T> objects, Action<T> removeAction)
    {
        Stopwatch stopWatch = Stopwatch.StartNew();
        var evictedCount = 0;

        foreach (T obj in objects)
        {
            // CONCERN:
            // Should I place a lock at the whole block below?
            // - If yes, then I won't use the UntrackObject() below since there's also a lock there.

            if (!_metadata.TryGetValue(obj, out ObjectMetadata? objectMetadata))
            {
                //MyClassWithReset convertedObj = (MyClassWithReset)Convert.ChangeType(obj, typeof(MyClassWithReset));
                //Console.WriteLine($"[{DateTimeOffset.Now}] {convertedObj.Name} here at obj metadata check");

                // BUG SCENARIO:
                // -------------
                // Object 1 was evicted first (its metadata was detached), but it remained in the pool
                // since the top of the pool (stack) is Object 2 so it didn't match.
                // 
                // After some time, Object 2 was removed from the pool (because it matched).
                // But Object 1 was skipped because its metadata was already detached beforehand.
                // So, the "[10/12/2026 7:16:50 AM] Eviction run completed: 1 object(s) evicted in 0ms" log that you're seeing
                // is actually pertaining to Object 2.
                // 
                // And then, the Object 1 remains in the pool indefinitely since we cannot validate its metadata (since it's already
                // detached), thus, it cannot be evicted.
                // 
                // How do I solve this?
                // - The solution is to simply call removeAction() delegate below so the object (w/o metadata) wouldn't stay
                // indefinitely in the pool.

                removeAction(obj);
                continue;
            }

            TimeSpan? curObjIdleTime = objectMetadata.GetIdleTime();
            if (curObjIdleTime != null && curObjIdleTime >= _idleTimeout)
            {
                // Remove from pool (implementation is delegated to ObjectPool itself)
                removeAction(obj);

                UntrackObject(obj);
                evictedCount++;
            }
        }

        stopWatch.Stop();

        Console.WriteLine($"[{_timeProvider.GetLocalNow()}] Eviction run completed: {evictedCount} object(s) evicted in {stopWatch.ElapsedMilliseconds}ms");
    }

    /// <summary>
    /// Tracks a new object in the pool.
    /// </summary>
    /// <param name="obj">The object be tracked when it's added to the pool.</param>
    /// <remarks>
    /// <see href="https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.dictionary-2.tryadd?view=net-10.0">TryAdd(TKey, TValue)</see>
    /// - Unlike the Dictionary indexer, this doesn't override the element if the element with the given key exists in the dictionary. If the
    /// key already exists, it does nothing and returns false which is suitable for the <see cref="TrackObject(T)"/>.
    /// </remarks>
    public void TrackObject(T obj)
    {
        lock (_lock)
        {
            _metadata.TryAdd(obj, new ObjectMetadata(_timeProvider));
        }
    }

    /// <summary>
    /// Records the timestamp when an object is accessed from the pool.
    /// </summary>
    /// <param name="obj">The object from the pool to be tracked.</param>
    /// <remarks>
    /// Use the <see href="https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.dictionary-2.trygetvalue?view=net-10.0">TryGetValue(TKey, TValue)</see>
    /// method if your code frequently attempts to access keys that are not in the dictionary.
    /// </remarks>
    public void RecordAccess(T obj)
    {
        lock (_lock)
        {
            if (_metadata.TryGetValue(obj, out ObjectMetadata? objectMetadata))
            {
                objectMetadata.RecordAccess(_timeProvider.GetLocalNow());
            }
        }
    }
    
    /// <summary>
    /// Records the timestamp when an object is returned to the pool.
    /// </summary>
    /// <param name="obj">The object returned to the pool.</param>
    public void RecordReturn(T obj)
    {
        lock (_lock)
        {
            if (_metadata.TryGetValue(obj, out ObjectMetadata? objectMetadata))
            {
                objectMetadata.RecordReturn(_timeProvider.GetLocalNow());
            }
        }
    }

    /// <summary>
    /// Stops tracking the object.
    /// </summary>
    /// <param name="obj">The object from the pool to be untracked.</param>
    public void UntrackObject(T obj)
    {
        lock (_lock)
        {
            _metadata.Remove(obj);
        }
    }

    /// <summary>
    /// Disposes the  eviction manager.
    /// </summary>
    /// <remarks>
    /// This also clears the <see cref="_metadata"/> dictionary.
    /// </remarks>
    public void Dispose()
    {
        if (_disposed)
        {
            return; // Idempotent
        }

        _disposed = true;
        _metadata.Clear();

        Console.WriteLine($"[{_timeProvider.GetLocalNow()}] Eviction manager disposed");
    }
}
