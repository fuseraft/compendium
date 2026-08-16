using Compendium.Ingest.Extractors;

namespace Compendium.Ingest.Tests.Extractors;

public class EmlExtractorTests
{
    [Fact]
    public void ExtractsSubjectAndBodyAsSingleRecord()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Fixtures", "fixture.eml");

        var records = new EmlExtractor().Extract(path);

        Assert.Single(records);
        Assert.Equal("Quarterly budget review", records[0].Title);
        Assert.Contains("scheduled for next Thursday", records[0].Text);
        Assert.Contains("Alice Example", records[0].Metadata["From"]);
        Assert.Contains("Bob Example", records[0].Metadata["To"]);
    }
}
