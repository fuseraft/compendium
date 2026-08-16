using UglyToad.PdfPig;

namespace Compendium.Ingest.Extractors;

public sealed class PdfExtractor : IDocumentExtractor
{
    public IReadOnlyList<ExtractedRecord> Extract(string filePath)
    {
        using var document = PdfDocument.Open(filePath);
        var title = Path.GetFileNameWithoutExtension(filePath);
        var text = string.Join("\n\n", document.GetPages().Select(p => p.Text));

        return new[] { new ExtractedRecord(title, text, new Dictionary<string, string>()) };
    }
}
