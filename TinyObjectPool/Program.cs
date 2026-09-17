using TinyObjectPool;

public class Program
{
    public static async Task Main(string[] args)
    {
        try
        {
            using var pool = new ObjectPool<MyObject>(
                factory: () => new MyObject(),
                maxSize: 0);

        
            using RentedObject<MyObject> obj = pool.Rent();
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error: {e}");
        }
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