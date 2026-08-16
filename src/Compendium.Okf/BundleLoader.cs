namespace Compendium.Okf;

// Loads a directory tree of concept documents into a Bundle per SPEC.md §3.
public static class BundleLoader
{
    private static readonly HashSet<string> ReservedFileNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "index.md",
        "log.md",
    };

    public static Bundle LoadBundle(string rootPath)
    {
        var root = Path.GetFullPath(rootPath);
        var concepts = new Dictionary<string, Concept>();

        foreach (var filePath in Directory.EnumerateFiles(root, "*.md", SearchOption.AllDirectories))
        {
            if (ReservedFileNames.Contains(Path.GetFileName(filePath)))
            {
                continue;
            }

            var relativePath = Path.GetRelativePath(root, filePath).Replace('\\', '/');
            var id = relativePath[..^".md".Length];

            var rawText = File.ReadAllText(filePath);
            var (frontmatter, body) = OkfParser.ParseConcept(rawText);

            concepts[id] = new Concept
            {
                Id = id,
                RawText = rawText,
                Frontmatter = frontmatter,
                Body = body,
            };
        }

        return new Bundle { RootPath = root, Concepts = concepts };
    }
}
