using Compendium.Okf;

namespace Compendium.Ingest.Extractors;

// If the file already carries OKF-shaped frontmatter, strip it and use its
// title — the body is the meaningful text either way. Anything else
// (a plain README, notes file, etc.) is treated as plain text.
public sealed class MarkdownExtractor : IDocumentExtractor
{
    public IReadOnlyList<ExtractedRecord> Extract(string filePath)
    {
        var raw = File.ReadAllText(filePath);
        var title = Path.GetFileNameWithoutExtension(filePath);
        var text = raw;

        if (raw.TrimStart().StartsWith("---"))
        {
            try
            {
                var (frontmatter, body) = OkfParser.ParseConcept(raw);
                text = body.Trim();
                if (frontmatter.TryGetValue("title", out var fmTitle) && fmTitle is not null)
                {
                    title = fmTitle.ToString()!;
                }
            }
            catch (ParseError)
            {
                // Not actually OKF frontmatter (e.g. a "---" divider) — use the raw file as-is.
            }
        }

        return new[] { new ExtractedRecord(title, text, new Dictionary<string, string>()) };
    }
}
