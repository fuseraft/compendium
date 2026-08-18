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

    [Description("List the concept types recognized by this bundle's spec (.compendium/config.json), with their directory and description. If the bundle has no spec, says so — any type is allowed.")]
    public string ListConceptTypes()
    {
        var config = BundleConfig.Load(_bundle.RootPath);
        if (config.Types.Count == 0)
        {
            return "This bundle has no .compendium/config.json spec — any concept type is allowed.";
        }

        var lines = config.Types
            .OrderBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase)
            .Select(kv => $"{kv.Key} ({kv.Value.Directory ?? "?"}): {kv.Value.Description}");

        return string.Join('\n', lines);
    }

    [Description("""
        Create a new concept in the knowledge bundle. Always saved as
        status: draft and attributed to this agent — you cannot mark a
        concept stable or verified; only a human can do that. Call
        ListConceptTypes first if you're unsure which types this bundle
        recognizes.
        """)]
    public string CreateConcept(
        [Description("Concept type, e.g. 'System', 'Process', 'Integration'.")] string type,
        [Description("Concept title.")] string title,
        [Description("One-sentence description of the concept.")] string description,
        [Description("Markdown body, e.g. starting with '# Overview'.")] string body,
        [Description("Optional extra tags beyond the automatic 'agent-authored' tag.")] string[]? tags = null)
    {
        var config = BundleConfig.Load(_bundle.RootPath);
        var decision = config.CheckType(type);
        if (decision == TypeDecision.Rejected)
        {
            return $"'{type}' is not a recognized concept type for this bundle. " +
                   $"Allowed types: {config.AllowedTypesSummary()}. Call ListConceptTypes for details.";
        }

        var atUtc = DateTime.UtcNow;
        var result = ConceptWriter.Create(_bundle.RootPath, type, title, description, body, tags, Actor, atUtc);

        if (result.Success && decision == TypeDecision.Proposed)
        {
            ConceptLog.Append(
                _bundle.RootPath,
                $"**New type proposed** — `{type}` is not yet in this bundle's `.compendium/config.json`; review and either add it to the spec or re-type the concept.",
                atUtc);
        }

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

    [Description("Read the contents of a file from the filesystem. Use this to read source code, configuration files, documentation, or any other text files you need to analyze.")]
    public string ReadFile([Description("The absolute or relative path to the file to read.")] string path)
    {
        try
        {
            if (!File.Exists(path))
            {
                return $"File not found: {path}";
            }

            var fileInfo = new FileInfo(path);
            if (fileInfo.Length > 1_000_000) // 1MB limit
            {
                return $"File too large to read (>{fileInfo.Length:N0} bytes): {path}";
            }

            var content = File.ReadAllText(path);
            return $"=== {path} ({fileInfo.Length:N0} bytes) ===\n{content}";
        }
        catch (Exception ex)
        {
            return $"Error reading {path}: {ex.Message}";
        }
    }

    [Description("List files in a directory, optionally filtered by pattern (e.g. '*.sql', '*.cs'). Returns file names with sizes and last modified dates.")]
    public string ListFiles(
        [Description("The directory path to list files from.")] string path,
        [Description("Optional file pattern like '*.sql' or '*.cs'. Use '*' for all files.")] string? pattern = "*",
        [Description("Include subdirectories recursively. Default is false.")] bool recursive = false)
    {
        try
        {
            if (!Directory.Exists(path))
            {
                return $"Directory not found: {path}";
            }

            var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            var files = Directory.GetFiles(path, pattern ?? "*", searchOption)
                .Select(f => new FileInfo(f))
                .OrderBy(f => f.FullName)
                .Take(500); // Limit to 500 files

            var lines = files.Select(f =>
            {
                var relativePath = Path.GetRelativePath(path, f.FullName);
                return $"{relativePath} ({f.Length:N0} bytes, modified {f.LastWriteTime:yyyy-MM-dd})";
            });

            var result = string.Join('\n', lines);
            return result.Length == 0 ? $"No files found matching '{pattern}' in {path}" : result;
        }
        catch (Exception ex)
        {
            return $"Error listing files in {path}: {ex.Message}";
        }
    }

    [Description("Get a recursive directory tree structure showing folders and file counts. Useful for understanding repository organization.")]
    public string ReadDirectoryStructure(
        [Description("The directory path to analyze.")] string path,
        [Description("Maximum depth to traverse. Default is 3 levels.")] int maxDepth = 3)
    {
        try
        {
            if (!Directory.Exists(path))
            {
                return $"Directory not found: {path}";
            }

            var lines = new List<string>();
            lines.Add($"Directory structure of: {path}");
            lines.Add("");

            void TraverseDirectory(string dir, int depth, string indent)
            {
                if (depth > maxDepth) return;

                try
                {
                    var dirInfo = new DirectoryInfo(dir);
                    var subdirs = dirInfo.GetDirectories()
                        .Where(d => !d.Attributes.HasFlag(FileAttributes.System) &&
                                   !d.Attributes.HasFlag(FileAttributes.Hidden) &&
                                   !d.Name.StartsWith('.'))
                        .OrderBy(d => d.Name)
                        .ToList();

                    var files = dirInfo.GetFiles().Length;

                    foreach (var subdir in subdirs)
                    {
                        var subfiles = subdir.GetFiles("*", SearchOption.AllDirectories).Length;
                        lines.Add($"{indent}📁 {subdir.Name}/ ({subfiles} files)");
                        TraverseDirectory(subdir.FullName, depth + 1, indent + "  ");
                    }

                    if (depth == 0 && files > 0)
                    {
                        lines.Add($"{indent}({files} files in root)");
                    }
                }
                catch (UnauthorizedAccessException)
                {
                    lines.Add($"{indent}⚠️ Access denied");
                }
            }

            TraverseDirectory(path, 0, "");
            return string.Join('\n', lines);
        }
        catch (Exception ex)
        {
            return $"Error reading directory structure: {ex.Message}";
        }
    }

    private void Reload(WriteResult result)
    {
        if (result.Success)
        {
            _bundle = BundleLoader.LoadBundle(_bundle.RootPath);
        }
    }
}
