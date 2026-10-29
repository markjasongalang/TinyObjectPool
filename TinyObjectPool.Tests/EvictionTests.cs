using Microsoft.Extensions.Time.Testing;
using TinyObjectPool.Tests.Models;

namespace TinyObjectPool.Tests;

[TestClass]
public sealed class EvictionTests
{
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
            Console.WriteLine($"[{fakeTime.GetLocalNow()}] Rented an object");
            
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
        var fakeTime = new FakeTimeProvider(startDateTime: DateTimeOffset.Now);

        using var pool = new ObjectPool<SampleClassWithReset>(
            factory: () => new SampleClassWithReset(),
            enableEviction: true,
            timeProvider: fakeTime,
            maxSize: 10);

        // Multiple idle objects to be cleaned (each have different delays)
        var rentTasks = new Task[]
        {
            Task.Run(async() =>
            {
                await using (RentedObject<SampleClassWithReset> obj = pool.Rent())
                {
                    obj.Unwrap().Name = "Object 1";

                    Task delay = Task.Delay(TimeSpan.FromSeconds(2), fakeTime);
                    fakeTime.Advance(TimeSpan.FromSeconds(2));
                    await Task.WhenAny(delay);
                }
            }),
            Task.Run(async() =>
            {
                await using (RentedObject<SampleClassWithReset> obj = pool.Rent())
                {
                    obj.Unwrap().Name = "Object 2";

                    Task delay = Task.Delay(TimeSpan.FromSeconds(10), fakeTime);
                    fakeTime.Advance(TimeSpan.FromSeconds(10));
                    await Task.WhenAny(delay);
                }
            }),
            Task.Run(async() =>
            {
                await using (RentedObject<SampleClassWithReset> obj = pool.Rent())
                {
                    obj.Unwrap().Name = "Object 3";

                    Task delay = Task.Delay(TimeSpan.FromSeconds(20), fakeTime);
                    fakeTime.Advance(TimeSpan.FromSeconds(20));
                    await Task.WhenAny(delay);
                }
            })
        };

        await Task.WhenAll(rentTasks);

        Assert.AreEqual(3, pool.Count);

        fakeTime.Advance(TimeSpan.FromMinutes(5));

        Assert.AreEqual(0, pool.Count);
    }
}
