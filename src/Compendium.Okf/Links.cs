using System.Text.RegularExpressions;

namespace Compendium.Okf;

// Resolves markdown links between concepts per SPEC.md §6. Broken and
// external links are tolerated, not errors (SPEC.md §11).
public static partial class Links
{
    [GeneratedRegex(@"\[[^\]]*\]\(([^)]+)\)", RegexOptions.Compiled)]
    private static partial Regex LinkPattern();

    public static string? ResolveLink(Bundle bundle, string fromConceptId, string href)
    {
        if (href.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            href.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
            href.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var path = href.Split('#')[0].Trim();
        if (path.Length == 0)
        {
            return null;
        }

        string candidate;
        if (path.StartsWith('/'))
        {
            candidate = path[1..];
        }
        else
        {
            var fromDir = fromConceptId.Contains('/')
                ? fromConceptId[..fromConceptId.LastIndexOf('/')]
                : string.Empty;
            candidate = Normalize(fromDir, path);
        }

        if (candidate.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
        {
            candidate = candidate[..^".md".Length];
        }

        return bundle.Concepts.ContainsKey(candidate) ? candidate : null;
    }

    public static IReadOnlyList<string> OutgoingLinks(Bundle bundle, string conceptId)
    {
        if (!bundle.Concepts.TryGetValue(conceptId, out var concept))
        {
            return [];
        }

        var seen = new HashSet<string>();
        var result = new List<string>();

        foreach (Match match in LinkPattern().Matches(concept.Body))
        {
            var target = ResolveLink(bundle, conceptId, match.Groups[1].Value.Trim());
            if (target is not null && seen.Add(target))
            {
                result.Add(target);
            }
        }

        return result;
    }

    private static string Normalize(string baseDir, string relativePath)
    {
        var segments = new List<string>(baseDir.Length == 0 ? [] : baseDir.Split('/'));

        foreach (var segment in relativePath.Split('/'))
        {
            switch (segment)
            {
                case "" or ".":
                    continue;
                case "..":
                    if (segments.Count > 0)
                    {
                        segments.RemoveAt(segments.Count - 1);
                    }

                    continue;
                default:
                    segments.Add(segment);
                    break;
            }
        }

        return string.Join('/', segments);
    }
}
