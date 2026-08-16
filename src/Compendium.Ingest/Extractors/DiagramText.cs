using System.Text;

namespace Compendium.Ingest.Extractors;

// Shared rendering for diagram-shaped extractors (VsdxExtractor, DrawioExtractor):
// turns a node/edge graph into readable text so an agent can answer
// "what connects to what" questions grounded in the diagram.
internal static class DiagramText
{
    public static string Render(IEnumerable<string> shapeLabels, IEnumerable<(string From, string To, string? Label)> connections)
    {
        var sb = new StringBuilder();
        sb.Append("Shapes: ").Append(string.Join(", ", shapeLabels));

        var lines = connections
            .Select(c => string.IsNullOrWhiteSpace(c.Label)
                ? $"- {c.From} -> {c.To}"
                : $"- {c.From} -> {c.To} (label: {c.Label})")
            .ToList();

        if (lines.Count > 0)
        {
            sb.Append("\n\nConnections:\n").Append(string.Join('\n', lines));
        }

        return sb.ToString();
    }
}
