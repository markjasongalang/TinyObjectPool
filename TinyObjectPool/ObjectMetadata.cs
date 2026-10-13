namespace TinyObjectPool;

public class ObjectMetadata
{
    // Backing fields
    private readonly object _sync = new();
    private DateTime _createdAt = DateTime.UtcNow;
    private DateTime? _lastAccessedAt;
    private DateTime? _lastReturnedAt;

    // Public properties
    public DateTime CreatedAt
    {
        get { lock (_sync) return _createdAt; }
        set { lock (_sync) _createdAt = value; }
    }

    public DateTime? LastAccessedAt
    {
        get { lock (_sync) return _lastAccessedAt; }
        set { lock (_sync) _lastAccessedAt = value; }
    }

    public DateTime? LastReturnedAt
    {
        get { lock (_sync) return _lastReturnedAt; }
        set { lock (_sync) _lastReturnedAt = value; }
    }

    public TimeSpan Age => DateTime.UtcNow - CreatedAt;

    public TimeSpan? GetIdleTime()
    {
        if (LastReturnedAt.HasValue && LastAccessedAt.HasValue && 
            LastReturnedAt.Value > LastAccessedAt.Value)
        {
            return DateTime.UtcNow - LastReturnedAt.Value;
        }

        return null;
    }

    internal void RecordAccess(DateTime timestamp)
    {
        lock (_sync)
        {
            _lastAccessedAt = timestamp;
        }
    }

    internal void RecordReturn(DateTime timestamp)
    {
        lock (_sync)
        {
            _lastReturnedAt = timestamp;
        }
    }
}
