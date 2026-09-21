using TinyObjectPool;

public class Program
{
    public static async Task Main(string[] args)
    {
        using var pool = new ObjectPool<MyClassNoReset>(
            factory: () => new MyClassNoReset(),
            //reset: delegate { Console.WriteLine("Reset mechanism here..."); },
            maxSize: 10);

        // TODO: Add 3 unit tests:
        // - Throw exception
        // - Success by including action delegate in pool instantiation
        // - Success by implementing IResettable
    }
}

public class MyObject : IResettable
{
    public string Name { get; set; }

    /// <summary>
    /// Prevent subtle data leaks or unexpected state bugs
    /// </summary>
    public void Reset()
    {
        Name = string.Empty;
    }
}

/// <summary>
/// This class doesn't implement IResettable for testing.
/// </summary>
public class MyClassNoReset
{
    public int Id { get; set; }
}
