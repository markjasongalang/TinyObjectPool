using TinyObjectPool.Tests.Models;

namespace TinyObjectPool.Tests;

[TestClass]
public sealed class BasicTests
{
    /// <remarks>
    /// Test names - naming convention
    /// UnitOfWork_Scenario_ExpectedResult
    /// Refer to https://canro91.github.io/2021/04/12/UnitTestNamingConventions/
    /// 
    /// The AAA (Arrange, Act, Assert) pattern is a common way of writing unit tests for a
    /// method test.
    /// Refer to https://learn.microsoft.com/en-us/visualstudio/test/unit-test-basics?view=visualstudio
    /// </remarks>
    [TestMethod]
    public void Reset_ObjectReturnedInPoolOfOne_ReturnsCleanObject()
    {
        // Arrange
        var pool = new ObjectPool<SampleClassWithReset>(
            factory: () => new SampleClassWithReset(),
            maxSize: 1);

        // Act
        using (RentedObject<SampleClassWithReset> obj1 = pool.Rent())
        {
            obj1.Value.Name = "Jason";
        }

        RentedObject<SampleClassWithReset> obj2 = pool.Rent();

        // Assert
        Assert.IsTrue(string.IsNullOrEmpty(obj2.Value.Name) && obj2.Value.Name != null);
    }

    [TestMethod]
    public void ObjectPool_CreatePoolWithZeroMaxSize_ThrowsException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            using var pool = new ObjectPool<SampleClassWithReset>(
                factory: () => new SampleClassWithReset(),
                maxSize: 0);
        });
    }

    [TestMethod]
    public void ObjectPool_CreatePoolWithoutResetDelegateAndClassWithoutReset_ThrowsException()
    {
        Assert.Throws<InvalidOperationException>(() =>
        {
            using var pool = new ObjectPool<SampleClassNoReset>(
                factory: () => new SampleClassNoReset(),
                maxSize: 10);
        });
    }

    [TestMethod]
    public void ObjectPool_CreatePoolWithResetDelegateAndClassWithoutReset_DoesNotThrowException()
    {
        using var pool = new ObjectPool<SampleClassNoReset>(
            factory: () => new SampleClassNoReset(),
            reset: pool => Console.WriteLine("Reset mechanism here..."),
            maxSize: 10);

        // We expect no exceptions to be thrown here
    }

    [TestMethod]
    public void ObjectPool_CreatePoolWithoutResetDelegateAndClassWithReset_DoesNotThrowException()
    {
        using var pool = new ObjectPool<SampleClassWithReset>(
            factory: () => new SampleClassWithReset(),
            maxSize: 10);

        // We expect no exceptions to be thrown here
    }
}
