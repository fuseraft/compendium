namespace Compendium.Okf.Tests;

public class ConceptWriterTests : IDisposable
{
    private static readonly string Sample = Path.Combine(
        AppContext.BaseDirectory, "..", "..", "..", "..", "..", "catalog", "sample");

    private static readonly DateTime AtUtc = new(2026, 8, 16, 0, 0, 0, DateTimeKind.Utc);
    private const string Actor = "agent:compendium-agent/0.1";

    private readonly string _bundleRoot;

    public ConceptWriterTests()
    {
        _bundleRoot = Path.Combine(Path.GetTempPath(), "compendium-writer-tests-" + Guid.NewGuid());
        CopyDirectory(Sample, _bundleRoot);
    }

    public void Dispose() => Directory.Delete(_bundleRoot, recursive: true);

    [Fact]
    public void CreateWritesDraftFrontmatterAndLogsCreation()
    {
        var result = ConceptWriter.Create(
            _bundleRoot, "System", "Notification Service", "Sends order status emails.",
            "# Overview\n\nSends emails.", tags: null, Actor, AtUtc);

        Assert.True(result.Success);

        var path = Path.Combine(_bundleRoot, "systems", "notification-service.md");
        var text = File.ReadAllText(path);
        Assert.Contains("type: System", text);
        Assert.Contains("status: draft", text);
        Assert.Contains($"generated: {{ by: {Actor}, at: 2026-08-16T00:00:00Z }}", text);
        Assert.Contains("tags: [agent-authored]", text);

        var log = File.ReadAllText(Path.Combine(_bundleRoot, "log.md"));
        Assert.Contains("**Creation**", log);
        Assert.Contains("systems/notification-service", log);
    }

    [Fact]
    public void CreateDedupesOnIdCollision()
    {
        ConceptWriter.Create(_bundleRoot, "System", "Billing Service", "Duplicate.", "# Overview", null, Actor, AtUtc);

        var path = Path.Combine(_bundleRoot, "systems", "billing-service-2.md");
        Assert.True(File.Exists(path));
    }

    [Fact]
    public void CreateRejectsPathTraversalTitle()
    {
        var result = ConceptWriter.Create(
            _bundleRoot, "..", "../../evil", "desc", "body", null, Actor, AtUtc);

        Assert.False(result.Success);
    }

    [Fact]
    public void UpdateBodyPreservesOtherFrontmatterAndResetsStatus()
    {
        var result = ConceptWriter.UpdateBody(_bundleRoot, "systems/billing-service", "# Overview\n\nUpdated text.", Actor, AtUtc);

        Assert.True(result.Success);

        var text = File.ReadAllText(Path.Combine(_bundleRoot, "systems", "billing-service.md"));
        Assert.Contains("title: Billing Service", text);
        Assert.Contains("status: draft", text);
        Assert.Contains($"generated: {{ by: {Actor}, at: 2026-08-16T00:00:00Z }}", text);
        Assert.Contains("Updated text.", text);
    }

    [Fact]
    public void UpdateBodyMissingIdFails()
    {
        var result = ConceptWriter.UpdateBody(_bundleRoot, "systems/does-not-exist", "body", Actor, AtUtc);

        Assert.False(result.Success);
    }

    [Fact]
    public void AddLinkInsertsUnderExistingHeading()
    {
        var result = ConceptWriter.AddLink(
            _bundleRoot, "systems/billing-service", "systems/inventory-service", "Inventory", "Integrations", Actor, AtUtc);

        Assert.True(result.Success);

        var text = File.ReadAllText(Path.Combine(_bundleRoot, "systems", "billing-service.md"));
        Assert.Contains("- [Inventory](/systems/inventory-service.md)", text);
    }

    [Fact]
    public void AddLinkKeepsBlankLineBeforeFollowingProseParagraph()
    {
        // "Integrations" in the billing-service fixture is a heading
        // directly followed by a prose paragraph (no blank line list
        // items to sit alongside) — the inserted link must not run
        // straight into that paragraph's text.
        ConceptWriter.AddLink(
            _bundleRoot, "systems/billing-service", "systems/inventory-service", "Inventory", "Integrations", Actor, AtUtc);

        var text = File.ReadAllText(Path.Combine(_bundleRoot, "systems", "billing-service.md"));
        Assert.Contains("- [Inventory](/systems/inventory-service.md)\n\nOnce payment settles", text);
    }

    [Fact]
    public void AddLinkCreatesHeadingWhenAbsent()
    {
        var result = ConceptWriter.AddLink(
            _bundleRoot, "systems/billing-service", "systems/inventory-service", "Inventory", "Related Systems", Actor, AtUtc);

        Assert.True(result.Success);

        var text = File.ReadAllText(Path.Combine(_bundleRoot, "systems", "billing-service.md"));
        Assert.Contains("# Related Systems", text);
        Assert.Contains("- [Inventory](/systems/inventory-service.md)", text);
    }

    [Fact]
    public void AddLinkToleratesDanglingTarget()
    {
        var result = ConceptWriter.AddLink(
            _bundleRoot, "systems/billing-service", "systems/does-not-exist", "Nothing", "Integrations", Actor, AtUtc);

        Assert.True(result.Success);
        Assert.Contains("dangling", result.Message);
    }

    [Fact]
    public void FlagForReviewDoesNotModifyConceptFile()
    {
        var before = File.ReadAllText(Path.Combine(_bundleRoot, "systems", "billing-service.md"));

        var result = ConceptWriter.FlagForReview(_bundleRoot, "systems/billing-service", "Looks stale.", Actor, AtUtc);

        Assert.True(result.Success);
        var after = File.ReadAllText(Path.Combine(_bundleRoot, "systems", "billing-service.md"));
        Assert.Equal(before, after);

        var log = File.ReadAllText(Path.Combine(_bundleRoot, "log.md"));
        Assert.Contains("**Flag**", log);
        Assert.Contains("Looks stale.", log);
    }

    [Fact]
    public void LogAppendsNewestFirstAndGroupsBySameDay()
    {
        ConceptWriter.FlagForReview(_bundleRoot, "systems/billing-service", "First flag.", Actor, AtUtc);
        ConceptWriter.FlagForReview(_bundleRoot, "systems/inventory-service", "Second flag.", Actor, AtUtc);

        var log = File.ReadAllText(Path.Combine(_bundleRoot, "log.md"));
        var headingCount = log.Split('\n').Count(l => l.TrimEnd('\r') == "## 2026-08-16");
        Assert.Equal(1, headingCount);
        Assert.True(log.IndexOf("Second flag.", StringComparison.Ordinal) < log.IndexOf("First flag.", StringComparison.Ordinal));
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
