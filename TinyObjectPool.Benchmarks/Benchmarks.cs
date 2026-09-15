// -----------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
// -----------------------------------------------------------------------

using System.Linq;
using System.Text;
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
    /// | Method        | N   | Mean        | Error      | StdDev     | Median      | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
    /// |-------------- |---- |------------:|-----------:|-----------:|------------:|------:|--------:|-------:|-------:|----------:|------------:|
    /// | StringJoin    | 5   |    73.36 ns |   1.315 ns |   1.165 ns |    72.93 ns |  1.00 |    0.02 | 0.0162 |      - |     136 B |        1.00 |
    /// | StringBuilder | 5   |    59.72 ns |   3.559 ns |  10.494 ns |    62.05 ns |  0.81 |    0.14 | 0.0191 |      - |     160 B |        1.18 |
    /// |               |     |             |            |            |             |       |         |        |        |           |             |
    /// | StringJoin    | 50  |   567.21 ns |  22.636 ns |  66.742 ns |   593.29 ns |  1.02 |    0.18 | 0.0582 |      - |     488 B |        1.00 |
    /// | StringBuilder | 50  |   525.39 ns |  16.689 ns |  47.885 ns |   530.99 ns |  0.94 |    0.16 | 0.1526 |      - |    1280 B |        2.62 |
    /// |               |     |             |            |            |             |       |         |        |        |           |             |
    /// | StringJoin    | 500 | 8,291.79 ns | 197.048 ns | 571.672 ns | 8,319.87 ns |  1.01 |    0.10 | 1.3428 |      - |   11288 B |        1.00 |
    /// | StringBuilder | 500 | 5,650.97 ns | 208.777 ns | 615.584 ns | 5,658.66 ns |  0.69 |    0.09 | 1.6251 | 0.0458 |   13648 B |        1.21 |
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
        /// <summary>
        /// Tells the benchmark that we want to use this property as a parameter for our
        /// benchmarks.
        /// </summary>
        [Params(5, 50, 500)]
        public int N { get; set; }

        /// <summary>
        /// The Baseline parameter adds just 1 or 2 columns in the report that give us basically
        /// a percentage difference between our baseline and the other test(s). It is displayed
        /// in the 'Ratio' column.
        /// Refer to https://benchmarkdotnet.org/articles/features/baselines.html
        /// </summary>
        [Benchmark(Baseline = true)]
        public string StringJoin()
        {
            return string.Join(", ", Enumerable.Range(0, N).Select(i => i.ToString()));
        }

        [Benchmark]
        public string StringBuilder()
        {
            var sb = new StringBuilder();
            for (var i = 0; i < N; i++)
            {
                sb.Append(i);
                sb.Append(", ");
            }

            return sb.ToString();
        }
    }
}
