using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

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
    private readonly ILogger<ObjectPool<T>> _logger;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Eviction manager for idle timeout (optional)
    /// </summary>
    private EvictionManager<T>? _evictionManager;

    private ITimer? _evictionCheckTimer;

    private bool _isDisposed;

    public int Count
    {
        get { lock (_lock) { return _objectPool.Count; } }
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
        bool enableEviction = false,
        TimeProvider? timeProvider = null,
        Action<T>? reset = null,
        TimeSpan? defaultTimeout = null,
        ILogger<ObjectPool<T>>? logger = null)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        _maxSize = maxSize;
        _reset = reset;

        // "At the end of the day, we expect almost no one will use anything other than TimeProvider.System
        // in production usage." - Stephen Toub (from Microsoft)
        // Refer to https://www.eriklieben.com/posts/net8_timeprovider_for_unit_tests/
        _timeProvider = timeProvider ?? TimeProvider.System;

        // NullLogger<T>.Instance
        // - A concrete, operational implementation of ILogger<T> that performs safe, empty no-ops when logging methods
        // are called.
        _logger = logger ?? NullLogger<ObjectPool<T>>.Instance;

        _logger.LogInformation(
            "ObjectPool of type '{Type}' created with MaxSize={MaxSize}, Eviction={EvictionEnabled}",
            typeof(T).Name, 
            maxSize, 
            enableEviction);

        // The pool must either implement IResettable or define a reset method
        if (reset == null && !typeof(IResettable).IsAssignableFrom(typeof(T)))
        {
            throw new InvalidOperationException($"Reset mechanism not defined for type '{typeof(T)}'");
        }

        // Use 5 seconds as reasonable timeout default if none is provided 
        _defaultTimeout = defaultTimeout ?? TimeSpan.FromSeconds(5);

        // Allows up to _maxSize concurrent rents before blocking
        _semaphore = new SemaphoreSlim(maxSize, maxSize);

        if (enableEviction)
        {
            InitEviction();
        }
    }

    /// <summary>
    /// Initializes the eviction manager instance to be used on tracking object eviction.
    /// </summary>
    //[MemberNotNull(nameof(_evictionManager))] // This is for removing the warning if we initialized a non-nullable field outside the constructor
    private void InitEviction()
    {
        _evictionManager = new EvictionManager<T>(idleTimeout: TimeSpan.FromMinutes(5), _timeProvider);

        // Set up periodic checks
        _evictionCheckTimer = _timeProvider.CreateTimer(
            PerformEvictionCheck,
            state: this,
            dueTime: TimeSpan.FromMinutes(1), // Delay before the first call of the callback
            period: TimeSpan.FromMinutes(1)); // Runs every minute

        _logger.LogInformation($"Eviction check will begin after 1 minute");
    }

    /// <summary>
    /// Snapshots the idle objects and asks <see cref="EvictionManager{T}.RunEviction(IEnumerable{T}, Action{T})"/> which ones
    /// to evict. Removal is top-of-stack only: a non-matching object is popped and immediately pushed back, so a buried expired
    /// object is reported by a Debug log instead of removed.
    /// </summary>
    /// <param name="state">
    /// An object containing application-specific information relevant to the method invoked by this method delegate, or null.
    /// </param>
    /// <remarks>
    /// <para>
    /// This method is called by the timer delegate and it does not execute in the thread that created
    /// the timer, but instead executes in a separate thread pool provided by the system.
    /// </para>
    /// <para>
    /// Refer to <see href="https://learn.microsoft.com/en-us/dotnet/api/system.threading.timercallback?view=net-10.0" />.
    /// </para>
    /// <para>
    /// Since we're passing this to the <see cref="Timer(TimerCallback)"/> instantiation, we must include 
    /// the optional <paramref name="state" /> parameter.
    /// </para>
    /// </remarks>
    private void PerformEvictionCheck(object? state)
    {
        var objectPool = (ObjectPool<T>)state!;
        
        // Returns elements in LIFO order (top first)
        T[] objectsToCheck = objectPool._objectPool.ToArray();

        objectPool._evictionManager?.RunEviction(objectsToCheck, removeAction: (obj) =>
        {
            // We're popping off an object from the stack and immediately push it back on mismatch
            if (objectPool._objectPool.TryPop(out T? removed) && EqualityComparer<T>.Default.Equals(removed, obj))
            {
                _logger.LogInformation("An idle object was removed from the pool. Pool count now = {Count}", Count);

                //MyClassWithReset convertedObj = (MyClassWithReset)Convert.ChangeType(obj, typeof(MyClassWithReset));
                //_logger.LogDebug("{ConvertedObjName} was removed. Pool count now: {Count}", convertedObj.Name, Count);
            }
            else if (removed != null)
            {
                // Return the object that we don't want to remove
                objectPool._objectPool.Push(removed);

                // An object returned earlier becomes eligible for eviction before a newer one returned later that
                // now sits on top of it. Since removal can only pop the top, the attempt fails and the popped object
                // is pushed back, leaving the stack unchanged.
                //
                // In the TestConsoleApp scenario, two objects are returned at t=0 and t~5 respectively, with a 15-second
                // idle timeout. The first one is eligible around t=15, while the second is still on top, so the log below appears
                // and the first object is untracked even though it remains in the pool. Around t=20 the second object expires
                // and is removed; the first is then removed as well.
                // 
                // To re-create this condition/scenario for testing:
                // - Configure the pool to have eviction enabled
                // - Configure the eviction manager to have a short idle timeout (e.g., 15 seconds)
                // - Configure the eviction check timer to have have no delay and a short period (e.g., 4 seconds)
                // - Rent 2 objects simultaneously
                // - Delay the return of the 2nd rented object (e.g., 5 seconds)
                // - Wait and observe the eviction until this log appears
                _logger.LogDebug("Tried to remove the object, but it wasn't the target object");
            }
        });

        _logger.LogDebug("Pool count = {Count}", Count);
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

            if (_objectPool.Count > 0) // Reuse an idle object if available
            {
                item = _objectPool.Pop();
            }
            else // Otherwise, create a new object (guaranteed <= _maxSize by semaphore);
            {
                item = _factory(); // Delegate creation to the factory
                _evictionManager?.TrackObject(item);
            }

            _evictionManager?.RecordAccess(item);
        }

        _logger.LogInformation("Successfully rented an object of type '{Type}'", typeof(T).Name);

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
            // If the pool was disposed while an object was rented, ignore the returned object
            if (_isDisposed)
            {
                if (item is IDisposable disposableItem)
                {
                    disposableItem.Dispose();
                }

                _logger.LogWarning("Pool is already disposed, so the returned object will be ignored");
                return;
            }

            // If object is IResettable, call it automatically
            if (item is IResettable resettable)
            {
                // Ensures callers don't even have to pass a reset delegate
                resettable.Reset(); // Note: You may disable or comment this temporarily if you need to investigate.
            }

            // Otherwise, invoke custom reset action delegate if provided
            _reset?.Invoke(item); // Pass existing item

            // Push returned object back to pool for reuse
            _objectPool.Push(item);

            _evictionManager?.RecordReturn(item);
        }

        // Release the semaphore so a waiting Rent() thread can wake up
        _semaphore.Release();

        _logger.LogInformation("Object of type '{Type}' returned to the pool", typeof(T).Name);
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

            _evictionCheckTimer = null;
            _evictionManager?.Dispose();

            // Dispose synchronization primitive
            _semaphore.Dispose();
        }

        _logger.LogInformation("Pool successfully disposed");
    }
}
