using TinyObjectPool;

public class Program
{
    public static async Task Main(string[] args)
    {
        using var pool = new ObjectPool<MyObject>(
            factory: () => new MyObject(),
            maxSize: 10);

        // TODO: Unit tests (MSTest - Microsoft Testing Platform)
        // - Test return if an object was rented first and then the pool was disposed
        // - Test pool dispose and then rent an object


        // TODO: Benchmarks

    }

    public static async Task TestRentOnDisposedPool()
    {
        using var pool = new ObjectPool<MyObject>(
            factory: () => new MyObject(),
            maxSize: 10);

        // Test if someone tries to call Rent() on a disposed pool
        pool.Dispose();
        using RentedObject<MyObject> obj = pool.Rent(); // Should throw an exception
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