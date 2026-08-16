using System.Text.Json;

namespace Compendium.Ingest.Extractors;

// An array root ("[{...}, {...}]") is treated like tabular data — one
// record per element. Anything else is a single record for the whole file.
public sealed class JsonExtractor : IDocumentExtractor
{
    private static readonly string[] TitleKeys = ["title", "name", "id", "Title", "Name", "Id"];

    public IReadOnlyList<ExtractedRecord> Extract(string filePath)
    {
        var raw = File.ReadAllText(filePath);
        using var doc = JsonDocument.Parse(raw);
        var fileTitle = Path.GetFileNameWithoutExtension(filePath);

        if (doc.RootElement.ValueKind == JsonValueKind.Array)
        {
            var records = new List<ExtractedRecord>();
            var index = 1;
            foreach (var element in doc.RootElement.EnumerateArray())
            {
                records.Add(BuildRecord(element, $"{fileTitle} #{index}"));
                index++;
            }

            return records;
        }

        return new[] { BuildRecord(doc.RootElement, fileTitle) };
    }

    private static ExtractedRecord BuildRecord(JsonElement element, string fallbackTitle)
    {
        var metadata = new Dictionary<string, string>();
        string? title = null;

        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (property.Value.ValueKind is JsonValueKind.Object or JsonValueKind.Array)
                {
                    continue;
                }

                var value = property.Value.ValueKind switch
                {
                    JsonValueKind.String => property.Value.GetString() ?? "",
                    JsonValueKind.Null => "",
                    _ => property.Value.ToString(),
                };

                metadata[property.Name] = value;

                if (title is null && TitleKeys.Contains(property.Name))
                {
                    title = value;
                }
            }
        }

        var text = JsonSerializer.Serialize(element, new JsonSerializerOptions { WriteIndented = true });
        return new ExtractedRecord(title ?? fallbackTitle, text, metadata);
    }
}
