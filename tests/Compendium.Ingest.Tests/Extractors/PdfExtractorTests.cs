using Compendium.Ingest.Extractors;

namespace Compendium.Ingest.Tests.Extractors;

public class PdfExtractorTests
{
    [Fact]
    public void ExtractsTextAsSingleRecord()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Fixtures", "fixture.pdf");

        var records = new PdfExtractor().Extract(path);

        Assert.Single(records);
        Assert.Contains("Fixture PDF text for extraction test.", records[0].Text);
        Assert.Equal("fixture", records[0].Title);
    }
}
