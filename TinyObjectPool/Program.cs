using TinyObjectPool;

public class Program
{
    public static async Task Main(string[] args)
    {
        using var pool = new ObjectPool<MyObject>(
            factory: () => new MyObject(),
            maxSize: 10);

        // TODO: Benchmarks
        // Basis: https://gist.github.com/admir-live/db304653649bd55f5f4eebba3d29d537

        // In our scenario, we'll have the:
        // private ObjectPool<T> _pool;

        // So, basically, our benchmark methods would be:
        // Benchmark1 - without object pool
        // Benchmark2 - with our ObjectPool<T>

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