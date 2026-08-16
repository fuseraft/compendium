namespace Compendium.Ingest;

public sealed record ConceptOptions(
    string Type,
    string Format,
    string SourceResourcePath,
    string SourceTitle,
    DateTime GeneratedAtUtc);
