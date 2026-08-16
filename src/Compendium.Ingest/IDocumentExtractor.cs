namespace Compendium.Ingest;

public interface IDocumentExtractor
{
    IReadOnlyList<ExtractedRecord> Extract(string filePath);
}
