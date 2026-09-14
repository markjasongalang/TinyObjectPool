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

    public static async Task TestSemaphoreTimeout()
    {
        var pool = new ObjectPool<MyObject>(
            factory: () => new MyObject(),
            maxSize: 1);

        // Object 1
        Console.WriteLine($"[{DateTime.UtcNow:HH:mm:ss}] Object 1 trying to rent");

        RentedObject <MyObject> obj1 = pool.Rent(); // Intentionally doesn't include 'using'

        Console.WriteLine($"[{DateTime.UtcNow:HH:mm:ss}] Object 1 successfully rented");

        // Object 2
        Console.WriteLine($"[{DateTime.UtcNow:HH:mm:ss}] Object 2 trying to rent");

        // An exception should be thrown here because the object above wasn't returned
        using RentedObject<MyObject> obj2 = pool.Rent();

        Console.WriteLine($"[{DateTime.UtcNow:HH:mm:ss}] Object 2 successfully rented");
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