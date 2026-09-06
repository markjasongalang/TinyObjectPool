using TinyObjectPool;

public class Program
{
    public static async Task Main(string[] args)
    {
        var pool = new ObjectPool<MyObject>(
            factory: () => new MyObject(),
            maxSize: 1);

        // TODO: Check if thread-safe
        // The previous one is fire-and-forget approach that's why the task is not holding onto the object.

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
}

public class MyObject
{
    // Empty for now
}