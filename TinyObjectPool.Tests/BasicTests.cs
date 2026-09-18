namespace TinyObjectPool.Tests
{
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
            var pool = new ObjectPool<MyObject>(
                factory: () => new MyObject(),
                maxSize: 1);

            // Act
            using (RentedObject<MyObject> obj1 = pool.Rent())
            {
                obj1.Value.Name = "Jason";
            }

            RentedObject<MyObject> obj2 = pool.Rent();

            // Assert
            Assert.IsTrue(string.IsNullOrEmpty(obj2.Value.Name) && obj2.Value.Name != null);
        }

        [TestMethod]
        public async Task Rent_MultipleTasksRentSimultaneously_BlockWhileWaiting()
        {
            var pool = new ObjectPool<MyObject>(
                factory: () => new MyObject(),
                maxSize: 1); // Set to 1 for testing below

            Task task1 = Task.Run(async () =>
            {
                // Add explicit scope so the object would be returned before the assertion below
                using (RentedObject<MyObject> obj1 = pool.Rent())
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
                using RentedObject<MyObject> obj2 = pool.Rent();

                Console.WriteLine($"[{DateTime.UtcNow:HH:mm:ss}] Task 2: Successfully Rented object!");
            });

            await Task.WhenAll(task1, task2);
        }

        [TestMethod]
        public async Task Rent_NotReturnRentedObject_ThrowTimeoutException()
        {
            var pool = new ObjectPool<MyObject>(
                factory: () => new MyObject(),
                maxSize: 1);

            // Object 1
            Console.WriteLine($"[{DateTime.UtcNow:HH:mm:ss}] Object 1 trying to rent");

            RentedObject<MyObject> obj1 = pool.Rent(); // Intentionally doesn't include 'using'

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
                using RentedObject<MyObject> obj2 = pool.Rent();
            });

            Console.WriteLine($"[{DateTime.UtcNow:HH:mm:ss}] Object 2 successfully rented");
        }

        [TestMethod]
        public async Task Rent_RentOnDisposedPool_ThrowObjectDisposedException()
        {
            using var pool = new ObjectPool<MyObject>(
                factory: () => new MyObject(),
                maxSize: 10);

            pool.Dispose();

            Assert.Throws<ObjectDisposedException>(() =>
            {
                using RentedObject<MyObject> obj = pool.Rent();
            });
        }

        [TestMethod]
        public void Dispose_RentedObjectNotReturned_PoolDisposesObjects()
        {
            using var pool = new ObjectPool<MyObject>(
                factory: () => new MyObject(),
                maxSize: 10);

            // Explicitly didn't include 'using' keyword below so the object will not be returned
            RentedObject<MyObject> obj = pool.Rent();

            pool.Dispose();

            Assert.AreEqual(0, pool.Count);
            Assert.IsTrue(string.IsNullOrEmpty(obj.Value.Name));
        }

        [TestMethod]
        public void Dispose_RentObjectOnDisposedPool_ThrowsObjectDisposedException()
        {
            using var pool = new ObjectPool<MyObject>(
                factory: () => new MyObject(),
                maxSize: 10);

            pool.Dispose();

            Assert.Throws<ObjectDisposedException>(() =>
            {
                using RentedObject<MyObject> obj = pool.Rent();
            });
        }

        [TestMethod]
        public void Rent_InstantiateObjectWithUsingStatement_AutomaticallyReturnsToPool()
        {
            using var pool = new ObjectPool<MyObject>(
                factory: () => new MyObject(),
                maxSize: 1);

            // Use known, hard-coded, pre-calculated values instead
            // Refer to https://dev.to/canro91/4-common-mistakes-when-writing-your-first-unit-tests-44e9
            Assert.AreEqual(0, pool.Count);

            using (RentedObject<MyObject> myObj = pool.Rent())
            {
                // Some actual operations here
            }

            Assert.AreEqual(1, pool.Count);
        }

        [TestMethod]
        public void ObjectPool_CreatePoolWithZeroMaxSize_ThrowsException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                using var pool = new ObjectPool<MyObject>(
                    factory: () => new MyObject(),
                    maxSize: 0);
            });
        }

        [TestMethod]
        public void Rent_InstantiateNullableRentedObject_ReturnsNotNullObject()
        {
            using var pool = new ObjectPool<MyObject>(
                factory: () => new MyObject(),
                maxSize: 10);

            Assert.AreEqual(0, pool.Count);

            using RentedObject<MyObject>? obj = pool.Rent();

            Assert.AreEqual(0, pool.Count);
            Assert.IsNotNull(obj);
        }
    }
}
