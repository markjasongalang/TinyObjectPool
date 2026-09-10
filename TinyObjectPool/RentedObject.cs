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

    internal RentedObject(ObjectPool<T> pool, T value)
    {
        _pool = pool;
        Value = value;
    }

    public void Dispose()
    {
        // Atomatically sets _pool to null and returns the previous instance.
        // If Dispose() was already called, targetPool is null and execution exits safely.
        ObjectPool<T>? targetPool = Interlocked.Exchange(ref _pool, null);
        targetPool?.Return(Value);
    }
}
