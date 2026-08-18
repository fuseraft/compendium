namespace Compendium.Okf.Tests;

public class BundleConfigTests : IDisposable
{
    private readonly string _bundleRoot;

    public BundleConfigTests()
    {
        _bundleRoot = Path.Combine(Path.GetTempPath(), "compendium-bundle-config-tests-" + Guid.NewGuid());
        Directory.CreateDirectory(_bundleRoot);
    }

    public void Dispose() => Directory.Delete(_bundleRoot, recursive: true);

    [Fact]
    public void LoadReturnsUnconstrainedWhenConfigFileIsMissing()
    {
        var config = BundleConfig.Load(_bundleRoot);

        Assert.Empty(config.Types);
        Assert.Equal(TypeDecision.Known, config.CheckType("AnythingGoes"));
    }

    [Fact]
    public void LoadParsesTypesAndAllowNewTypes()
    {
        WriteConfig("""
            {
              "name": "test-bundle",
              "types": {
                "System": { "directory": "systems", "description": "An app or service." }
              },
              "allow_new_types": "closed"
            }
            """);

        var config = BundleConfig.Load(_bundleRoot);

        Assert.Equal("test-bundle", config.Name);
        Assert.True(config.IsKnownType("system")); // case-insensitive
        Assert.Equal("closed", config.AllowNewTypes);
    }

    [Fact]
    public void CheckTypeRejectsUnknownTypeWhenClosed()
    {
        WriteConfig("""{ "types": { "System": {} }, "allow_new_types": "closed" }""");

        var config = BundleConfig.Load(_bundleRoot);

        Assert.Equal(TypeDecision.Rejected, config.CheckType("Widget"));
    }

    [Fact]
    public void CheckTypeProposesUnknownTypeWhenModeIsPropose()
    {
        WriteConfig("""{ "types": { "System": {} }, "allow_new_types": "propose" }""");

        var config = BundleConfig.Load(_bundleRoot);

        Assert.Equal(TypeDecision.Proposed, config.CheckType("Widget"));
    }

    [Fact]
    public void CheckTypeIsKnownWhenNoTypesAreDeclaredRegardlessOfMode()
    {
        WriteConfig("""{ "types": {}, "allow_new_types": "closed" }""");

        var config = BundleConfig.Load(_bundleRoot);

        Assert.Equal(TypeDecision.Known, config.CheckType("Widget"));
    }

    private void WriteConfig(string json)
    {
        var dir = Path.Combine(_bundleRoot, ".compendium");
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "config.json"), json);
    }
}
