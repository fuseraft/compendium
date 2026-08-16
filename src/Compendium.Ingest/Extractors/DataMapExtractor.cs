using System.Globalization;
using System.Text;
using CsvHelper;

namespace Compendium.Ingest.Extractors;

// Specialized extractor for data map files that document field-level data lineage.
// Groups rows by "Int Name" (integration/batch job name) to create one concept per
// integration instead of one concept per row.
//
// Expected columns: Int Name, Record Type, SRC DB, SRC Schema, SRC Table, SRC Column,
// DST DB, DST Schema, DST Table, DST Column, Details
//
// Record types:
// - "1-High-Level Overview": describes what the integration does
// - "2-Field Mapping": source-to-destination field mapping
public sealed class DataMapExtractor : IDocumentExtractor
{
    private static readonly string[] RequiredHeaders =
        ["Int Name", "Record Type", "SRC Column", "DST Column"];

    private static readonly string[] AllExpectedHeaders =
    [
        "Int Name", "Record Type",
        "SRC DB", "SRC Schema", "SRC Table", "SRC Column",
        "DST DB", "DST Schema", "DST Table", "DST Column",
        "Details"
    ];

    public IReadOnlyList<ExtractedRecord> Extract(string filePath)
    {
        using var reader = new StreamReader(filePath);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

        csv.Read();
        csv.ReadHeader();
        var headers = csv.HeaderRecord ?? [];

        // Verify this is a data map file
        if (!IsDataMapFile(headers))
        {
            return [];
        }

        // Group rows by integration name
        var integrations = new Dictionary<string, IntegrationData>();

        while (csv.Read())
        {
            var intName = csv.GetField("Int Name")?.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(intName))
                continue;

            if (!integrations.TryGetValue(intName, out var data))
            {
                data = new IntegrationData(intName);
                integrations[intName] = data;
            }

            var recordType = csv.GetField("Record Type")?.Trim() ?? "";

            if (recordType == "1-High-Level Overview")
            {
                data.Overview = csv.GetField("Details")?.Trim() ?? "";
            }
            else if (recordType == "2-Field Mapping")
            {
                data.Mappings.Add(new FieldMapping(
                    SrcDb: GetFieldOrNA(csv, "SRC DB"),
                    SrcSchema: GetFieldOrNA(csv, "SRC Schema"),
                    SrcTable: GetFieldOrNA(csv, "SRC Table"),
                    SrcColumn: GetFieldOrNA(csv, "SRC Column"),
                    DstDb: GetFieldOrNA(csv, "DST DB"),
                    DstSchema: GetFieldOrNA(csv, "DST Schema"),
                    DstTable: GetFieldOrNA(csv, "DST Table"),
                    DstColumn: GetFieldOrNA(csv, "DST Column"),
                    Details: csv.GetField("Details")?.Trim() ?? ""
                ));
            }
        }

        // Convert each integration to an ExtractedRecord
        var records = new List<ExtractedRecord>();
        foreach (var (intName, data) in integrations)
        {
            var text = FormatIntegrationAsMarkdown(data);
            var metadata = BuildMetadata(data);
            records.Add(new ExtractedRecord(intName, text, metadata));
        }

