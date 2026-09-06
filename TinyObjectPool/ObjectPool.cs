namespace TinyObjectPool;

public class ObjectPool<T> where T : class, new() // Compiler needs to know 'T' has a parameterless constructor
{
    private readonly Stack<T> _objectPool;

    public ObjectPool(int maxSize)
    {
        _objectPool = new Stack<T>(maxSize);

        for (var i = 0; i < maxSize; i++)
        {
            _objectPool.Push(new T());
        }
    }

    public T? Rent()
    {
        return _objectPool.Pop();
    }

    public void Return(T item)
    {
        _objectPool.Push(item);
    }
}
