using TinyObjectPool.Tests.Models;

namespace TinyObjectPool.Tests;

[TestClass]
public sealed class RentTests
{
    [TestMethod]
    public async Task Rent_MultipleTasksRentSimultaneously_BlockWhileWaiting()
    {
        var pool = new ObjectPool<SampleClassWithReset>(
            factory: () => new SampleClassWithReset(),
            maxSize: 1); // Set to 1 for testing below

        Task task1 = Task.Run(async () =>
        {
            // Add explicit scope so the object would be returned before the assertion below
            using (RentedObject<SampleClassWithReset> obj1 = pool.Rent())
            {
                Console.WriteLine($"[{DateTime.UtcNow:HH:mm:ss}] Task 1: Rented object");

                await Task.Delay(2000);

                Console.WriteLine($"[{DateTime.UtcNow:HH:mm:ss}] Task 1: Returning object");
            }

            Assert.AreEqual(1, pool.Count);
        });

        await Task.Delay(100); // Ensures task 1 gets the single object from the pool first

        Task task2 = Task.Run(async () =>
        {
            Console.WriteLine($"[{DateTime.UtcNow:HH:mm:ss}] Task 2: Attempting to Rent...");

            Assert.AreEqual(0, pool.Count);

            // This should block/wait until task 1 above returns the object that it rented so this 
            // task could rent it after
            using RentedObject<SampleClassWithReset> obj2 = pool.Rent();

            Console.WriteLine($"[{DateTime.UtcNow:HH:mm:ss}] Task 2: Successfully Rented object!");
        });

        await Task.WhenAll(task1, task2);
    }

    [TestMethod]
    public async Task Rent_NotReturnRentedObject_ThrowTimeoutException()
    {
        var pool = new ObjectPool<SampleClassWithReset>(
            factory: () => new SampleClassWithReset(),
            maxSize: 1);

        // Object 1
        Console.WriteLine($"[{DateTime.UtcNow:HH:mm:ss}] Object 1 trying to rent");

        RentedObject<SampleClassWithReset> obj1 = pool.Rent(); // Intentionally doesn't include 'using'

        Console.WriteLine($"[{DateTime.UtcNow:HH:mm:ss}] Object 1 successfully rented");

        Assert.AreEqual(0, pool.Count);

        // Object 2
        Console.WriteLine($"[{DateTime.UtcNow:HH:mm:ss}] Object 2 trying to rent");

        // Asserts that the block inside throws and exception of type TimeoutException
        // and throws AssertFailedException if code does not throw exception or throws
        // exception of type other than TimeoutException.
        // Refer to https://learn.microsoft.com/en-us/dotnet/api/microsoft.visualstudio.testtools.unittesting.assert.throws?view=mstest-net-4.4
        Assert.Throws<TimeoutException>(() =>
        {
            // An exception should be thrown here because the object above wasn't returned
            using RentedObject<SampleClassWithReset> obj2 = pool.Rent();
        });

        Console.WriteLine($"[{DateTime.UtcNow:HH:mm:ss}] Object 2 successfully rented");
    }

    [TestMethod]
    public async Task Rent_RentOnDisposedPool_ThrowObjectDisposedException()
    {
        using var pool = new ObjectPool<SampleClassWithReset>(
            factory: () => new SampleClassWithReset(),
            maxSize: 10);

        pool.Dispose();

        Assert.Throws<ObjectDisposedException>(() =>
        {
            using RentedObject<SampleClassWithReset> obj = pool.Rent();
        });
    }

    [TestMethod]
    public void Rent_InstantiateObjectWithUsingStatement_AutomaticallyReturnsToPool()
    {
        using var pool = new ObjectPool<SampleClassWithReset>(
            factory: () => new SampleClassWithReset(),
            maxSize: 1);

        // Use known, hard-coded, pre-calculated values instead
        // Refer to https://dev.to/canro91/4-common-mistakes-when-writing-your-first-unit-tests-44e9
        Assert.AreEqual(0, pool.Count);

        using (RentedObject<SampleClassWithReset> myObj = pool.Rent())
        {
            // Some actual operations here
        }

        Assert.AreEqual(1, pool.Count);
    }

    [TestMethod]
    public void Rent_InstantiateNullableRentedObject_ReturnsNotNullObject()
    {
        using var pool = new ObjectPool<SampleClassWithReset>(
            factory: () => new SampleClassWithReset(),
            maxSize: 10);

        Assert.AreEqual(0, pool.Count);

        using RentedObject<SampleClassWithReset>? obj = pool.Rent();

        Assert.AreEqual(0, pool.Count);
        Assert.IsNotNull(obj);
    }

    [TestMethod]
    public void Rent_AsyncObjectRent_ReturnsNotNullObject()
    {
        using var pool = new ObjectPool<SampleClassWithReset>(
            factory: () => new SampleClassWithReset(),
            maxSize: 10);

        using RentedObject<SampleClassWithReset>? obj = pool.Rent();

        Assert.IsNotNull(obj);
    }
}
