using Compendium.Ingest.Extractors;

namespace Compendium.Ingest.Tests.Extractors;

public class JsonExtractorTests
{
    [Fact]
    public void ArrayRootProducesOneRecordPerElement()
    {
        var path = Path.GetTempFileName();
        File.WriteAllText(path, """[{"name": "Alpha", "owner": "Team A"}, {"name": "Beta", "owner": "Team B"}]""");
        try
        {
            var records = new JsonExtractor().Extract(path);

            Assert.Equal(2, records.Count);
            Assert.Equal("Alpha", records[0].Title);
            Assert.Equal("Team A", records[0].Metadata["owner"]);
            Assert.Equal("Beta", records[1].Title);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void ObjectRootProducesSingleRecord()
    {
        var path = Path.GetTempFileName();
        File.WriteAllText(path, """{"config": "value"}""");
        try
        {
            var records = new JsonExtractor().Extract(path);

            Assert.Single(records);
            Assert.Equal("value", records[0].Metadata["config"]);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
