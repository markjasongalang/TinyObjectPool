using Microsoft.Extensions.Time.Testing;
using TinyObjectPool.Tests.Models;

namespace TinyObjectPool.Tests;

[TestClass]
public sealed class EvictionTests
{
    private object _lock = null!;

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

        using var pool = new ObjectPool<MyClassWithReset>(
            factory: () => new MyClassWithReset(),
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
                await using (RentedObject<MyClassWithReset> obj = pool.Rent())
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
                await using (RentedObject<MyClassWithReset> obj = pool.Rent())
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
                await using (RentedObject<MyClassWithReset> obj = pool.Rent())
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
}
