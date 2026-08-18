using Compendium.Agent;
using Compendium.Okf;

namespace Compendium.Agent.Tests;

public class CompendiumToolsTests
{
    private static readonly string Sample = Path.Combine(
        AppContext.BaseDirectory, "..", "..", "..", "..", "..", "catalog", "sample");

    private static CompendiumTools LoadTools() => new(BundleLoader.LoadBundle(Sample));

    [Fact]
    public void ListConceptsFiltersByType()
    {
        var tools = LoadTools();

        var result = tools.ListConcepts("System");

        Assert.Contains("systems/billing-service", result);
        Assert.DoesNotContain("processes/order-fulfillment", result);
    }

    [Fact]
    public void ReadConceptReturnsRawText()
    {
        var tools = LoadTools();

        var result = tools.ReadConcept("systems/billing-service");

        Assert.Contains("type: System", result);
    }

    [Fact]
    public void ReadConceptMissingId()
    {
        var tools = LoadTools();

        var result = tools.ReadConcept("nope");

        Assert.Contains("No concept", result);
    }

    [Fact]
    public void SearchConceptsMatchesBodyText()
    {
        var tools = LoadTools();

        var result = tools.SearchConcepts("reservation");

        Assert.Contains("integrations/billing-to-inventory", result);
    }

    [Fact]
    public void ListConceptTypesReportsNoSpecWhenBundleHasNoConfig()
    {
        var tools = LoadTools();

        var result = tools.ListConceptTypes();

        Assert.Contains(".compendium/config.json", result);
    }
}

public class CompendiumToolsWriteTests : IDisposable
{
    private static readonly string Sample = Path.Combine(
        AppContext.BaseDirectory, "..", "..", "..", "..", "..", "catalog", "sample");

    private readonly string _bundleRoot;
    private readonly CompendiumTools _tools;

    public CompendiumToolsWriteTests()
    {
        _bundleRoot = Path.Combine(Path.GetTempPath(), "compendium-tools-tests-" + Guid.NewGuid());
        CopyDirectory(Sample, _bundleRoot);
        _tools = new CompendiumTools(BundleLoader.LoadBundle(_bundleRoot));
    }

    public void Dispose() => Directory.Delete(_bundleRoot, recursive: true);

    [Fact]
    public void CreatedConceptIsImmediatelyReadableInTheSameSession()
    {
        _tools.CreateConcept("System", "Notification Service", "Sends order status emails.", "# Overview\n\nSends emails.");

        var result = _tools.ReadConcept("systems/notification-service");

        Assert.Contains("status: draft", result);
        Assert.DoesNotContain("No concept", result);
    }

    [Fact]
    public void UpdatedBodyIsImmediatelyVisibleInTheSameSession()
    {
        _tools.UpdateConceptBody("systems/billing-service", "# Overview\n\nBrand new text.");

        var result = _tools.ReadConcept("systems/billing-service");

        Assert.Contains("Brand new text.", result);
    }

    [Fact]
    public void AddLinkIsImmediatelyVisibleInTheSameSession()
    {
        _tools.AddLink("systems/billing-service", "systems/inventory-service", "Inventory", "Integrations");

        var result = _tools.ReadConcept("systems/billing-service");

        Assert.Contains("- [Inventory](/systems/inventory-service.md)", result);
    }

    [Fact]
    public void FlagForReviewDoesNotChangeListConceptsOutput()
    {
        var before = _tools.ListConcepts();

        var message = _tools.FlagForReview("systems/billing-service", "Looks stale.");

        Assert.Contains("Flagged", message);
        Assert.Equal(before, _tools.ListConcepts());
    }

    [Fact]
    public void CreateConceptRejectsUnknownTypeWhenBundleConfigIsClosed()
    {
        WriteBundleConfig("""{ "types": { "System": { "directory": "systems" } }, "allow_new_types": "closed" }""");

        var result = _tools.CreateConcept("Widget", "Some Widget", "desc", "# Overview");

        Assert.Contains("not a recognized concept type", result);
        Assert.False(File.Exists(Path.Combine(_bundleRoot, "widgets", "some-widget.md")));
    }

    [Fact]
    public void CreateConceptSucceedsAndLogsProposalWhenTypeUnknownAndModeIsPropose()
    {
        WriteBundleConfig("""{ "types": { "System": { "directory": "systems" } }, "allow_new_types": "propose" }""");

        var result = _tools.CreateConcept("Widget", "Some Widget", "desc", "# Overview");

        Assert.Contains("Created", result);
        Assert.True(File.Exists(Path.Combine(_bundleRoot, "widgets", "some-widget.md")));

        var log = File.ReadAllText(Path.Combine(_bundleRoot, "log.md"));
        Assert.Contains("New type proposed", log);
    }

    [Fact]
    public void ListConceptTypesReturnsDeclaredTypesFromConfig()
    {
        WriteBundleConfig("""
            {
              "types": {
                "System": { "directory": "systems", "description": "An app or service." }
              },
              "allow_new_types": "propose"
            }
            """);

        var result = _tools.ListConceptTypes();

        Assert.Contains("System", result);
        Assert.Contains("An app or service.", result);
    }

    private void WriteBundleConfig(string json)
    {
        var dir = Path.Combine(_bundleRoot, ".compendium");
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "config.json"), json);
    }

    private static void CopyDirectory(string source, string destination)
    {
        Directory.CreateDirectory(destination);
        foreach (var dir in Directory.GetDirectories(source, "*", SearchOption.AllDirectories))
        {
            Directory.CreateDirectory(Path.Combine(destination, Path.GetRelativePath(source, dir)));
        }

        foreach (var file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
        {
            File.Copy(file, Path.Combine(destination, Path.GetRelativePath(source, file)));
        }
    }
}
