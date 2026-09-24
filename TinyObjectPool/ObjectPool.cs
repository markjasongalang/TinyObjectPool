namespace TinyObjectPool;

/// <summary>
/// A thread-safe and bounded object pool that mainly utilizes <see cref="Stack{T}"/> and <see cref="SemaphoreSlim"/>.
/// </summary>
/// <remarks>
/// <para>This should be for object lifetime management only.</para>
/// <para>
/// <see href="https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/where-generic-type-constraint">where</see>
/// is a generic type constraint. A <see href="https://essentialcsharp.com/constraints#constraints">constraint</see> declares the characteristics 
/// that the generic type requires of the type argument supplied for each type parameter.
/// </para>
/// <para>
/// The constraint is set to <see href="https://essentialcsharp.com/structclass-constraints#structclass-constraints">class</see>
/// and not <see href="https://essentialcsharp.com/notnull-constraint#notnull-constraint">notnull</see> because value types
/// (allocation-free) don't need pooling, thus, it's better to focus on supporting reference types instead (i.e., class,
/// interface, delegate, or array types).
/// </para>
/// </remarks>
public class ObjectPool<T> : IDisposable where T : class
{
    private readonly Stack<T> _objectPool = new();
    private readonly Func<T> _factory; // Func<T> expects to return a brand-new object (without taking inputs)
    private readonly int _maxSize;
    private readonly Action<T>? _reset; // Action<T> accepts the item to reset
    private readonly TimeSpan _defaultTimeout;
    private readonly object _lock = new(); // Always private and dedicated
    private readonly SemaphoreSlim _semaphore; // Thread-agnostic (any thread can call Release method)

    private bool _isDisposed;

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

    /// <summary>
    /// Upon instance creation, there is a strict validation for defining the reset mechanism of type T.
    /// </summary>
    /// <remarks>
    /// <list>
    /// <item>
    /// <see href="https://learn.microsoft.com/en-us/dotnet/api/system.func-2?view=net-10.0">Func</see> - delegate that returns a value <br/>
    /// </item>
    /// <item>
    /// <see href="https://learn.microsoft.com/en-us/dotnet/api/system.action-1?view=net-10.0">Action</see> - delegate that does not return a value
    /// </item>
    /// </list>
    /// </remarks>
    public ObjectPool(
        Func<T> factory, 
        int maxSize, 
        Action<T>? reset = null,
        TimeSpan? defaultTimeout = null)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        _maxSize = maxSize;
        _reset = reset;

        // The pool must either implement IResettable or define a reset method
        if (reset == null && !typeof(IResettable).IsAssignableFrom(typeof(T)))
        {
            throw new InvalidOperationException($"Reset mechanism not defined for type '{typeof(T)}'");
        }

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
        // Guard against accessing a disposed pool
        ObjectDisposedException.ThrowIf(_isDisposed, this);

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
        lock (_lock) // We need to lock here since Stack<T> is not thread-safe
        {
            // Double check disposal inside lock frame
            ObjectDisposedException.ThrowIf(_isDisposed, this);

            // Reuse an idle object if available
            // Otherwise, create a new object (guaranteed <= _maxSize by semaphore);
            item = _objectPool.Count > 0 ? _objectPool.Pop() : _factory(); // Delegate creation to the factory
        }

        return new RentedObject<T>(this, item);
    }

    /// <summary>
    /// <para>Returns an object to the pool or disposes it if pool is already disposed.</para>
    /// <para>
    /// This automatically calls the reset mechanism of the object implementing <see cref="IResettable"/> 
    /// (otherwise, the reset action delegate provided upon pool creation).
    /// </para>
    /// <para>Releases the <see cref="SemaphoreSlim"/> object so other waiting tasks can rent the returned object.</para>
    /// </summary>
    /// <param name="item">The object to be returned to the pool or disposed.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when item passed as argument is null.
    /// </exception>
    /// <remarks>
    /// Due to the <c>Dispose()</c> method from <see cref="RentedObject{T}"/>, this
    /// will only be called at most once per object.
    /// </remarks>
    internal void Return(T item)
    {
        // C#'s nullable reference types feature is purely a compile-time static analysis safety check,
        // not a hard runtime constraint that's why we still need to check if T is null below.
        ArgumentNullException.ThrowIfNull(item);

        lock (_lock) // We need to lock here since Stack<T> is not thread-safe
        {
            // If the pool was disposed while an object was rented, dispose the returning object
            if (_isDisposed)
            {
                if (item is IDisposable disposableItem)
                {
                    disposableItem.Dispose();
                }

                return;
            }

            // If object is IResettable, call it automatically
            if (item is IResettable resettable)
            {
                // Ensures callers don't even have to pass a reset delegate
                resettable.Reset();
            }

            // Otherwise, invoke custom reset action delegate if provided
            _reset?.Invoke(item); // Pass existing item

            // Push returned object back to pool for reuse
            _objectPool.Push(item);
        }

        // Release the semaphore so a waiting Rent() thread can wake up
        _semaphore.Release();
    }

    public void Dispose()
    {
        lock (_lock)
        {
            if (_isDisposed)
            {
                return; // Idempotent check
            }

            _isDisposed = true;

            // Dispose any pooled objects sitting in the stack if T is IDisposable
            while (_objectPool.Count > 0)
            {
                T item = _objectPool.Pop();
                if (item is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }

            // Dispose synchronization primitive
            _semaphore.Dispose();
        }
    }
}
