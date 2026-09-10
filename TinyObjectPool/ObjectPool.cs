namespace TinyObjectPool;

/// <summary>
/// A thread-safe, bounded object pool backed by a stack and managed via a SemaphoreSlim.
/// </summary>
/// <remarks>
/// Should only be for object lifetime managemen
/// </remarks>
public class ObjectPool<T> : IDisposable where T : class
{
    private readonly Stack<T> _objectPool = new();
    private readonly Func<T> _factory; // Func<T> expects to return a brand-new object (without taking inputs)
    private readonly int _maxSize;
    private readonly Action<T>? _reset; // Action<T> accepts the item to reset
    private readonly TimeSpan _defaultTimeout;
    private readonly object _lock = new();
    private readonly SemaphoreSlim _semaphore; // Thread-agnostic (any thread can call Release method)

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

    // Accept a factory delegate
    public ObjectPool(
        Func<T> factory, 
        int maxSize, 
        Action<T>? reset = null,
        TimeSpan? defaultTimeout = null)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        _maxSize = maxSize;
        _reset = reset;

        // Use 5 seconds as reasonable timeout default if none is provided 
        _defaultTimeout = defaultTimeout ?? TimeSpan.FromSeconds(5);

        // Allows up to _maxSize concurrent rents before blocking
        _semaphore = new SemaphoreSlim(maxSize, maxSize);
    }

    /// <summary>
    /// Rents an object wrapped in a scope-managed struct.
    /// </summary>
    /// <param name="timeout">Optional timeout duration (allow callers to override when needed).</param>
    public RentedObject<T> Rent(TimeSpan? timeout = null)
    {
        TimeSpan effectiveTimeout = timeout ?? _defaultTimeout;

         // Block if max capacity is reached and no idle objects are available
        if (!_semaphore.Wait(effectiveTimeout))
        {
            // Prevent indefinite thread deadlocks
            throw new TimeoutException(
                $"ObjectPool<{typeof(T).Name}> capacity of {_maxSize} exhausted. " +
                "A rented object was likely leaked without calling Dispose() or 'using'.");
        }

        T item;
        lock (_lock)
        {
            // Reuse an idle object if available
            // Otherwise, create a new object (guaranteed <= _maxSize by semaphore);
            item = _objectPool.Count > 0 ? _objectPool.Pop() : _factory(); // Delegate creation to the factory
        }

        return new RentedObject<T>(this, item);
    }

    internal void Return(T item)
    {
        // C#'s nullable reference types feature is purely a compile-time static analysis safety check,
        // not a hard runtime constraint that's why we still need to check if T is null below.
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

    public void Dispose()
    {
        // I think we should just clear the list here or not?

        throw new NotImplementedException();
    }
}
