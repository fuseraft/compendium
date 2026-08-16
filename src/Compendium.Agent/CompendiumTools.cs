using System.ComponentModel;
using Compendium.Okf;

namespace Compendium.Agent;

// Grounds the system agent in a loaded OKF bundle. Each public method
// becomes an AIFunction tool via AIFunctionFactory.Create in CompendiumAgentFactory.
// Which tools are actually registered (read-only vs. read+write) is decided
// there, not here — every method on this class always exists and is
// directly unit-testable.
public sealed class CompendiumTools
{
    private const string Actor = "agent:compendium-agent/0.1";

    private Bundle _bundle;

    public CompendiumTools(Bundle bundle)
    {
        _bundle = bundle;
    }

    [Description("List concept ids in the knowledge bundle, optionally filtered by concept type.")]
    public string ListConcepts([Description("Optional concept type to filter by, e.g. 'System'.")] string? type = null)
    {
        var matches = _bundle.Concepts.Values
            .Where(c => type is null || string.Equals(c.Type, type, StringComparison.OrdinalIgnoreCase))
            .OrderBy(c => c.Id, StringComparer.Ordinal)
            .Select(c => $"{c.Id}: {c.Title} (status: {c.Status})");

        var result = string.Join('\n', matches);
        return result.Length == 0 ? "No concepts found." : result;
    }

    [Description("Read the full raw contents (frontmatter and body) of a concept by its id.")]
    public string ReadConcept([Description("The concept id, e.g. 'systems/billing-service'.")] string id)
    {
        return _bundle.Concepts.TryGetValue(id, out var concept)
            ? concept.RawText
            : $"No concept with id '{id}'.";
    }

    [Description("Search concept titles, descriptions, and bodies for a text query and return matching concept ids.")]
    public string SearchConcepts([Description("Text to search for, case-insensitive.")] string query)
    {
        var matches = _bundle.Concepts.Values
            .Where(c =>
                c.Body.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                c.Title.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                c.Description.Contains(query, StringComparison.OrdinalIgnoreCase))
            .OrderBy(c => c.Id, StringComparer.Ordinal)
            .Select(c => c.Id);

        var result = string.Join('\n', matches);
        return result.Length == 0 ? $"No concepts matched '{query}'." : result;
    }

    [Description("""
        Create a new concept in the knowledge bundle. Always saved as
        status: draft and attributed to this agent — you cannot mark a
        concept stable or verified; only a human can do that.
        """)]
    public string CreateConcept(
        [Description("Concept type, e.g. 'System', 'Process', 'Integration'.")] string type,
        [Description("Concept title.")] string title,
        [Description("One-sentence description of the concept.")] string description,
        [Description("Markdown body, e.g. starting with '# Overview'.")] string body,
        [Description("Optional extra tags beyond the automatic 'agent-authored' tag.")] string[]? tags = null)
    {
        var result = ConceptWriter.Create(_bundle.RootPath, type, title, description, body, tags, Actor, DateTime.UtcNow);
        Reload(result);
        return result.Message;
    }

    [Description("""
        Replace the markdown body of an existing concept. Resets its status
        back to draft (a body change needs human re-review) and attributes
        the change to this agent.
        """)]
    public string UpdateConceptBody(
        [Description("The concept id to update, e.g. 'systems/billing-service'.")] string id,
        [Description("The new markdown body to replace the concept's current body with.")] string body)
    {
        var result = ConceptWriter.UpdateBody(_bundle.RootPath, id, body, Actor, DateTime.UtcNow);
        Reload(result);
        return result.Message;
    }

    [Description("""
        Add a link from one concept to another under a section heading in
        the source concept's body (creating the heading if it doesn't
        exist). The target concept doesn't need to exist yet. Resets the
        source concept's status back to draft.
        """)]
    public string AddLink(
        [Description("The concept id the link is added to, e.g. 'systems/billing-service'.")] string fromId,
        [Description("The concept id being linked to, e.g. 'systems/inventory-service'.")] string toId,
        [Description("The link's visible text.")] string linkText,
        [Description("The section heading to add the link under, e.g. 'Integrations'.")] string section)
    {
        var result = ConceptWriter.AddLink(_bundle.RootPath, fromId, toId, linkText, section, Actor, DateTime.UtcNow);
        Reload(result);
        return result.Message;
    }

    [Description("""
        Flag a concept for human review (e.g. it looks stale, duplicated, or
        unverified) without modifying the concept itself. Records a note in
        the bundle's log.md.
        """)]
    public string FlagForReview(
        [Description("The concept id to flag, e.g. 'systems/billing-service'.")] string id,
        [Description("Why this concept needs human review.")] string reason)
    {
        var result = ConceptWriter.FlagForReview(_bundle.RootPath, id, reason, Actor, DateTime.UtcNow);
        return result.Message;
    }

    private void Reload(WriteResult result)
    {
        if (result.Success)
        {
            _bundle = BundleLoader.LoadBundle(_bundle.RootPath);
        }
    }
}
