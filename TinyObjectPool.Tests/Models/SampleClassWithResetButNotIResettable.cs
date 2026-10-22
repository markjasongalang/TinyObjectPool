namespace TinyObjectPool.Tests.Models;

/// <summary>
/// A class that intentionally doesn't implement <see cref="IResettable"/> for testing.
/// </summary>
public class SampleClassWithResetButNotIResettable
{
    public string? Name { get; set; }

    public void Reset()
    {
        Name = string.Empty;
    }
}
