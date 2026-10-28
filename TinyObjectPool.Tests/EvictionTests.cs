using Microsoft.Extensions.Time.Testing;

namespace TinyObjectPool.Tests;

[TestClass]
public sealed class EvictionTests
{
    [TestMethod]
    public async Task Evict_ReturnedObjectIdleInPool_ZeroPoolCount()
    {
        var fakeTime = new FakeTimeProvider(startDateTime: DateTimeOffset.Now);
        
        using var pool = new ObjectPool<MyClassWithReset>(
            factory: () => new MyClassWithReset(),
            enableEviction: true,
            timeProvider: fakeTime, // Deterministic time-based testing
            maxSize: 10);
        
        await using (RentedObject<MyClassWithReset> obj = pool.Rent())
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
}
