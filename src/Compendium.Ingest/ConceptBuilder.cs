using System.Text;
using System.Text.RegularExpressions;

namespace Compendium.Ingest;

// Renders an ExtractedRecord as OKF concept markdown per SPEC.md — same
// frontmatter shape (provenance via `sources`, trust via `generated`,
// lifecycle via `status`) established by catalog/sample and catalog/intcat.
public static class ConceptBuilder
{
    // Frontmatter fields ConceptBuilder itself owns for trust/provenance —
    // never let source metadata collide with these keys, since YAML
    // deserializers (YamlDotNet included) resolve duplicate keys by taking
    // the last one silently, letting untrusted source content overwrite
    // status/sources/generated/etc.
    private static readonly HashSet<string> ReservedKeys = new(StringComparer.Ordinal)
    {
        "type", "title", "description", "tags", "status", "generated", "sources",
    };

    public static string Build(ExtractedRecord record, ConceptOptions options)
    {
        var description = Summarize(record.Text);

        var sb = new StringBuilder();
        sb.AppendLine("---");
        sb.AppendLine($"type: {options.Type}");
        sb.AppendLine($"title: {YamlString(record.Title)}");
        sb.AppendLine($"description: {YamlString(description)}");
        sb.AppendLine($"tags: [imported, {options.Format}]");
        sb.AppendLine("status: draft");
        sb.AppendLine(
            $"generated: {{ by: process:compendium-ingest/0.1, at: {options.GeneratedAtUtc:yyyy-MM-ddTHH:mm:ssZ} }}");
        sb.AppendLine("sources:");
        sb.AppendLine("  - id: ingest");
        sb.AppendLine($"    resource: {options.SourceResourcePath}");
        sb.AppendLine($"    title: {YamlString(options.SourceTitle)}");

        foreach (var (key, value) in record.Metadata)
        {
            sb.AppendLine($"{SanitizeKey(key)}: {YamlString(value)}");
        }

        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine("# Overview");
        sb.AppendLine();
        sb.AppendLine(record.Text);

        if (record.Metadata.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("# Details");
            sb.AppendLine();
            foreach (var (key, value) in record.Metadata)
            {
                sb.AppendLine($"- **{key}:** {value}");
            }
        }

        return sb.ToString();
    }

    private static string Summarize(string text)
    {
        var clean = Regex.Replace(text, @"\s+", " ").Trim();
        if (clean.Length <= 240)
        {
            return clean;
        }

        var match = Regex.Match(clean, @"^.*?[.!?](\s|$)");
        if (match.Success && match.Length > 20)
        {
            return match.Value.Trim();
        }

        return clean[..240] + "...";
    }

    private static string YamlString(string s)
    {
        var clean = Regex.Replace(s, @"\s+", " ").Trim();
        var escaped = clean.Replace("\\", "\\\\").Replace("\"", "\\\"");
        return $"\"{escaped}\"";
    }

    private static string SanitizeKey(string key)
    {
        var lowered = key.ToLowerInvariant();
        var sanitized = Regex.Replace(lowered, "[^a-z0-9]+", "_").Trim('_');
        sanitized = string.IsNullOrEmpty(sanitized) ? "field" : sanitized;

        // Source metadata must never shadow the trust/provenance fields
        // above — a duplicate YAML key would silently overwrite them.
        return ReservedKeys.Contains(sanitized) ? $"source_{sanitized}" : sanitized;
    }
}
