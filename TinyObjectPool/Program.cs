using TinyObjectPool;

public class Program
{
    public static async Task Main(string[] args)
    {
#if DEBUG
        // (Pre-processor command)
        // This only compiles in debug mode.
        Console.Clear();
#endif


    }
}

public class MyClassWithReset : IResettable
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
