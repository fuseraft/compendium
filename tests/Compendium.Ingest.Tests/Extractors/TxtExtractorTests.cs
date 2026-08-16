using Compendium.Ingest.Extractors;

namespace Compendium.Ingest.Tests.Extractors;

public class TxtExtractorTests
{
    [Fact]
    public void ReturnsWholeFileAsOneRecord()
    {
        var path = Path.GetTempFileName();
        File.WriteAllText(path, "Plain text content.");
        try
        {
            var records = new TxtExtractor().Extract(path);

            Assert.Single(records);
            Assert.Equal("Plain text content.", records[0].Text);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
