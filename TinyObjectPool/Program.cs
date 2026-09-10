using TinyObjectPool;

public class Program
{
    public static async Task Main(string[] args)
    {
        //await TestIfThreadSafe();
        //await TestObjectReset();
        await TestSemaphoreTimeout();

        //using var pool = new ObjectPool<MyObject>(
        //    factory: () => new MyObject(),
        //    maxSize: 10);

        //using (RentedObject<MyObject> obj = pool.Rent())
        //{
        //    Console.WriteLine(pool.Count);

        //    // What if we dispose the pool here? 
        //    // Just before returning the rented object

        //    // Once we dispose the object, it will call the Return(T) but the pool is already destroyed
        //    // But there's a condition inside the Dispose() which checks if the pool is null (the idempotent one)
        //}

        
    }

    public static async Task TestIfThreadSafe()
    {
        var pool = new ObjectPool<MyObject>(
            factory: () => new MyObject(),
            maxSize: 1); // Set to 1 for testing below

        Task task1 = Task.Run(async () =>
        {
            using RentedObject<MyObject> obj1 = pool.Rent();
            Console.WriteLine($"[{DateTime.UtcNow:HH:mm:ss}] Task 1: Rented object");

            await Task.Delay(2000);

            Console.WriteLine($"[{DateTime.UtcNow:HH:mm:ss}] Task 1: Returning object");
        });

        await Task.Delay(100);

        Task task2 = Task.Run(async () =>
        {
            Console.WriteLine($"[{DateTime.UtcNow:HH:mm:ss}] Task 2: Attempting to Rent...");

            // This should block
            using RentedObject<MyObject> obj2 = pool.Rent();

            Console.WriteLine($"[{DateTime.UtcNow:HH:mm:ss}] Task 2: Successfully Rented object!");
        });

        await Task.WhenAll(task1, task2); // Run tasks simultaneously
    }

    public static async Task TestObjectReset()
    {
        var pool = new ObjectPool<MyObject>(
            factory: () => new MyObject(),
            maxSize: 10);

        using RentedObject<MyObject> obj = pool.Rent();
        obj.Value.Name = "Jason";

        RentedObject<MyObject> obj2 = pool.Rent();
        Console.WriteLine($"{nameof(obj2)}.name = {obj2.Value.Name}"); // Should be blank
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