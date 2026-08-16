using Compendium.Ingest.Extractors;

namespace Compendium.Ingest.Tests.Extractors;

public class CsvExtractorTests
{
    [Fact]
    public void OneRecordPerRowWithColumnsAsMetadata()
    {
        var path = Path.GetTempFileName();
        File.WriteAllText(path, "Name,Owner,Description\nWidget,Team A,\"Handles, widgets\"\nGadget,Team B,Simple");
        try
        {
            var records = new CsvExtractor().Extract(path);

            Assert.Equal(2, records.Count);
            Assert.Equal("Widget", records[0].Title);
            Assert.Equal("Team A", records[0].Metadata["Owner"]);
            Assert.Equal("Handles, widgets", records[0].Metadata["Description"]);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void FallsBackToRowNumberWhenNoTitleColumn()
    {
        var path = Path.GetTempFileName();
        File.WriteAllText(path, "Col1,Col2\nA,B");
        try
        {
            var records = new CsvExtractor().Extract(path);

            Assert.Single(records);
            Assert.Contains("row 1", records[0].Title);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
