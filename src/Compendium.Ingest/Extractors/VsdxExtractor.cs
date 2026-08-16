using OfficeIMO.Visio;

namespace Compendium.Ingest.Extractors;

// One record per page — a Visio page is a diagram, not a table of rows, so
// unlike CsvExtractor/XlsxExtractor the record is the whole graph, not a row.
public sealed class VsdxExtractor : IDocumentExtractor
{
    public IReadOnlyList<ExtractedRecord> Extract(string filePath)
    {
        var document = VisioDocument.Load(filePath);
        var records = new List<ExtractedRecord>();

        foreach (var page in document.Pages)
        {
            try
            {
                records.Add(ToRecord(page));
            }
            catch
            {
                // Skip this one page; the rest of the document still ingests.
            }
        }

        return records;
    }

    private static ExtractedRecord ToRecord(VisioPage page)
    {
        var shapes = page.Shapes.Where(s => !string.IsNullOrWhiteSpace(s.Text)).ToList();
        var connectors = page.Connectors.Where(c => c.From is not null && c.To is not null).ToList();

        var text = DiagramText.Render(
            shapes.Select(s => s.Text!),
            connectors.Select(c => (From: c.From!.Text!, To: c.To!.Text!, c.Label)));

        var metadata = new Dictionary<string, string>
        {
            ["shape_count"] = shapes.Count.ToString(),
            ["connector_count"] = connectors.Count.ToString(),
        };

        var title = string.IsNullOrWhiteSpace(page.Name) ? "(untitled page)" : page.Name;
        return new ExtractedRecord(title, text, metadata);
    }
}
