namespace TinyObjectPool;

public class ObjectPool<T> where T : class, new() // Compiler needs to know 'T' has a parameterless constructor
{
    private readonly Stack<T> _objectPool;

    public ObjectPool()
    {
        _objectPool = new Stack<T>();
    }

    public T? Rent()
    {
        if (_objectPool.Count == 0)
        {
            _objectPool.Push(new T());
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
