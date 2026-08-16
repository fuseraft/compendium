using System.Xml.Linq;

namespace Compendium.Ingest.Extractors;

// One record per ArchiMate element (Application Component, Business Actor,
// Node, ...) — unlike drawio/vsdx shapes, these are semantically typed
// nodes with real relationships, so each one is a standalone concept in
// its own right, not just a shape in a diagram. Relationships and views
// don't get their own records: relationships are folded into whichever
// elements they connect, and views are pure visual layout with no model
// content of their own.
public sealed class ArchimateExtractor : IDocumentExtractor
{
    private sealed record ConceptElement(string Name, string ArchimateType, string? Layer);

    private sealed record Relationship(string SourceId, string TargetId, string Label);

    public IReadOnlyList<ExtractedRecord> Extract(string filePath)
    {
        var doc = XDocument.Load(filePath);

        var diagramElements = new HashSet<XElement>(
            doc.Descendants("folder")
                .Where(f => (string?)f.Attribute("type") == "diagrams")
                .SelectMany(f => f.Descendants("element")));

        var candidates = doc.Descendants("element")
            .Where(e => !diagramElements.Contains(e))
            .ToList();

        var concepts = new Dictionary<string, ConceptElement>();
        var relationships = new List<Relationship>();

        foreach (var candidate in candidates)
        {
            var id = (string?)candidate.Attribute("id");
            if (id is null)
            {
                continue;
            }

            var source = (string?)candidate.Attribute("source");
            var target = (string?)candidate.Attribute("target");

            if (source is not null && target is not null)
            {
                var label = (string?)candidate.Attribute("name");
                relationships.Add(new Relationship(source, target, string.IsNullOrWhiteSpace(label) ? TypeName(candidate) : label));
            }
            else
            {
                var name = (string?)candidate.Attribute("name");
                if (!string.IsNullOrWhiteSpace(name))
                {
                    concepts[id] = new ConceptElement(name, TypeName(candidate), Layer(candidate));
                }
            }
        }

        var records = new List<ExtractedRecord>();
        foreach (var (id, concept) in concepts)
        {
            try
            {
                records.Add(ToRecord(id, concept, concepts, relationships));
            }
            catch
            {
                // Skip this one element; the rest of the model still ingests.
            }
        }

        return records;
    }

    private static ExtractedRecord ToRecord(
        string id,
        ConceptElement concept,
        Dictionary<string, ConceptElement> concepts,
        List<Relationship> relationships)
    {
        var lines = relationships
            .Where(r => r.SourceId == id || r.TargetId == id)
            .Select(r => $"- {ResolveName(concepts, r.SourceId)} -> {ResolveName(concepts, r.TargetId)} ({r.Label})")
            .ToList();

        var layerSuffix = concept.Layer is null ? "" : $" (layer: {concept.Layer})";
        var text = $"Type: {concept.ArchimateType}{layerSuffix}";
        if (lines.Count > 0)
        {
            text += "\n\nRelationships:\n" + string.Join('\n', lines);
        }

        var metadata = new Dictionary<string, string>
        {
            ["archimate_type"] = concept.ArchimateType,
            ["relationship_count"] = lines.Count.ToString(),
        };
        if (concept.Layer is not null)
        {
            metadata["layer"] = concept.Layer;
        }

        return new ExtractedRecord(concept.Name, text, metadata);
    }

    private static string ResolveName(Dictionary<string, ConceptElement> concepts, string id)
    {
        return concepts.TryGetValue(id, out var concept) ? concept.Name : id;
    }

    private static string? Layer(XElement element)
    {
        var folder = element.Ancestors("folder").FirstOrDefault(f => f.Attribute("type") is not null);
        return (string?)folder?.Attribute("type");
    }

    private static string TypeName(XElement element)
    {
        var raw = element.Attributes().FirstOrDefault(a => a.Name.LocalName == "type")?.Value ?? "Unknown";
        var colon = raw.IndexOf(':');
        var type = colon >= 0 ? raw[(colon + 1)..] : raw;
        return type.EndsWith("Relationship") ? type[..^"Relationship".Length] : type;
    }
}
