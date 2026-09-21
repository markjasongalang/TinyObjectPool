using TinyObjectPool;

public class Program
{
    public static async Task Main(string[] args)
    {
        using var pool = new ObjectPool<MyClassWithReset>(
            factory: () => new MyClassWithReset(),
            maxSize: 10);

        RentedObject<MyClassWithReset> obj = pool.Rent();

        obj.Dispose();
        obj.Dispose();
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
