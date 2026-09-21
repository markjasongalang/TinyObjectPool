namespace TinyObjectPool.Tests;

[TestClass]
public class DisposeTests
{
    [TestMethod]
    public void Dispose_RentedObjectNotReturned_PoolDisposesObjects()
    {
        using var pool = new ObjectPool<MyClassWithReset>(
            factory: () => new MyClassWithReset(),
            maxSize: 10);

        // Explicitly didn't include 'using' keyword below so the object will not be returned
        RentedObject<MyClassWithReset> obj = pool.Rent();

        pool.Dispose();

        Assert.AreEqual(0, pool.Count);
        Assert.IsTrue(string.IsNullOrEmpty(obj.Value.Name));
    }

    [TestMethod]
    public void Dispose_RentObjectOnDisposedPool_ThrowsObjectDisposedException()
    {
        using var pool = new ObjectPool<MyClassWithReset>(
            factory: () => new MyClassWithReset(),
            maxSize: 10);

        pool.Dispose();

        Assert.Throws<ObjectDisposedException>(() =>
        {
            using RentedObject<MyClassWithReset> obj = pool.Rent();
        });
    }

    [TestMethod]
    public void Dispose_AlreadyDisposedObject_Idempotent()
    {
        using var pool = new ObjectPool<MyClassWithReset>(
            factory: () => new MyClassWithReset(),
            maxSize: 10);

        RentedObject<MyClassWithReset> obj = pool.Rent();

        obj.Dispose();

        // In the second Dispose() below, we expect no exceptions to be thrown.
        // So, this unit test will fail if an exception is thrown and we don't need to
        // add a special assert (though, in NUnit, there's a method called
        // Assert.DoesNotThrow for this scenario)
        obj.Dispose();

        Assert.AreEqual(1, pool.Count);
    }

    [TestMethod]
    public void Dispose_AccessDisposedObject_ThrowsException()
    {
        using var pool = new ObjectPool<MyClassWithReset>(
            factory: () => new MyClassWithReset(),
            maxSize: 10);

        RentedObject<MyClassWithReset> rentedObject = pool.Rent();

        rentedObject.Dispose();

        Assert.Throws<ObjectDisposedException>(() =>
        {
            MyClassWithReset obj = rentedObject.Value;
        });
    }
}
