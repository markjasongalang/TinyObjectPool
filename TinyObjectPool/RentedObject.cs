namespace TinyObjectPool;

/// <summary>
/// With this wrapper, we can avoid:
/// - Double return
/// - Return an object from another pool
/// - Never return
/// </summary>
public struct RentedObject<T> : IDisposable where T : class
{
    private readonly ObjectPool<T> _pool;
    private bool _isDisposed;

    public T Value { get; }

    internal RentedObject(ObjectPool<T> pool, T value)
    {
        _pool = pool;
        _isDisposed = false;

        Value = value;
    }

    public void Dispose()
    {
        if (_isDisposed || _pool == null)
        {
            return; // Idempotent: Prevents double-returning to the pool
        }

        _isDisposed = true;
        _pool.Return(Value);
    }
}
