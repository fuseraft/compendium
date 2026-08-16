using YamlDotNet.Serialization;

namespace Compendium.Okf;

public sealed class ParseError : Exception
{
    public ParseError(string message) : base(message)
    {
    }
}

// Splits a concept document into its YAML frontmatter and markdown body per SPEC.md §4.
public static class OkfParser
{
    private static readonly IDeserializer Deserializer = new DeserializerBuilder().Build();

    public static (IReadOnlyDictionary<string, object?> Frontmatter, string Body) ParseConcept(string text)
    {
        var lines = text.Split('\n');

        if (lines.Length == 0 || lines[0].TrimEnd('\r') != "---")
        {
            throw new ParseError("Concept is missing a YAML frontmatter block.");
        }

        var closingLine = -1;
        for (var i = 1; i < lines.Length; i++)
        {
            if (lines[i].TrimEnd('\r') == "---")
            {
                closingLine = i;
                break;
            }
        }

        if (closingLine < 0)
        {
            throw new ParseError("Concept frontmatter block is not terminated with '---'.");
        }

        var yaml = string.Join('\n', lines[1..closingLine]);
        var body = closingLine + 1 < lines.Length
            ? string.Join('\n', lines[(closingLine + 1)..])
            : string.Empty;

        var frontmatter = string.IsNullOrWhiteSpace(yaml)
            ? new Dictionary<string, object?>()
            : Deserializer.Deserialize<Dictionary<string, object?>>(yaml) ?? new Dictionary<string, object?>();

        return (frontmatter, body);
    }
}
