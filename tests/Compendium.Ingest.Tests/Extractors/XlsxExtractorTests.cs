using ClosedXML.Excel;
using Compendium.Ingest.Extractors;

namespace Compendium.Ingest.Tests.Extractors;

public class XlsxExtractorTests
{
    [Fact]
    public void OneRecordPerRowWithColumnsAsMetadata()
    {
        var path = Path.GetTempFileName() + ".xlsx";
        using (var workbook = new XLWorkbook())
        {
            var sheet = workbook.Worksheets.Add("Sheet1");
            sheet.Cell(1, 1).Value = "Name";
            sheet.Cell(1, 2).Value = "Owner";
            sheet.Cell(2, 1).Value = "Widget";
            sheet.Cell(2, 2).Value = "Team A";
            sheet.Cell(3, 1).Value = "Gadget";
            sheet.Cell(3, 2).Value = "Team B";
            workbook.SaveAs(path);
        }

        try
        {
            var records = new XlsxExtractor().Extract(path);

            Assert.Equal(2, records.Count);
            Assert.Equal("Widget", records[0].Title);
            Assert.Equal("Team A", records[0].Metadata["Owner"]);
            Assert.Equal("Gadget", records[1].Title);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
