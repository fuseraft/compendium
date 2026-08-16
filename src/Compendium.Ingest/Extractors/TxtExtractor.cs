namespace Compendium.Ingest.Extractors;

public sealed class TxtExtractor : IDocumentExtractor
{
    public IReadOnlyList<ExtractedRecord> Extract(string filePath)
    {
        var text = File.ReadAllText(filePath);
        var title = Path.GetFileNameWithoutExtension(filePath);
        return new[] { new ExtractedRecord(title, text, new Dictionary<string, string>()) };
    }
}
