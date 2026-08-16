using System.Globalization;
using CsvHelper;

namespace Compendium.Ingest.Extractors;

// Wrapper extractor that auto-detects data map files and delegates to the
// appropriate extractor:
// - DataMapExtractor if headers match data map pattern (Int Name, Record Type, etc.)
// - CsvExtractor otherwise (default row-per-concept behavior)
public sealed class SmartCsvExtractor : IDocumentExtractor
{
    private static readonly string[] DataMapHeaders =
        ["Int Name", "Record Type", "SRC Column", "DST Column"];

    private readonly DataMapExtractor _dataMapExtractor = new();
    private readonly CsvExtractor _csvExtractor = new();

    public IReadOnlyList<ExtractedRecord> Extract(string filePath)
    {
        if (IsDataMapFile(filePath))
        {
            return _dataMapExtractor.Extract(filePath);
        }

        return _csvExtractor.Extract(filePath);
    }

    private static bool IsDataMapFile(string filePath)
    {
        try
        {
            using var reader = new StreamReader(filePath);
            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

            csv.Read();
            csv.ReadHeader();
            var headers = csv.HeaderRecord ?? [];

            return DataMapHeaders.All(required =>
                headers.Any(h => string.Equals(h, required, StringComparison.OrdinalIgnoreCase)));
        }
        catch
        {
            // If we can't read the headers, fall back to regular CSV extractor
            return false;
        }
    }
}
