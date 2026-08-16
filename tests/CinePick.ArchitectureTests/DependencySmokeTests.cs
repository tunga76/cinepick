namespace CinePick.ArchitectureTests;

public sealed class DependencySmokeTests
{
    [Fact]
    public void DomainAssemblyHasNoCinePickProjectReferences()
    {
        var references = typeof(Domain.AssemblyMarker).Assembly.GetReferencedAssemblies();

        Assert.DoesNotContain(
            references,
            assembly => assembly.Name?.StartsWith("CinePick.", StringComparison.Ordinal) is true);
    }
}
