using Compendium.Ingest.Extractors;

namespace Compendium.Ingest.Tests.Extractors;

public class MarkdownExtractorTests
{
    [Fact]
    public void PlainMarkdownIsUsedAsIs()
    {
        var path = Path.GetTempFileName();
        File.WriteAllText(path, "# Notes\n\nJust some notes.");
        try
        {
            var records = new MarkdownExtractor().Extract(path);

            Assert.Single(records);
            Assert.Contains("Just some notes.", records[0].Text);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void OkfFrontmatterIsStrippedAndTitleExtracted()
    {
        var path = Path.GetTempFileName();
        File.WriteAllText(path, "---\ntype: System\ntitle: Widget Service\n---\n\nBody text here.");
        try
        {
            var records = new MarkdownExtractor().Extract(path);

            Assert.Single(records);
            Assert.Equal("Widget Service", records[0].Title);
            Assert.Equal("Body text here.", records[0].Text);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
