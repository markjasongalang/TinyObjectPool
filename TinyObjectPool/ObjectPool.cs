namespace TinyObjectPool;

/// <summary>
/// Should only be for object lifetime management
/// </summary>
public class ObjectPool<T> where T : class
{
    private readonly Stack<T> _objectPool = new();
    private readonly Func<T> _factory;

    // Accept a factory delegate
    public ObjectPool(Func<T> factory)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
    }

    public T? Rent()
    {
        if (_objectPool.Count == 0)
        {
            return _factory(); // Delegate creation to the factory
        }

        return _objectPool.Pop();
    }

    public void Return(T item)
    {
        // TODO: Bounded capacity
        // - Excess objects beyond max size should be disposed

        // TODO: Clean State resets
        // - When object is returned it must be cleansed of old state

        _objectPool.Push(item);
    }

    public int Count => _objectPool.Count;
}
