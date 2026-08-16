using ClosedXML.Excel;

namespace Compendium.Ingest.Extractors;

// Wrapper extractor that auto-detects data map files in Excel format and delegates
// to the appropriate extractor:
// - DataMapExtractor if headers match data map pattern (Int Name, Record Type, etc.)
// - XlsxExtractor otherwise (default row-per-concept behavior per worksheet)
public sealed class SmartXlsxExtractor : IDocumentExtractor
{
    private static readonly string[] DataMapHeaders =
        ["Int Name", "Record Type", "SRC Column", "DST Column"];

    private readonly DataMapExtractor _dataMapExtractor = new();
    private readonly XlsxExtractor _xlsxExtractor = new();

    public IReadOnlyList<ExtractedRecord> Extract(string filePath)
    {
        if (IsDataMapFile(filePath))
        {
            // Convert XLSX to temp CSV for DataMapExtractor (which expects CSV/CsvReader)
            // Note: This is a simplification. In production, DataMapExtractor should be
            // refactored to accept a generic row reader interface.
            var tempCsv = ConvertFirstSheetToCsv(filePath);
            try
            {
                return _dataMapExtractor.Extract(tempCsv);
            }
            finally
            {
                if (File.Exists(tempCsv))
                {
                    File.Delete(tempCsv);
                }
            }
        }

        return _xlsxExtractor.Extract(filePath);
    }

    private static bool IsDataMapFile(string filePath)
    {
        try
        {
            using var workbook = new XLWorkbook(filePath);
            var firstSheet = workbook.Worksheets.FirstOrDefault();

            if (firstSheet is null)
                return false;

            var rows = firstSheet.RangeUsed()?.RowsUsed().ToList();
            if (rows is null || rows.Count < 2)
                return false;

            var headers = rows[0].Cells().Select(c => c.GetString()).ToArray();

            return DataMapHeaders.All(required =>
                headers.Any(h => string.Equals(h, required, StringComparison.OrdinalIgnoreCase)));
        }
        catch
        {
            return false;
        }
    }

    private static string ConvertFirstSheetToCsv(string xlsxPath)
    {
        using var workbook = new XLWorkbook(xlsxPath);
        var firstSheet = workbook.Worksheets.First();

        var tempCsv = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.csv");

        using var writer = new StreamWriter(tempCsv);
        var rows = firstSheet.RangeUsed()?.RowsUsed();

        if (rows is not null)
        {
            foreach (var row in rows)
            {
                var cells = row.Cells().Select(c =>
                {
                    var value = c.GetString();
                    // Escape values containing commas or quotes
                    if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
                    {
                        return $"\"{value.Replace("\"", "\"\"")}\"";
                    }
                    return value;
                });

                writer.WriteLine(string.Join(",", cells));
            }
        }

        return tempCsv;
    }
}
