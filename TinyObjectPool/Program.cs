using TinyObjectPool;

public class Program
{
    public static void Main(string[] args)
    {
        var pool = new ObjectPool<MyObject>(
            factory: () => new MyObject(),
            maxSize: 5);

        MyObject a = pool.Rent();
        Console.WriteLine(pool.Count);

        MyObject b = pool.Rent();
        Console.WriteLine(pool.Count);

        pool.Return(a);
        Console.WriteLine(pool.Count);

        MyObject c = pool.Rent();

        // Reference identity check (same object in memory)
        bool isSameInstance = ReferenceEquals(a, c);
        Console.WriteLine($"Is exact same object? {isSameInstance}");

        MyObject d = pool.Rent();
        MyObject e = pool.Rent();
        MyObject f = pool.Rent();
        Console.WriteLine($"Rented objects at this point: {pool.CreatedObjectCount}");

        pool.Return(d);
        MyObject g = pool.Rent(); // This will wait indefinitely until an object is returned
        Console.WriteLine($"Rented objects at this point: {pool.CreatedObjectCount}");
    }
}

public class MyObject
{
    // Empty for now
}