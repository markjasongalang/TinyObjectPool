using TinyObjectPool;

public class Program
{
    public static void Main(string[] args)
    {
        var pool = new ObjectPool<MyObject>(5);

        MyObject? a = pool.Rent();
        MyObject? b = pool.Rent();

        pool.Return(a);

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