using System.Xml.Linq;

namespace Compendium.Ingest.Extractors;

// A root with multiple same-named children ("<items><item/><item/></items>")
// is treated like tabular data — one record per child. Anything else is a
// single record for the whole document.
public sealed class XmlExtractor : IDocumentExtractor
{
    private static readonly string[] TitleNames = ["title", "name", "id", "Title", "Name", "Id"];

    public IReadOnlyList<ExtractedRecord> Extract(string filePath)
    {
        var doc = XDocument.Load(filePath);
        var fileTitle = Path.GetFileNameWithoutExtension(filePath);
        var root = doc.Root;

        if (root is null)
        {
            return new[] { new ExtractedRecord(fileTitle, doc.ToString(), new Dictionary<string, string>()) };
        }

        var children = root.Elements().ToList();
        var repeating = children.Count > 1 && children.Select(c => c.Name).Distinct().Count() == 1;

        if (repeating)
        {
            var records = new List<ExtractedRecord>();
            var index = 1;
            foreach (var child in children)
            {
                records.Add(BuildRecord(child, $"{fileTitle} #{index}"));
                index++;
            }

            return records;
        }

        return new[] { BuildRecord(root, fileTitle) };
    }

    private static ExtractedRecord BuildRecord(XElement element, string fallbackTitle)
    {
        var metadata = new Dictionary<string, string>();
        string? title = null;

        foreach (var child in element.Elements())
        {
            if (child.HasElements)
            {
                continue;
            }

            var value = child.Value.Trim();
            metadata[child.Name.LocalName] = value;

            if (title is null && TitleNames.Contains(child.Name.LocalName))
            {
                title = value;
            }
        }

        foreach (var attr in element.Attributes())
        {
            metadata[attr.Name.LocalName] = attr.Value;
        }

        return new ExtractedRecord(title ?? fallbackTitle, element.ToString(), metadata);
    }
}
