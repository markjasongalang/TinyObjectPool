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

        using var pool = new ObjectPool<MyClassWithReset>(
            factory: () => new MyClassWithReset(),
            enableEviction: true,
            maxSize: 10);

        var rentTasks = new Task[]
        {
            Task.Run(async() =>
            {
                await using (RentedObject<MyClassWithReset> obj = pool.Rent())
                {
                    Console.WriteLine($"[{DateTime.Now}] Rented an object");
                    await Task.Delay(TimeSpan.FromSeconds(2));
                    Console.WriteLine("here1");

                    obj.Unwrap().Name = "Object 1";
                } // Dispose here

                Console.WriteLine($"[{DateTime.Now}] Returned the object");
                Console.WriteLine($"[{DateTime.Now}] Pool object count: {pool.Count}");
            }),
            Task.Run(async() =>
            {
                await using (RentedObject<MyClassWithReset> obj = pool.Rent())
                {
                    Console.WriteLine($"[{DateTime.Now}] Rented an object");
                    await Task.Delay(TimeSpan.FromSeconds(12));
                    Console.WriteLine("here2");

                    obj.Unwrap().Name = "Object 2";
                } // Dispose here

                Console.WriteLine($"[{DateTime.Now}] Returned the object");
                Console.WriteLine($"[{DateTime.Now}] Pool object count: {pool.Count}");
            })
        };

        await Task.WhenAll(rentTasks);

        // Intentional delay so we can observe the background eviction
        await Task.Delay(TimeSpan.FromMinutes(15));
        Console.WriteLine("Program exiting...");
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
