using EkadashiEngine.Core;

namespace EkadashiEngine.Tests;

public sealed class ArchitectureTests
{
    [Fact]
    public void CoreDoesNotReferenceDesktopOrUiFrameworks()
    {
        var references = typeof(Location).Assembly.GetReferencedAssemblies();

        Assert.DoesNotContain(references, reference =>
            reference.Name!.StartsWith("Avalonia", StringComparison.Ordinal) ||
            reference.Name.StartsWith("EkadashiEngine.Desktop", StringComparison.Ordinal) ||
            reference.Name.StartsWith("SwissEph", StringComparison.Ordinal) ||
            reference.Name.StartsWith("EkadashiEngine.Astronomy", StringComparison.Ordinal));
    }
}
