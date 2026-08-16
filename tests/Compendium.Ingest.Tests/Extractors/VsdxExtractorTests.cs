using Compendium.Ingest.Extractors;
using OfficeIMO.Visio;

namespace Compendium.Ingest.Tests.Extractors;

public class VsdxExtractorTests
{
    [Fact]
    public void ExtractsShapesAndConnectorsAsSingleRecordPerPage()
    {
        var path = Path.GetTempFileName() + ".vsdx";
        WriteFixtureVsdx(path);

        try
        {
            var records = new VsdxExtractor().Extract(path);

            Assert.Single(records);
            Assert.Equal("Page-1", records[0].Title);
            Assert.Contains("Load Balancer", records[0].Text);
            Assert.Contains("- Load Balancer -> Web Server (label: routes)", records[0].Text);
            Assert.Equal("2", records[0].Metadata["shape_count"]);
            Assert.Equal("1", records[0].Metadata["connector_count"]);
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static void WriteFixtureVsdx(string path)
    {
        var document = VisioDocument.Create();
        var page = document.AddPage("Page-1", 8.5, 11, VisioMeasurementUnit.Inches, null);
        var lb = page.AddRectangle(1, 1, 2, 1, "Load Balancer");
        var web = page.AddRectangle(4, 1, 2, 1, "Web Server");
        var connector = page.AddConnector(lb, web, ConnectorKind.Straight, VisioSide.Auto, VisioSide.Auto);
        connector.Label = "routes";
        document.Save(path);
    }
}
