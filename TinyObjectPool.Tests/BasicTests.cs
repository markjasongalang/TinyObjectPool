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
    }
}
