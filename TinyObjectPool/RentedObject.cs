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
    private bool _isDisposed;

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
        _isDisposed = false;

        Value = value;
    }

    /// <summary>
    /// Returns the rented object to its parent pool.
    /// </summary>
    /// <remarks>
    /// This should be callable multiple times without throwing an exception.
    /// Refer to <see href="https://stackoverflow.com/questions/8923853/should-idisposable-dispose-implementations-be-idempotent"/>
    /// </remarks>
    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        // Automatically sets _pool to null and returns the previous instance.
        // If Dispose() was already called, targetPool is null and execution exits safely.
        ObjectPool<T>? targetPool = Interlocked.Exchange(ref _pool, null);
        targetPool?.Return(Value);

        _isDisposed = true;
    }
}
