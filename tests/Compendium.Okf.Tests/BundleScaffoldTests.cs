namespace Compendium.Okf.Tests;

public class BundleScaffoldTests : IDisposable
{
    private static readonly DateTime AtUtc = new(2026, 8, 17, 0, 0, 0, DateTimeKind.Utc);

    private readonly string _path;

    public BundleScaffoldTests()
    {
        _path = Path.Combine(Path.GetTempPath(), "compendium-scaffold-tests-" + Guid.NewGuid());
    }

    public void Dispose()
    {
        if (Directory.Exists(_path))
        {
            Directory.Delete(_path, recursive: true);
        }
    }

    [Fact]
    public void CreateWritesExpectedStructure()
    {
        var result = BundleScaffold.Create(_path, AtUtc);

        Assert.True(result.Success);
        Assert.True(File.Exists(Path.Combine(_path, ".compendium", "config.json")));
        Assert.True(File.Exists(Path.Combine(_path, "index.md")));
        Assert.True(File.Exists(Path.Combine(_path, "log.md")));
        Assert.True(File.Exists(Path.Combine(_path, "references", ".gitkeep")));
        Assert.True(File.Exists(Path.Combine(_path, "systems", "example-system.md")));
    }

    [Fact]
    public void CreateFailsWhenDirectoryIsNonEmpty()
    {
        Directory.CreateDirectory(_path);
        File.WriteAllText(Path.Combine(_path, "existing.txt"), "hello");

        var result = BundleScaffold.Create(_path, AtUtc);

        Assert.False(result.Success);
        Assert.False(File.Exists(Path.Combine(_path, ".compendium", "config.json")));
    }

    [Fact]
    public void CreateSucceedsWhenDirectoryExistsButIsEmpty()
    {
        Directory.CreateDirectory(_path);

        var result = BundleScaffold.Create(_path, AtUtc);

        Assert.True(result.Success);
    }

    [Fact]
    public void ScaffoldedBundleLoadsAndSeedConceptIsReadable()
    {
        BundleScaffold.Create(_path, AtUtc);

        var bundle = BundleLoader.LoadBundle(_path);

        Assert.True(bundle.Concepts.ContainsKey("systems/example-system"));
        Assert.Equal("System", bundle.Concepts["systems/example-system"].Type);
        Assert.Equal("draft", bundle.Concepts["systems/example-system"].Status);
    }

    [Fact]
    public void ScaffoldedConfigDeclaresDefaultTypesInProposeMode()
    {
        BundleScaffold.Create(_path, AtUtc);

        var config = BundleConfig.Load(_path);

        Assert.True(config.IsKnownType("System"));
        Assert.True(config.IsKnownType("Process"));
        Assert.True(config.IsKnownType("Integration"));
        Assert.Equal("propose", config.AllowNewTypes);
    }
}
