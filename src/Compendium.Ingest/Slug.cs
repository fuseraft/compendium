using System.Text.RegularExpressions;

namespace Compendium.Ingest;

public static class Slug
{
    public static string Of(string value)
    {
        var lowered = value.ToLowerInvariant();
        var slug = Regex.Replace(lowered, "[^a-z0-9]+", "-").Trim('-');
        return string.IsNullOrEmpty(slug) ? "item" : slug;
    }

    // First-match-wins dedup: "foo", "foo-2", "foo-3", ... — mirrors the
    // scheme used by the hand-written CSV bundle script this generalizes.
    public static string Unique(string value, HashSet<string> used)
    {
        var slug = Of(value);
        var candidate = slug;
        var i = 2;
        while (used.Contains(candidate))
        {
            candidate = $"{slug}-{i}";
            i++;
        }

        used.Add(candidate);
        return candidate;
    }
}
