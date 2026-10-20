namespace TinyObjectPool;

/// <summary>
/// With this wrapper, we can avoid:
/// <list type="bullet">
/// <item>Double return</item>
/// <item>Return an object from another pool</item>
/// <item>Never return</item>
/// </list>
/// </summary>
/// <remarks>
/// <para>
/// It's typical when implementing the <see cref="IAsyncDisposable"/> interface that classes also implement the
/// <see cref="IDisposable"/> interface.
/// </para>
/// <para>
/// If you implement the <see cref="IAsyncDisposable"/> interface but not the <see cref="IDisposable"/> interface,
/// your app can potentially leak resources. If a class implements <see cref="IAsyncDisposable"/>, but not <see cref="IDisposable"/>,
/// and a consumer only calls <see cref="Dispose"/>, your implementation wouuld never call <see cref="DisposeAsync"/>. This would
/// result in a resource leak.
/// </para>
/// <para>
/// Refer to <see href="https://learn.microsoft.com/en-us/dotnet/standard/garbage-collection/implementing-disposeasync"/>
/// </para>
/// </remarks>
public struct RentedObject<T> : IDisposable, IAsyncDisposable where T : class
{
    private ObjectPool<T>? _pool;
    private readonly object _lock = new();
    private bool _isDisposed;

    private T _value;

    /// <summary>
    /// Internal constructor for the rented object
    /// </summary>
    /// <param name="pool">The parent pool to which this RentedObject belongs to</param>
    /// <param name="value">The actual value of the object</param>
    internal RentedObject(ObjectPool<T> pool, T value)
    {
        ArgumentNullException.ThrowIfNull(pool);
        ArgumentNullException.ThrowIfNull(value);

        _pool = pool;
        _isDisposed = false;

        _value = value;
    }

    /// <summary>
    /// Gets the value of the rented object.
    /// </summary>
    /// <returns>The actual value of the rented object of type <see cref="T"/></returns>
    /// <exception cref="ObjectDisposedException">Thrown when trying to access a disposed object.</exception>
    /// <remarks>
    /// We explicitly defined this method instead of an actual getter because if we placed <see cref="ObjectDisposedException"/>
    /// on it, multiple calls to <see cref="Dispose"/> or <see cref="DisposeAsync"/> would throw exceptions instead of it being
    /// idempotent.
    /// </remarks>
    public T Unwrap()
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);
        return _value;
    }

    #region IDisposable and IAsyncDisposable Implementation

    /// <summary>
    /// Helper class that acts accordingly if the parent object pool is already disposed by unsetting <see cref="_pool"/> value
    /// of this instance and using the copy of it on the caller for the actual returning of this instance to the pool.
    /// </summary>
    /// <returns>The parent object pool of this instance.</returns>
    /// <remarks>
    /// The <see cref="Lock"/> exists to protect this object's fields (_isDisposed and _isPool).
    /// More importantly, decide and mutate under the lock, then act afterward. This same shape works
    /// for things like raising events (copy the handler under the lock, invoke it outside) or processing
    /// a queue (dequeue under the lock, process outside).
    /// </remarks>
    private ObjectPool<T>? TryTakePool()
    {
        lock (_lock)
        {
            if (_isDisposed)
            {
                return null;
            }

            _isDisposed = true;

            ObjectPool<T>? targetPool = _pool;
            _pool = null;

            return targetPool;
        }
    }

    /// <summary>
    /// Returns the rented object to its parent pool.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This should be callable multiple times without throwing an exception.
    /// Refer to <see href="https://stackoverflow.com/questions/8923853/should-idisposable-dispose-implementations-be-idempotent"/>
    /// </para>
    /// <para>
    /// We need to make a copy of the <see cref="T"/> object before calling <see cref="TryTakePool"/> because it will mark the pool
    /// as disposed and if we try access the object, it will throw <see cref="ObjectDisposedException"/>.
    /// </para>
    /// </remarks>
    public void Dispose()
    {
        T valueCopy = _value;

        // The Return() method is foreign code to this class, so run it after the lock is released.
        // Deadlock risk as well since there's also a lock inside the Return() method.
        TryTakePool()?.Return(valueCopy);
    }

    /// <summary>
    /// Returns the rented object to its parent pool asynchronously.
    /// </summary>
    /// <returns>
    /// <para>An awaitable result of an asynchronous operation.</para>
    /// <para>
    /// A method may return an instance of this value type when it's likely that the result of this operation will be available
    /// synchronously, and when it's expected to be invoked so frequently that the cost of allocating a new <see cref="Task{TResult}"/>
    /// for each call prohibitive.
    /// </para>
    /// </returns>
    /// <inheritdoc cref="Dispose"/>
    public ValueTask DisposeAsync()
    {
        T valueCopy = _value;

        TryTakePool()?.Return(valueCopy);
        return ValueTask.CompletedTask;
    }

    #endregion
}
