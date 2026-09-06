using TinyObjectPool;

public class Program
{
    public static void Main(string[] args)
    {
        var pool = new ObjectPool<MyObject>();

        MyObject? a = pool.Rent();
        Console.WriteLine(pool.Count);

        MyObject? b = pool.Rent();
        Console.WriteLine(pool.Count);

        pool.Return(a);
        Console.WriteLine(pool.Count);

        MyObject? c = pool.Rent();

        // Reference identity check (same object in memory)
        bool isSameInstance = ReferenceEquals(a, c);
        Console.WriteLine($"Is exact same object? {isSameInstance}");
    }
}

public class MyObject
{
    // Empty for now
}