        return records;
    }

    private static bool IsDataMapFile(string[] headers)
    {
        return RequiredHeaders.All(required =>
            headers.Any(h => string.Equals(h, required, StringComparison.OrdinalIgnoreCase)));
    }

    private static string GetFieldOrNA(CsvReader csv, string header)
    {
        var value = csv.GetField(header)?.Trim() ?? "";
        return string.IsNullOrWhiteSpace(value) || value.Equals("N/A", StringComparison.OrdinalIgnoreCase)
            ? "N/A"
            : value;
    }

    private static string FormatIntegrationAsMarkdown(IntegrationData data)
    {
        var sb = new StringBuilder();

        // Overview - ConceptBuilder will add the "# Overview" heading
        if (!string.IsNullOrWhiteSpace(data.Overview))
        {
            sb.AppendLine(data.Overview);
            sb.AppendLine();
        }

        // Field Mappings table
        if (data.Mappings.Any())
        {
            sb.AppendLine("**Field Mappings:**");
            sb.AppendLine();
            sb.AppendLine("| Source | Destination | Details |");
            sb.AppendLine("|--------|-------------|---------|");

            foreach (var mapping in data.Mappings)
            {
                var source = FormatLocation(mapping.SrcDb, mapping.SrcSchema, mapping.SrcTable, mapping.SrcColumn);
                var destination = FormatLocation(mapping.DstDb, mapping.DstSchema, mapping.DstTable, mapping.DstColumn);
                var details = mapping.Details.Replace("|", "\\|").Replace("\n", " ");

                sb.AppendLine($"| {source} | {destination} | {details} |");
            }
            sb.AppendLine();
        }

        // Source Systems
        var sourceSystems = data.Mappings
            .Select(m => m.SrcDb)
            .Where(db => db != "N/A")
            .Distinct()
            .OrderBy(db => db);

        if (sourceSystems.Any())
        {
            sb.AppendLine("**Source Systems:**");
            sb.AppendLine();
            foreach (var system in sourceSystems)
            {
                sb.AppendLine($"- {system}");
            }
            sb.AppendLine();
        }

        // Destination Systems
        var destSystems = data.Mappings
            .Select(m => CategorizeDestination(m))
            .Distinct()
            .OrderBy(d => d);

        if (destSystems.Any())
        {
            sb.AppendLine("**Destination Systems:**");
            sb.AppendLine();
            foreach (var system in destSystems)
            {
                sb.AppendLine($"- {system}");
            }
        }

        return sb.ToString().Trim();
    }

    private static string FormatLocation(string db, string schema, string table, string column)
    {
        var parts = new List<string>();

        if (db != "N/A") parts.Add(db);
        if (schema != "N/A") parts.Add(schema);
        if (table != "N/A") parts.Add(table);
        if (column != "N/A") parts.Add(column);

        return parts.Any() ? string.Join(".", parts) : "N/A";
    }

    private static string CategorizeDestination(FieldMapping mapping)
    {
        // If DST DB is specified and not N/A, it's a database destination
        if (mapping.DstDb != "N/A")
        {
            return mapping.DstDb;
        }

        // Otherwise, infer from context
        var details = mapping.Details.ToLowerInvariant();
        var dstColumn = mapping.DstColumn.ToLowerInvariant();

        if (details.Contains("csv") || dstColumn.Contains("csv column"))
            return "File (CSV)";

        if (details.Contains("email") || dstColumn.Contains("email"))
            return "Email";

        if (details.Contains("sftp") || details.Contains("ftp"))
            return "File (SFTP)";

        if (details.Contains("api") || details.Contains("rest") || details.Contains("web service"))
            return "API";

        if (details.Contains("report"))
            return "Report";

        if (details.Contains("sharepoint"))
            return "SharePoint";

        // Default to generic external/file destination
        return mapping.DstColumn != "N/A" ? "File/External" : "N/A";
    }

    private static IReadOnlyDictionary<string, string> BuildMetadata(IntegrationData data)
    {
        var metadata = new Dictionary<string, string>();

        // Source systems
        var sourceSystems = data.Mappings
            .Select(m => m.SrcDb)
            .Where(db => db != "N/A")
            .Distinct()
            .OrderBy(db => db)
            .ToArray();

        if (sourceSystems.Any())
        {
            metadata["source_systems"] = string.Join(", ", sourceSystems);
        }

        // Destination systems
        var destSystems = data.Mappings
            .Select(m => CategorizeDestination(m))
            .Where(d => d != "N/A")
            .Distinct()
            .OrderBy(d => d)
            .ToArray();

        if (destSystems.Any())
        {
            metadata["destination_systems"] = string.Join(", ", destSystems);
        }

        // Destination types (for agent queries)
        var destTypes = data.Mappings
            .Select(m => CategorizeDestinationType(m))
            .Distinct()
            .OrderBy(t => t)
            .ToArray();

        if (destTypes.Any())
        {
            metadata["destination_types"] = string.Join(", ", destTypes);
        }

        // Field count
        metadata["field_count"] = data.Mappings.Count.ToString();

        return metadata;
    }

    private static string CategorizeDestinationType(FieldMapping mapping)
    {
        if (mapping.DstDb != "N/A")
            return "Database";

        var details = mapping.Details.ToLowerInvariant();
        var dstColumn = mapping.DstColumn.ToLowerInvariant();

        if (details.Contains("csv") || dstColumn.Contains("csv"))
            return "File";

        if (details.Contains("email"))
            return "Email";

        if (details.Contains("sftp") || details.Contains("ftp"))
            return "File";

        if (details.Contains("api") || details.Contains("rest") || details.Contains("web service"))
            return "API";

        if (details.Contains("report"))
            return "Report";

        if (details.Contains("sharepoint"))
            return "SharePoint";

        return "External";
    }

    private sealed class IntegrationData
    {
        public string Name { get; }
        public string Overview { get; set; } = "";
        public List<FieldMapping> Mappings { get; } = new();

        public IntegrationData(string name)
        {
            Name = name;
        }
    }

    private sealed record FieldMapping(
        string SrcDb,
        string SrcSchema,
        string SrcTable,
        string SrcColumn,
        string DstDb,
        string DstSchema,
        string DstTable,
        string DstColumn,
        string Details);
}
