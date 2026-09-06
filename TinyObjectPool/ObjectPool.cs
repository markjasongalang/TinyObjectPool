namespace TinyObjectPool;

/// <summary>
/// Should only be for object lifetime management
/// </summary>
public class ObjectPool<T> where T : class
{
    private readonly Stack<T> _objectPool = new();
    private readonly Func<T> _factory;
    private readonly int _maxSize;
    private readonly object _lock = new();
    private readonly SemaphoreSlim _semaphore; // Thread-agnostic (any thread can call Release method)
    private int _createdCount;

    // Accept a factory delegate
    public ObjectPool(Func<T> factory, int maxSize)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        _maxSize = maxSize;

        // Allows up to _maxSize concurrent rents before blocking
        _semaphore = new SemaphoreSlim(maxSize, maxSize);
    }

    public T Rent()
    {
         // Block if max capacity is reached and no idle objects are available
        _semaphore.Wait();

        lock(_lock)
        {
            // Reuse an idle object if available
            if (Count > 0)
            {
                return _objectPool.Pop();
            }

            // Otherwise, create a new object (guaranteed <= _maxSize by semaphore)
            _createdCount++;
            return _factory(); // Delegate creation to the factory
        }
    }

    public void Return(T item)
    {
        ArgumentNullException.ThrowIfNull(item);

        // TODO: Clean State resets
        // - When object is returned it must be cleansed of old state

        lock (_lock)
        {
            // Push returned object back to pool for reuse
            _objectPool.Push(item);
        }

        // Release the semaphore so a waiting Rent() thread can wake up
        _semaphore.Release();
    }

    public int Count
    {
        get
        {
            lock (_lock)
            {
                return _objectPool.Count;
            }
        }
    }

    public int CreatedObjectCount
    {
        get
        {
            lock (_lock)
            {
                return _createdCount;
            }
        }
    }
}
