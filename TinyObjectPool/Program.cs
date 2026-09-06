using TinyObjectPool;

public class Program
{
    public static async Task Main(string[] args)
    {
        var pool = new ObjectPool<MyObject>(
            factory: () => new MyObject(),
            maxSize: 10);

        //await TestIfThreadSafe(pool);
        await TestObjectReset(pool);
        
    }

    public static async Task TestIfThreadSafe(ObjectPool<MyObject> pool)
    {
        Task task1 = Task.Run(async () =>
        {
            MyObject obj1 = pool.Rent();
            Console.WriteLine($"[{DateTime.UtcNow:HH:mm:ss}] Task 1: Rented object");

            await Task.Delay(2000);

            Console.WriteLine($"[{DateTime.UtcNow:HH:mm:ss}] Task 1: Returning object");
            pool.Return(obj1);
        });

        await Task.Delay(100);

        Task task2 = Task.Run(async () =>
        {
            Console.WriteLine($"[{DateTime.UtcNow:HH:mm:ss}] Task 2: Attempting to Rent...");

            // This should block
            MyObject obj2 = pool.Rent();

            Console.WriteLine($"[{DateTime.UtcNow:HH:mm:ss}] Task 2: Successfully Rented object!");
            pool.Return(obj2);
        });

        await Task.WhenAll(task1, task2); // Run tasks simultaneously
    }

    public static async Task TestObjectReset(ObjectPool<MyObject> pool)
    {
        MyObject obj = pool.Rent();

        obj.Name = "Jason";
        pool.Return(obj);

        MyObject obj2 = pool.Rent();
        Console.WriteLine($"{nameof(obj2)}.name = {obj.Name}"); // Should be blank
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