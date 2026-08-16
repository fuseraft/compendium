using ClosedXML.Excel;

namespace Compendium.Ingest.Extractors;

// One record per row per worksheet — the first used row in each sheet is
// treated as a header row, mirroring CsvExtractor's row-based approach.
public sealed class XlsxExtractor : IDocumentExtractor
{
    private static readonly string[] TitleHeaders = ["Name", "Title", "name", "title"];

    public IReadOnlyList<ExtractedRecord> Extract(string filePath)
    {
        using var workbook = new XLWorkbook(filePath);
        var fileTitle = Path.GetFileNameWithoutExtension(filePath);
        var records = new List<ExtractedRecord>();

        foreach (var worksheet in workbook.Worksheets)
        {
            var rows = worksheet.RangeUsed()?.RowsUsed().ToList();
            if (rows is null || rows.Count < 2)
            {
                continue;
            }

            var headers = rows[0].Cells().Select(c => c.GetString()).ToList();

            var rowNumber = 1;
            foreach (var row in rows.Skip(1))
            {
                var metadata = new Dictionary<string, string>();
                for (var i = 0; i < headers.Count; i++)
                {
                    if (string.IsNullOrWhiteSpace(headers[i]))
                    {
                        continue;
                    }

                    metadata[headers[i]] = row.Cell(i + 1).GetString();
                }

                var title = TitleHeaders
                    .Select(h => metadata.GetValueOrDefault(h))
                    .FirstOrDefault(v => !string.IsNullOrWhiteSpace(v))
                    ?? $"{fileTitle} / {worksheet.Name} row {rowNumber}";

                var text = string.Join("\n", metadata.Select(kv => $"{kv.Key}: {kv.Value}"));
                records.Add(new ExtractedRecord(title, text, metadata));
                rowNumber++;
            }
        }

        return records;
    }
}
