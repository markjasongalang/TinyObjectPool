using TinyObjectPool.Tests.Models;

namespace TinyObjectPool.Tests;

[TestClass]
public sealed class BasicTests
{
    /// <remarks>
    /// <para>
    /// Test names - naming convention
    /// </para>
    /// <para>
    /// UnitOfWork_Scenario_ExpectedResult
    /// </para>
    /// <para>
    /// Refer to <see href="https://canro91.github.io/2021/04/12/UnitTestNamingConventions/"/>
    /// </para>
    /// <para>
    /// The AAA (Arrange, Act, Assert) pattern is a common way of writing unit tests for a method test.
    /// </para>
    /// <para>
    /// Refer to <see href="https://learn.microsoft.com/en-us/visualstudio/test/unit-test-basics?view=visualstudio"/>
    /// </para>
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
            obj1.Unwrap().Name = "Jason";
        }

        RentedObject<SampleClassWithReset> obj2 = pool.Rent();

        // Assert
        Assert.IsTrue(string.IsNullOrEmpty(obj2.Unwrap().Name) && obj2.Unwrap().Name != null);
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

    /// <remarks>
    /// We expect no exceptions to be thrown here.
    /// </remarks>
    [TestMethod]
    public void ObjectPool_CreatePoolWithResetDelegateAndClassWithoutReset_DoesNotThrowException()
    {
        using var pool = new ObjectPool<SampleClassNoReset>(
            factory: () => new SampleClassNoReset(),
            reset: pool => Console.WriteLine("Reset mechanism here..."),
            maxSize: 10);
    }

    /// <remarks>
    /// We expect no exceptions to be thrown here.
    /// </remarks>
    [TestMethod]
    public void ObjectPool_CreatePoolWithoutResetDelegateAndClassWithReset_DoesNotThrowException()
    {
        using var pool = new ObjectPool<SampleClassWithReset>(
            factory: () => new SampleClassWithReset(),
            maxSize: 10);
    }

    [TestMethod]
    public void ObjectPool_CreatePoolWithoutResetDelegateAndClassWithResetButNotIResettable_ThrowsException()
    {
        Assert.Throws<InvalidOperationException>(() =>
        {
            using var pool = new ObjectPool<SampleClassWithResetButNotIResettable>(
                factory: () => new SampleClassWithResetButNotIResettable(),
                maxSize: 10);
        });
    }

    /// <remarks>
    /// <para>
    /// Since there's already a <c>reset</c> delegate defined upon pool creation, there's no need to throw an exception if 
    /// the class of type T doesn't implement <see cref="IResettable"/> properly.
    /// </para>
    /// <para>
    /// Also, only classes that implement <see cref="IResettable"/> are allowed to invoke its <see cref="IResettable.Reset"/>.
    /// </para>
    /// </remarks>
    [TestMethod]
    public void ObjectPool_CreatePoolWithResetDelegateAndClassWithResetButNotIResettable_DoesNotThrowException()
    {
        using var pool = new ObjectPool<SampleClassWithResetButNotIResettable>(
            factory: () => new SampleClassWithResetButNotIResettable(),
            reset: pool => Console.WriteLine("Reset mechanism here..."),
            maxSize: 10);
    }
}
