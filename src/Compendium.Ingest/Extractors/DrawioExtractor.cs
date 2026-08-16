using System.IO.Compression;
using System.Net;
using System.Xml.Linq;

namespace Compendium.Ingest.Extractors;

// One record per <diagram> (page/tab) in the .drawio <mxfile>. Diagram
// content is either raw mxGraphModel XML (files saved uncompressed — common
// for diagrams kept diffable in git) or, by default, deflate+base64+
// URI-encoded XML, which draw.io's own JS pipeline produces via:
// encodeURIComponent(xml) -> raw deflate -> base64. Decoding reverses that.
public sealed class DrawioExtractor : IDocumentExtractor
{
    public IReadOnlyList<ExtractedRecord> Extract(string filePath)
    {
        var doc = XDocument.Load(filePath);
        var records = new List<ExtractedRecord>();

        foreach (var diagram in doc.Descendants("diagram"))
        {
            try
            {
                records.Add(ToRecord(diagram));
            }
            catch
            {
                // Skip this one page; the rest of the file still ingests.
            }
        }

        return records;
    }

    private static ExtractedRecord ToRecord(XElement diagram)
    {
        var root = ParseGraphModel(diagram).Descendants("root").Single();

        var labelById = root.Elements("mxCell")
            .Where(c => (string?)c.Attribute("id") is not null)
            .ToDictionary(c => (string)c.Attribute("id")!, c => DecodeLabel(c.Attribute("value")?.Value));

        var shapes = root.Elements("mxCell")
            .Where(c => (string?)c.Attribute("vertex") == "1")
            .Select(c => DecodeLabel(c.Attribute("value")?.Value))
            .Where(label => !string.IsNullOrWhiteSpace(label))
            .Select(label => label!)
            .ToList();

        var connections = root.Elements("mxCell")
            .Where(c => (string?)c.Attribute("edge") == "1")
            .Select(c => (
                From: ResolveLabel(labelById, (string?)c.Attribute("source")),
                To: ResolveLabel(labelById, (string?)c.Attribute("target")),
                Label: (string?)DecodeLabel(c.Attribute("value")?.Value)))
            .Where(edge => edge.From is not null && edge.To is not null)
            .Select(edge => (edge.From!, edge.To!, edge.Label))
            .ToList();

        var text = DiagramText.Render(shapes, connections);

        var metadata = new Dictionary<string, string>
        {
            ["shape_count"] = shapes.Count.ToString(),
            ["connector_count"] = connections.Count.ToString(),
        };

        var title = (string?)diagram.Attribute("name");
        title = string.IsNullOrWhiteSpace(title) ? "(untitled page)" : title;
        return new ExtractedRecord(title, text, metadata);
    }

    private static string? ResolveLabel(Dictionary<string, string?> labelById, string? cellId)
    {
        if (cellId is null || !labelById.TryGetValue(cellId, out var label))
        {
            return null;
        }

        return string.IsNullOrWhiteSpace(label) ? cellId : label;
    }

    private static string? DecodeLabel(string? value)
    {
        return value is null ? null : WebUtility.HtmlDecode(value).Trim();
    }

    private static XElement ParseGraphModel(XElement diagram)
    {
        // Uncompressed diagrams nest <mxGraphModel> as a real child element;
        // compressed ones store it as base64 text content — XElement.Value
        // only ever sees the latter, so check for child elements first.
        var inline = diagram.Elements().FirstOrDefault();
        if (inline is not null)
        {
            return inline;
        }

        return XElement.Parse(Decompress(diagram.Value.Trim()));
    }

    private static string Decompress(string base64)
    {
        var compressed = Convert.FromBase64String(base64);
        using var input = new MemoryStream(compressed);
        using var deflate = new DeflateStream(input, CompressionMode.Decompress);
        using var output = new MemoryStream();
        deflate.CopyTo(output);

        var uriEncoded = System.Text.Encoding.UTF8.GetString(output.ToArray());
        return Uri.UnescapeDataString(uriEncoded);
    }
}
