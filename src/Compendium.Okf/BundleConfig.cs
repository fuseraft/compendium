using System.Text.Json;
using System.Text.Json.Serialization;

namespace Compendium.Okf;

public sealed class BundleTypeSpec
{
    public string? Directory { get; init; }
    public string? Description { get; init; }

    [JsonPropertyName("recommended_fields")]
    public IReadOnlyList<string>? RecommendedFields { get; init; }
}

public enum TypeDecision
{
    // Declared in the bundle's spec, or the bundle has no spec at all.
    Known,

    // Not declared, but the spec's `allow_new_types` permits it anyway.
    Proposed,

    // Not declared, and the spec's `allow_new_types` is "closed".
    Rejected,
}

// A bundle's optional `.compendium/config.json` — the concept-type taxonomy
// curation agents are expected to grow the bundle within. This is a
// Compendium extension, not part of OKF SPEC.md: a bundle without this file
// is fully unconstrained, exactly as bundles behaved before this file existed.
public sealed class BundleConfig
{
    public string? Name { get; init; }
    public string? Description { get; init; }
    public IReadOnlyDictionary<string, BundleTypeSpec> Types { get; init; } = new Dictionary<string, BundleTypeSpec>();

    [JsonPropertyName("allow_new_types")]
    public string AllowNewTypes { get; init; } = "open";

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public static readonly BundleConfig Unconstrained = new();

    public static BundleConfig Load(string bundleRoot)
    {
        var path = Path.Combine(bundleRoot, ".compendium", "config.json");
        if (!File.Exists(path))
        {
            return Unconstrained;
        }

        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<BundleConfig>(json, JsonOptions) ?? Unconstrained;
    }

    public bool IsKnownType(string type) =>
        Types.Keys.Any(k => string.Equals(k, type, StringComparison.OrdinalIgnoreCase));

    // A bundle that declares no types at all isn't meaningfully constrained,
    // regardless of what allow_new_types says.
    public TypeDecision CheckType(string type)
    {
        if (Types.Count == 0 || IsKnownType(type))
        {
            return TypeDecision.Known;
        }

        return string.Equals(AllowNewTypes, "closed", StringComparison.OrdinalIgnoreCase)
            ? TypeDecision.Rejected
            : TypeDecision.Proposed;
    }

    public string AllowedTypesSummary() =>
        string.Join(", ", Types.Keys.OrderBy(k => k, StringComparer.OrdinalIgnoreCase));
}
