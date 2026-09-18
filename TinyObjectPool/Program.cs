using TinyObjectPool;

public class Program
{
    public static async Task Main(string[] args)
    {
        using var pool = new ObjectPool<MyObject>(
            factory: () => new MyObject(),
            maxSize: 10);

        RentedObject<MyObject> obj = pool.Rent();

        obj.Dispose();

        Console.WriteLine(obj.Value.Name); // FIXME: Object should not be accessed anymore

        obj.Dispose();

        Console.WriteLine(pool.Count); // 1
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