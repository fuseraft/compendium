namespace Compendium.Ingest.Tests;

public class ConceptBuilderTests
{
    private static readonly ConceptOptions Options = new(
        Type: "Document",
        Format: "txt",
        SourceResourcePath: "/references/notes.txt",
        SourceTitle: "notes.txt",
        GeneratedAtUtc: new DateTime(2026, 8, 16, 0, 0, 0, DateTimeKind.Utc));

    [Fact]
    public void ProducesRequiredFrontmatterFields()
    {
        var record = new ExtractedRecord("My Note", "Some body text.", new Dictionary<string, string>());

        var markdown = ConceptBuilder.Build(record, Options);

        Assert.Contains("type: Document", markdown);
        Assert.Contains("title: \"My Note\"", markdown);
        Assert.Contains("status: draft", markdown);
        Assert.Contains("generated: { by: process:compendium-ingest/0.1, at: 2026-08-16T00:00:00Z }", markdown);
        Assert.Contains("resource: /references/notes.txt", markdown);
        Assert.Contains("# Overview", markdown);
        Assert.Contains("Some body text.", markdown);
    }

    [Fact]
    public void MetadataBecomesFrontmatterAndDetailsBullets()
    {
        var record = new ExtractedRecord(
            "Row 1",
            "Owner: Finance",
            new Dictionary<string, string> { ["Owner Team"] = "Finance" });

        var markdown = ConceptBuilder.Build(record, Options);

        Assert.Contains("owner_team: \"Finance\"", markdown);
        Assert.Contains("# Details", markdown);
        Assert.Contains("- **Owner Team:** Finance", markdown);
    }

    [Fact]
    public void TruncatesLongDescriptionToFirstSentence()
    {
        var longText = "First sentence is short. " + new string('x', 300);
        var record = new ExtractedRecord("Long", longText, new Dictionary<string, string>());

        var markdown = ConceptBuilder.Build(record, Options);

        Assert.Contains("description: \"First sentence is short.\"", markdown);
    }

    [Fact]
    public void EscapesQuotesInYamlStrings()
    {
        var record = new ExtractedRecord("A \"quoted\" title", "text", new Dictionary<string, string>());

        var markdown = ConceptBuilder.Build(record, Options);

        Assert.Contains("title: \"A \\\"quoted\\\" title\"", markdown);
    }

    [Fact]
    public void SourceMetadataCannotShadowTrustOrProvenanceFields()
    {
        var record = new ExtractedRecord(
            "Row 1",
            "text",
            new Dictionary<string, string>
            {
                ["status"] = "verified",
                ["Sources"] = "http://attacker.example/fake",
                ["generated"] = "by human",
                ["type"] = "Something Else",
            });

        var markdown = ConceptBuilder.Build(record, Options);

        Assert.Contains("status: draft", markdown);
        Assert.DoesNotContain("\nstatus: \"verified\"", markdown);
        Assert.Contains("source_status: \"verified\"", markdown);
        Assert.Contains("source_sources: \"http://attacker.example/fake\"", markdown);
        Assert.Contains("source_generated: \"by human\"", markdown);
        Assert.Contains("source_type: \"Something Else\"", markdown);
        Assert.Contains("type: Document", markdown);
    }
}
