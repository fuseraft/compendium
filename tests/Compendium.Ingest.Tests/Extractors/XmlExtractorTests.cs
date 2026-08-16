using Compendium.Ingest.Extractors;

namespace Compendium.Ingest.Tests.Extractors;

public class XmlExtractorTests
{
    [Fact]
    public void RepeatingChildrenProduceOneRecordEach()
    {
        var path = Path.GetTempFileName();
        File.WriteAllText(
            path,
            "<items><item><name>Alpha</name><owner>Team A</owner></item><item><name>Beta</name><owner>Team B</owner></item></items>");
        try
        {
            var records = new XmlExtractor().Extract(path);

            Assert.Equal(2, records.Count);
            Assert.Equal("Alpha", records[0].Title);
            Assert.Equal("Team A", records[0].Metadata["owner"]);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void NonRepeatingRootProducesSingleRecord()
    {
        var path = Path.GetTempFileName();
        File.WriteAllText(path, "<config><setting>value</setting></config>");
        try
        {
            var records = new XmlExtractor().Extract(path);

            Assert.Single(records);
            Assert.Equal("value", records[0].Metadata["setting"]);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
