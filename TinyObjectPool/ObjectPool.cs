namespace TinyObjectPool;

/// <summary>
/// Should only be for object lifetime management
/// </summary>
public class ObjectPool<T> where T : class
{
    private readonly Stack<T> _objectPool = new();
    private readonly Func<T> _factory; // Func<T> expects to return a brand-new object (without taking inputs)
    private readonly Action<T>? _reset; // Action<T> accepts the item to reset
    private readonly int _maxSize;
    private readonly object _lock = new();
    private readonly SemaphoreSlim _semaphore; // Thread-agnostic (any thread can call Release method)
    private int _createdCount;

    // Accept a factory delegate
    public ObjectPool(Func<T> factory, int maxSize, Action<T>? reset = null)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        _maxSize = maxSize;
        _reset = reset;

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

        // If object is IResettable, call it automatically
        if (item is IResettable resettable)
        {
            // Ensures callers don't even have to pass a reset delegate
            resettable.Reset();
        }

        // Otherwise, invoke custom reset action delegate if provided
        _reset?.Invoke(item); // Pass existing item

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
