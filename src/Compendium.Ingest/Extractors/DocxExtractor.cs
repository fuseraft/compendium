using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace Compendium.Ingest.Extractors;

public sealed class DocxExtractor : IDocumentExtractor
{
    public IReadOnlyList<ExtractedRecord> Extract(string filePath)
    {
        using var document = WordprocessingDocument.Open(filePath, isEditable: false);
        var body = document.MainDocumentPart?.Document?.Body;
        var title = Path.GetFileNameWithoutExtension(filePath);

        var text = body is null
            ? ""
            : string.Join("\n", body.Elements<Paragraph>().Select(p => p.InnerText));

        return new[] { new ExtractedRecord(title, text, new Dictionary<string, string>()) };
    }
}
