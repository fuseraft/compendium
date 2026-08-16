using Compendium.Ingest.Extractors;

namespace Compendium.Ingest;

public sealed record IngestionResult(
    int FilesProcessed,
    int ConceptsWritten,
    IReadOnlyList<string> SkippedFiles,
    IReadOnlyList<(string File, string Error)> FailedFiles);

// Walks a file or directory, dispatches each file to the extractor for its
// extension, mirrors the original into <bundle>/references/ (SPEC.md §6.3),
// and writes one OKF concept per ExtractedRecord. One bad file never aborts
// the run — it's recorded in FailedFiles and ingestion continues.
public sealed class IngestionPipeline
{
    // Extractors for archive-based formats (xlsx/docx/vsdx are zip; pdf has
    // its own compressed streams) fully materialize a file in memory with no
    // size cap of their own — this bounds exposure to a decompression-bomb
    // file pulled in from an untrusted connector source.
    private const long MaxFileSizeBytes = 200 * 1024 * 1024;

    private static readonly Dictionary<string, (IDocumentExtractor Extractor, string Format)> ExtractorsByExtension =
        new(StringComparer.OrdinalIgnoreCase)
        {
            [".txt"] = (new TxtExtractor(), "txt"),
            [".md"] = (new MarkdownExtractor(), "markdown"),
            [".json"] = (new JsonExtractor(), "json"),
            [".xml"] = (new XmlExtractor(), "xml"),
            [".csv"] = (new CsvExtractor(), "csv"),
            [".pdf"] = (new PdfExtractor(), "pdf"),
            [".docx"] = (new DocxExtractor(), "docx"),
            [".xlsx"] = (new XlsxExtractor(), "xlsx"),
            [".eml"] = (new EmlExtractor(), "eml"),
            [".msg"] = (new MsgExtractor(), "msg"),
            [".ost"] = (new OstExtractor(), "ost"),
            [".drawio"] = (new DrawioExtractor(), "drawio"),
            [".vsdx"] = (new VsdxExtractor(), "vsdx"),
            [".archimate"] = (new ArchimateExtractor(), "archimate"),
        };

    public IngestionResult Ingest(string sourcePath, string bundleRoot, string conceptType = "Document")
    {
        var files = File.Exists(sourcePath)
            ? new[] { sourcePath }
            : Directory.EnumerateFiles(sourcePath, "*", SearchOption.AllDirectories).ToArray();

        var referencesDir = Path.Combine(bundleRoot, "references");
        var conceptsDir = Path.Combine(bundleRoot, Pluralize(conceptType));
        Directory.CreateDirectory(referencesDir);
        Directory.CreateDirectory(conceptsDir);

        var usedReferenceNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var usedConceptSlugs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var skipped = new List<string>();
        var failed = new List<(string, string)>();
        var processed = 0;
        var written = 0;

        foreach (var file in files)
        {
            var ext = Path.GetExtension(file);
            if (!ExtractorsByExtension.TryGetValue(ext, out var entry))
            {
                skipped.Add(file);
                continue;
            }

            processed++;

            var fileSize = new FileInfo(file).Length;
            if (fileSize > MaxFileSizeBytes)
            {
                failed.Add((file, $"File is {fileSize / (1024 * 1024)} MB, exceeding the {MaxFileSizeBytes / (1024 * 1024)} MB ingest limit"));
                continue;
            }

            try
            {
                var records = entry.Extractor.Extract(file);

                string? sharedMirrorName = null;
                if (records.Any(r => r.MirrorText is null))
                {
                    sharedMirrorName = Slug.Unique(Path.GetFileNameWithoutExtension(file), usedReferenceNames) + ext;
                    File.Copy(file, Path.Combine(referencesDir, sharedMirrorName), overwrite: true);
                }

                foreach (var record in records)
                {
                    var slug = Slug.Unique(record.Title, usedConceptSlugs);

                    string resourcePath;
                    string sourceTitle;
                    if (record.MirrorText is not null)
                    {
                        var mirroredName = Slug.Unique(slug, usedReferenceNames) + ".txt";
                        File.WriteAllText(Path.Combine(referencesDir, mirroredName), record.MirrorText);
                        resourcePath = $"/references/{mirroredName}";
                        sourceTitle = mirroredName;
                    }
                    else
                    {
                        resourcePath = $"/references/{sharedMirrorName}";
                        sourceTitle = Path.GetFileName(file);
                    }

                    var options = new ConceptOptions(
                        Type: conceptType,
                        Format: entry.Format,
                        SourceResourcePath: resourcePath,
                        SourceTitle: sourceTitle,
                        GeneratedAtUtc: DateTime.UtcNow);

                    var markdown = ConceptBuilder.Build(record, options);
                    File.WriteAllText(Path.Combine(conceptsDir, $"{slug}.md"), markdown);
                    written++;
                }
            }
            catch (Exception ex)
            {
                failed.Add((file, ex.Message));
            }
        }

        return new IngestionResult(processed, written, skipped, failed);
    }

    private static string Pluralize(string type)
    {
        var lower = type.ToLowerInvariant().Replace(' ', '-');
        return lower.EndsWith('s') ? lower : lower + "s";
    }
}
