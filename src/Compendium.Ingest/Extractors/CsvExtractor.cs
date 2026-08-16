using System.Globalization;
using CsvHelper;

namespace Compendium.Ingest.Extractors;

// One record per row — CsvHelper (not a hand-rolled split) so embedded
// commas/newlines in quoted fields are handled correctly.
public sealed class CsvExtractor : IDocumentExtractor
{
    private static readonly string[] TitleHeaders = ["Name", "Title", "name", "title"];

    public IReadOnlyList<ExtractedRecord> Extract(string filePath)
    {
        using var reader = new StreamReader(filePath);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

        csv.Read();
        csv.ReadHeader();
        var headers = csv.HeaderRecord ?? [];

        var records = new List<ExtractedRecord>();
        var rowNumber = 1;
        while (csv.Read())
        {
            var metadata = new Dictionary<string, string>();
            foreach (var header in headers)
            {
                metadata[header] = csv.GetField(header) ?? "";
            }

            var title = TitleHeaders
                .Select(h => metadata.GetValueOrDefault(h))
                .FirstOrDefault(v => !string.IsNullOrWhiteSpace(v))
                ?? $"{Path.GetFileNameWithoutExtension(filePath)} row {rowNumber}";

            var text = string.Join("\n", headers.Select(h => $"{h}: {metadata[h]}"));
            records.Add(new ExtractedRecord(title, text, metadata));
            rowNumber++;
        }

        return records;
    }
}
