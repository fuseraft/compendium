namespace Compendium.Okf;

public sealed class Bundle
{
    public required string RootPath { get; init; }
    public required IReadOnlyDictionary<string, Concept> Concepts { get; init; }
}
