namespace TinyObjectPool;

/// <summary>
/// With this wrapper, we can avoid:
/// - Double return
/// - Return an object from another pool
/// - Never return
/// </summary>
public struct RentedObject<T> : IDisposable where T : class
{
    private ObjectPool<T>? _pool;

    public T Value { get; }

    /// <summary>
    /// Internal constructor for the rented object
    /// </summary>
    /// <param name="pool">The parent pool to which this RentedObject belongs to</param>
    /// <param name="value">The actual value of the object</param>
    internal RentedObject(ObjectPool<T> pool, T value)
    {
        ArgumentNullException.ThrowIfNull(pool);
        ArgumentNullException.ThrowIfNull(value);

        _pool = pool;
        Value = value;
    }

    /// <summary>
    /// Returns the rented object to its parent pool.
    /// </summary>
    public void Dispose()
    {
        // Atomatically sets _pool to null and returns the previous instance.
        // If Dispose() was already called, targetPool is null and execution exits safely.
        ObjectPool<T>? targetPool = Interlocked.Exchange(ref _pool, null);
        targetPool?.Return(Value);
    }
}
