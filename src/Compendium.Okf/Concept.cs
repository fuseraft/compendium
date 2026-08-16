namespace Compendium.Okf;

public sealed class Concept
{
    public required string Id { get; init; }
    public required string RawText { get; init; }
    public required IReadOnlyDictionary<string, object?> Frontmatter { get; init; }
    public required string Body { get; init; }

    public string Type => Frontmatter.TryGetValue("type", out var value) ? value?.ToString() ?? "" : "";

    public string Title => Frontmatter.TryGetValue("title", out var value) && value is not null
        ? value.ToString()!
        : Id;

    public string Description => Frontmatter.TryGetValue("description", out var value)
        ? value?.ToString() ?? ""
        : "";

    public string Status => Frontmatter.TryGetValue("status", out var value) && value is not null
        ? value.ToString()!
        : "unknown";
}
