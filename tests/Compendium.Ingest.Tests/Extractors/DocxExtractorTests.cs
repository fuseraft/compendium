using Compendium.Ingest.Extractors;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace Compendium.Ingest.Tests.Extractors;

public class DocxExtractorTests
{
    [Fact]
    public void ExtractsParagraphTextAsSingleRecord()
    {
        var path = Path.GetTempFileName() + ".docx";
        WriteFixtureDocx(path, "First paragraph.", "Second paragraph.");
        try
        {
            var records = new DocxExtractor().Extract(path);

            Assert.Single(records);
            Assert.Contains("First paragraph.", records[0].Text);
            Assert.Contains("Second paragraph.", records[0].Text);
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static void WriteFixtureDocx(string path, params string[] paragraphs)
    {
        using var document = WordprocessingDocument.Create(path, WordprocessingDocumentType.Document);
        var mainPart = document.AddMainDocumentPart();
        mainPart.Document = new Document();
        var body = mainPart.Document.AppendChild(new Body());

        foreach (var text in paragraphs)
        {
            body.AppendChild(new Paragraph(new Run(new Text(text))));
        }

        mainPart.Document.Save();
    }
}
