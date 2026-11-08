using Microsoft.Extensions.Time.Testing;
using TinyObjectPool.Tests.Models;

namespace TinyObjectPool.Tests;

[TestClass]
public sealed class EvictionTests
{
    /// <remarks>
    /// <para>
    /// As a constructor parameter of a test class (starting with v3.6). This way is recommended over using the property,
    /// because it gives access to the object in the constructor.
    /// </para>
    /// <para>
    /// Refer to https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-mstest-writing-tests-testcontext
    /// </para>
    /// </remarks>
    private readonly TestContext _testContext;

    private object _lock = null!;

    public EvictionTests(TestContext testContext)
    {
        _testContext = testContext;
    }

    /// <summary>
    /// <para>
    /// Test-level lifecycle initialization method that runs before each test method in the class.
    /// </para>
    /// <para>
    /// https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-mstest-writing-tests-lifecycle#test-level-lifecycle
    /// </para>
    /// </summary>
    [TestInitialize]
    public void EvictionTestsInit()
    {
        _lock = new();
    }

    [TestMethod]
    public async Task Evict_ReturnedObjectIdleInPool_ZeroPoolCount()
    {
        var fakeTime = new FakeTimeProvider(startDateTime: DateTimeOffset.Now);
        
        using var pool = new ObjectPool<SampleClassWithReset>(
            factory: () => new SampleClassWithReset(),
            enableEviction: true,
            timeProvider: fakeTime, // Deterministic time-based testing
            maxSize: 10);
        
        await using (RentedObject<SampleClassWithReset> obj = pool.Rent())
        {
            obj.Unwrap().Name = "Object 1";
            
            Task delay = Task.Delay(TimeSpan.FromSeconds(2), fakeTime);
            fakeTime.Advance(TimeSpan.FromSeconds(2));
            await Task.WhenAny(delay);
        }
        // --- The idle time will start to be measured at this point ---

        Assert.AreEqual(1, pool.Count);
        
        fakeTime.Advance(TimeSpan.FromMinutes(5));

        Assert.AreEqual(0, pool.Count);
    }

    [TestMethod]
    public async Task Evict_MultipleIdleObjects_ZeroPoolCount()
    {
        var fakeTime = new FakeTimeProvider();
        var startDateTime = new DateTimeOffset(2026, 11, 01, 22, 01, 0, TimeSpan.Zero);
        fakeTime.SetUtcNow(startDateTime);

        using var pool = new ObjectPool<SampleClassWithReset>(
            factory: () => new SampleClassWithReset(),
            enableEviction: true,
            timeProvider: fakeTime,
            maxSize: 10);

        // Signal before we advance the time
        var startedCount = 0;
        var allStarted = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);

        // Note: Don't forget that Task.Run(...) is simply a function/method call, so the moment we used it below, we called it already.
        // The tasks will start executing immediately and simultaneously.
        var rentTasks = new Task[]
        {
            Task.Run(async () =>
            {
                await using (RentedObject<SampleClassWithReset> obj = pool.Rent())
                {
                    obj.Unwrap().Name = "Object 1";

                    lock (_lock)
                    {
                        startedCount++;
                        if (startedCount >= 3)
                        {
                            allStarted.SetResult();
                        }
                    }

                    await Task.Delay(TimeSpan.FromSeconds(2), fakeTime);
                } // Returned here
            }),
            Task.Run(async () =>
            {
                await using (RentedObject<SampleClassWithReset> obj = pool.Rent())
                {
                    obj.Unwrap().Name = "Object 2";

                    lock (_lock)
                    {
                        startedCount++;
                        if (startedCount >= 3)
                        {
                            allStarted.SetResult();
                        }
                    }

                    await Task.Delay(TimeSpan.FromSeconds(2), fakeTime);
                }
            }),
            Task.Run(async () =>
            {
                await using (RentedObject<SampleClassWithReset> obj = pool.Rent())
                {
                    obj.Unwrap().Name = "Object 3";

                    lock (_lock)
                    {
                        startedCount++;
                        if (startedCount >= 3)
                        {
                            allStarted.SetResult();
                        }
                    }

                    await Task.Delay(TimeSpan.FromSeconds(2), fakeTime);
                }
            })
        };

        // Synchronization: coordinating the actions of threads for a predictable outcome.
        // Refer to https://www.albahari.com/threading/part2.aspx#_Synchronization
        // 
        // In tests involving virtual time and asynchronous operations, synchronize the asynchronous operations
        // before advancing the virtual time.
        await allStarted.Task;
        fakeTime.Advance(TimeSpan.FromSeconds(2));

