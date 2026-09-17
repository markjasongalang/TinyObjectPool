// -----------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
// -----------------------------------------------------------------------

using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Microsoft.CodeAnalysis;

namespace TinyObjectPool.Benchmarks
{
    /// <summary>
    /// Contains benchmarks to check if TinyObjectPool helps make programs efficient.
    /// </summary>
    /// <remarks>
    /// For more information on the VS BenchmarkDotNet Diagnosers see https://learn.microsoft.com/visualstudio/profiling/profiling-with-benchmark-dotnet
    /// 
    /// Note: You need to run this project on Release configuration and Start Without Debugging.
    /// Also, for the benchmarks to be as accurate as possible, you should exit every running application on your system and run the
    /// command from the terminal.
    /// Refer to https://blog.nimblepros.com/blogs/benchmarking-in-dotnet/
    /// </remarks>
    /// <example>
    /// * Summary *
    /// BenchmarkDotNet v0.15.2, Windows 11 (10.0.26200.9457)
    /// Unknown processor
    /// .NET SDK 10.0.401
    /// [Host]     : .NET 10.0.12 (10.0.1226.42308), X64 RyuJIT AVX2
    /// DefaultJob : .NET 10.0.12 (10.0.1226.42308), X64 RyuJIT AVX2
    /// 
    /// | Method                      | N    | Mean          | Error        | StdDev        | Ratio | RatioSD | Gen0    | Allocated | Alloc Ratio |
    /// |---------------------------- |----- |--------------:|-------------:|--------------:|------:|--------:|--------:|----------:|------------:|
    /// | CreateObjectsWithoutPooling | 5    |      28.78 ns |     1.229 ns |      3.605 ns |  1.02 |    0.18 |  0.0143 |     120 B |        1.00 |
    /// | CreateObjectsWithPooling    | 5    |     264.39 ns |     9.071 ns |     26.746 ns |  9.33 |    1.53 |       - |         - |        0.00 |
    /// |                             |      |               |              |               |       |         |         |           |             |
    /// | CreateObjectsWithoutPooling | 50   |     239.14 ns |    13.583 ns |     39.836 ns |  1.03 |    0.27 |  0.1433 |    1200 B |        1.00 |
    /// | CreateObjectsWithPooling    | 50   |   2,584.18 ns |    51.345 ns |    138.815 ns | 11.16 |    2.30 |       - |         - |        0.00 |
    /// |                             |      |               |              |               |       |         |         |           |             |
    /// | CreateObjectsWithoutPooling | 500  |   2,675.55 ns |   126.382 ns |    370.658 ns |  1.02 |    0.20 |  1.4343 |   12000 B |        1.00 |
    /// | CreateObjectsWithPooling    | 500  |  26,207.48 ns |   702.915 ns |  2,072.560 ns |  9.98 |    1.60 |       - |         - |        0.00 |
    /// |                             |      |               |              |               |       |         |         |           |             |
    /// | CreateObjectsWithoutPooling | 1000 |   4,373.35 ns |   410.298 ns |  1,209.772 ns |  1.08 |    0.42 |  2.8687 |   24000 B |        1.00 |
    /// | CreateObjectsWithPooling    | 1000 |  51,236.44 ns | 1,864.197 ns |  5,496.627 ns | 12.61 |    3.64 |       - |         - |        0.00 |
    /// |                             |      |               |              |               |       |         |         |           |             |
    /// | CreateObjectsWithoutPooling | 2000 |  10,360.17 ns |   498.870 ns |  1,447.313 ns |  1.02 |    0.21 |  5.7373 |   48000 B |        1.00 |
    /// | CreateObjectsWithPooling    | 2000 |  95,822.95 ns | 5,261.886 ns | 15,514.792 ns |  9.45 |    2.12 |       - |         - |        0.00 |
    /// |                             |      |               |              |               |       |         |         |           |             |
    /// | CreateObjectsWithoutPooling | 5000 |  25,184.58 ns | 1,241.936 ns |  3,583.269 ns |  1.02 |    0.21 | 14.3433 |  120000 B |        1.00 |
    /// | CreateObjectsWithPooling    | 5000 | 251,455.61 ns | 7,569.867 ns | 22,201.113 ns | 10.19 |    1.73 |       - |         - |        0.00 |
    /// 
    /// * Legends *
    /// N           : Value of the 'N' parameter
    /// Mean        : Arithmetic mean of all measurements
    /// Error       : Half of 99.9 % confidence interval
    /// StdDev      : Standard deviation of all measurements
    /// Median      : Value separating the higher half of all measurements(50th percentile)
    /// Ratio       : Mean of the ratio distribution ([Current]/[Baseline])
    /// RatioSD     : Standard deviation of the ratio distribution ([Current]/[Baseline])
    /// Gen0        : GC Generation 0 collects per 1000 operations
    /// Gen1        : GC Generation 1 collects per 1000 operations
    /// Allocated   : Allocated memory per single operation(managed only, inclusive, 1KB = 1024B)
    /// Alloc Ratio : Allocated memory ratio distribution ([Current]/[Baseline])
    /// 1 ns        : 1 Nanosecond(0.000000001 sec)
    /// 
    /// ***** BenchmarkRunner: End *****
    /// Run time: 00:09:14 (554.16 sec), executed benchmarks: 6
    /// Global total time: 00:09:22 (562.39 sec), executed benchmarks: 6
    /// </example>
    //[CPUUsageDiagnoser]
    [MemoryDiagnoser]
    public class Benchmarks
    {
        private ObjectPool<MyObject> _pool;

        /// <summary>
        /// Tells the benchmark that we want to use this property as a parameter for our
        /// benchmarks.
        /// </summary>
        [Params(5, 50, 500, 1000, 2000, 5000)]
        public int N { get; set; }

        /// <summary>
        /// <see href="https://benchmarkdotnet.org/articles/features/setup-and-cleanup.html">[GlobalSetup]</see> attribute - executed only once per 
        /// a benchmarked method after initialization of benchmark parameters and before all the benchmark method invocations.
        /// </summary>
        [GlobalSetup]
        public void Setup()
        {
            // TODO: Proper dependency injection
            _pool = new ObjectPool<MyObject>(
                factory: () => new MyObject(),
                maxSize: 10);
        }

        /// <summary>
        /// The Baseline parameter adds just 1 or 2 columns in the report that give us basically
        /// a percentage difference between our baseline and the other test(s). It is displayed
        /// in the 'Ratio' column.
        /// Refer to https://benchmarkdotnet.org/articles/features/baselines.html
        /// </summary>
        /// <remarks>
        /// Based on the documentation, it's good practice to avoid dead code elimination by using the result
        /// of the calculation, that's why <see cref="Task"/> is returned.
        /// </remarks>
        [Benchmark(Baseline = true)]
        public Task CreateObjectsWithoutPooling()
        {
            for (var i = 0; i < N; i++)
            {
                var myObject = new MyObject
                {
                    Name = "Sample"
                };
            }

            return Task.CompletedTask;
        }

        // TODO: Try later with multiple threads

        [Benchmark]
        public Task CreateObjectsWithPooling()
        {
            for (var i = 0; i < N; i++)
            {
                using RentedObject<MyObject> myRentedObject = _pool.Rent();
                myRentedObject.Value.Name = "Sample";
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// <see href="https://benchmarkdotnet.org/articles/features/setup-and-cleanup.html">[GlobalCleanup]</see> attribute - executed only once per
        /// a benchmarked method after all the benchmark method invocations.
        /// </summary>
        [GlobalCleanup]
        public void GlobalCleanup()
        {
            // Disposing logic
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
}
