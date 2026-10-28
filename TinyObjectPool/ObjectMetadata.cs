namespace TinyObjectPool;

public class ObjectMetadata
{
    private readonly TimeProvider _timeProvider;
    private readonly object _sync = new();

    private DateTimeOffset _createdAt;
    public DateTimeOffset CreatedAt
    {
        get { lock (_sync) return _createdAt; }
    }

    private DateTimeOffset? _lastAccessedAt;
    public DateTimeOffset? LastAccessedAt
    {
        get { lock (_sync) return _lastAccessedAt; }
    }

    private DateTimeOffset? _lastReturnedAt;
    public DateTimeOffset? LastReturnedAt
    {
        get { lock (_sync) return _lastReturnedAt; }
    }

    public ObjectMetadata(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
        _createdAt = _timeProvider.GetLocalNow();
    }

    /// <summary>
    /// Gets the idle time of the rented object that was returned to the pool.
    /// </summary>
    /// <returns>The idle time of the object.</returns>
    /// <remarks>
    /// If <see cref="LastReturnedAt"/> is greater than or equal to <see cref="LastAccessedAt"/>, then
    /// it means the object was rented and then returned to the pool afterwards, so this is what we
    /// want to refer as the idle time.
    /// </remarks>
    public TimeSpan? GetIdleTime()
    {
        if (LastReturnedAt.HasValue && LastAccessedAt.HasValue && 
            LastReturnedAt.Value > LastAccessedAt.Value)
        {
            return _timeProvider.GetLocalNow() - LastReturnedAt.Value;
        }

        return null;
    }

    internal void RecordAccess(DateTimeOffset timestamp)
    {
        lock (_sync)
        {
            _lastAccessedAt = timestamp;
        }
    }

    internal void RecordReturn(DateTimeOffset timestamp)
    {
        lock (_sync)
        {
            _lastReturnedAt = timestamp;
        }
    }
}