        await Task.WhenAll(rentTasks);
        Assert.AreEqual(3, pool.Count);

        fakeTime.Advance(TimeSpan.FromMinutes(5));
        Assert.AreEqual(0, pool.Count);
    }

    [TestMethod]
    public async Task Evict_TwoIdleObjectsAndOneActiveObject_OnePoolCount()
    {
        var fakeTime = new FakeTimeProvider();
        var startDateTime = new DateTimeOffset(
            year: 2026, 
            month: 11, 
            day: 03, 
            hour: 11, 
            minute: 09, 
            second: 10, 
            offset: TimeSpan.Zero);

        fakeTime.SetUtcNow(startDateTime);

        using var pool = new ObjectPool<SampleClassWithReset>(
            factory: () => new SampleClassWithReset(),
            enableEviction: true,
            timeProvider: fakeTime,
            maxSize: 10);

        var startedCount = 0;

        var twoTasksStarted = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);

        var thirdTaskStarted = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);

        Task task1 = Task.Run(async () =>
        {
            await using (RentedObject<SampleClassWithReset> obj1 = pool.Rent())
            {
                obj1.Unwrap().Name = "Object 1";
                
                lock (_lock)
                {
                    startedCount++;
                    if (startedCount >= 2)
                    {
                        twoTasksStarted.SetResult();
                    }
                }

                await Task.Delay(TimeSpan.FromSeconds(10), fakeTime, _testContext.CancellationToken);
            }
        }, _testContext.CancellationToken);

        Task task2 = Task.Run(async () =>
        {
            await using (RentedObject<SampleClassWithReset> obj2 = pool.Rent())
            {
                obj2.Unwrap().Name = "Object 2";
                
                lock (_lock)
                {
                    startedCount++;
                    if (startedCount >= 2)
                    {
                        twoTasksStarted.SetResult();
                    }
                }

                await Task.Delay(TimeSpan.FromSeconds(3), fakeTime, _testContext.CancellationToken);
            }
        }, _testContext.CancellationToken);

        Task task3 = Task.Run(async () =>
        {
            await using (RentedObject<SampleClassWithReset> obj3 = pool.Rent())
            {
                obj3.Unwrap().Name = "Object 3";

                thirdTaskStarted.SetResult();

                await Task.Delay(TimeSpan.FromMinutes(6), fakeTime, _testContext.CancellationToken);
            }
        }, _testContext.CancellationToken);

        await twoTasksStarted.Task;
        fakeTime.Advance(TimeSpan.FromSeconds(10));

        await Task.WhenAll(task1, task2);
        Assert.AreEqual(2, pool.Count);

        fakeTime.Advance(TimeSpan.FromMinutes(6));
        Assert.AreEqual(1, pool.Count);
    }

    [TestMethod]
    public async Task Evict_MultipleRentedObjectsPastFirstEvictionCheckThenReturned_FullPool()
    {
        var fakeTime = new FakeTimeProvider();
        var startDateTime = new DateTimeOffset(
            year: 2026,
            month: 11,
            day: 08,
            hour: 09,
            minute: 01,
            second: 12,
            offset: TimeSpan.Zero);

        fakeTime.SetUtcNow(startDateTime);

        using var pool = new ObjectPool<SampleClassWithReset>(
            factory: () => new SampleClassWithReset(),
            enableEviction: true,
            timeProvider: fakeTime,
            maxSize: 10);

        var startedCount = 0;
        var allStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        // Rent 10 objects concurrently, each with a 2-second delay before returning to the pool.
        var rentTasks = new Task[10];
        for (var i = 0; i < 10; i++)
        {
            rentTasks[i] = Task.Run(async () =>
            {
                await using (RentedObject<SampleClassWithReset> obj = pool.Rent())
                {
                    obj.Unwrap().Name = $"Object {i + 1}";

                    lock (_lock)
                    {
                        startedCount++;
                        if (startedCount >= 10)
                        {
                            allStarted.SetResult();
                        }
                    }

                    await Task.Delay(TimeSpan.FromMinutes(6), fakeTime, _testContext.CancellationToken);
                }
            }, _testContext.CancellationToken);
        }

        await allStarted.Task;
        fakeTime.Advance(TimeSpan.FromMinutes(6));

        await Task.WhenAll(rentTasks);

        Assert.AreEqual(10, pool.Count);
    }
}
