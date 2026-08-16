using System.Text;
using System.Text.RegularExpressions;

namespace Compendium.Okf;

public sealed record WriteResult(bool Success, string Message);

// Writes concept files and log.md entries per SPEC.md. Every write this
// class produces lands as `status: draft`, attributed via `generated` — a
// human, not this class, is responsible for ever promoting a concept to
// `stable` or setting the separate `verified` field (SPEC.md §5.2/§5.4).
public static class ConceptWriter
{
    public static WriteResult Create(
        string bundleRoot,
        string type,
        string title,
        string description,
        string body,
        IReadOnlyList<string>? tags,
        string actor,
        DateTime atUtc)
    {
        var baseId = $"{Pluralize(type)}/{Slugify(title)}";
        var id = baseId;
        var i = 2;
        while (true)
        {
            var validation = ValidateId(bundleRoot, id, out var candidatePath);
            if (!validation.Success)
            {
                return validation;
            }

            if (!File.Exists(candidatePath))
            {
                var allTags = new List<string> { "agent-authored" };
                if (tags is not null)
                {
                    allTags.AddRange(tags);
                }

                Directory.CreateDirectory(Path.GetDirectoryName(candidatePath)!);

                var sb = new StringBuilder();
                sb.AppendLine("---");
                sb.AppendLine($"type: {type}");
                sb.AppendLine($"title: {YamlString(title)}");
                sb.AppendLine($"description: {YamlString(description)}");
                sb.AppendLine($"tags: [{string.Join(", ", allTags)}]");
                sb.AppendLine("status: draft");
                sb.AppendLine($"generated: {{ by: {actor}, at: {atUtc:yyyy-MM-ddTHH:mm:ssZ} }}");
                sb.AppendLine("---");
                sb.AppendLine();
                sb.AppendLine(body.TrimEnd('\n'));

                File.WriteAllText(candidatePath, sb.ToString());
                ConceptLog.Append(bundleRoot, $"**Creation** — created `{id}` (by {actor}).", atUtc);
                return new WriteResult(true, $"Created '{id}'.");
            }

            id = $"{baseId}-{i}";
            i++;
        }
    }

    public static WriteResult UpdateBody(string bundleRoot, string id, string newBody, string actor, DateTime atUtc)
    {
        var validation = ValidateId(bundleRoot, id, out var path);
        if (!validation.Success)
        {
            return validation;
        }

        if (!File.Exists(path))
        {
            return new WriteResult(false, $"No concept with id '{id}'.");
        }

        var rawText = File.ReadAllText(path);
        var lines = rawText.Split('\n');
        if (lines.Length == 0 || lines[0].TrimEnd('\r') != "---")
        {
            return new WriteResult(false, $"Concept '{id}' is missing a YAML frontmatter block.");
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
            return new WriteResult(false, $"Concept '{id}' frontmatter block is not terminated with '---'.");
        }

        var frontmatterLines = lines[1..closingLine].ToList();

        var generatedLine = $"generated: {{ by: {actor}, at: {atUtc:yyyy-MM-ddTHH:mm:ssZ} }}";
        ReplaceOrAddLine(frontmatterLines, @"^generated:\s*\{", generatedLine);

        // A body change invalidates any prior human review — every agent
        // write lands back at draft; only a human promotes to stable.
        ReplaceOrAddLine(frontmatterLines, @"^status:\s*\S+", "status: draft");

        var newText = "---\n" + string.Join('\n', frontmatterLines) + "\n---\n\n" + newBody.TrimEnd('\n') + "\n";
        File.WriteAllText(path, newText);

        ConceptLog.Append(bundleRoot, $"**Update** — updated the body of `{id}` (by {actor}).", atUtc);
        return new WriteResult(true, $"Updated '{id}'.");
    }

    public static WriteResult AddLink(
        string bundleRoot,
        string fromId,
        string toId,
        string linkText,
        string section,
        string actor,
        DateTime atUtc)
    {
        var fromValidation = ValidateId(bundleRoot, fromId, out var fromPath);
        if (!fromValidation.Success)
        {
            return fromValidation;
        }

        if (!File.Exists(fromPath))
        {
            return new WriteResult(false, $"No concept with id '{fromId}'.");
        }

        var toValidation = ValidateId(bundleRoot, toId, out var toPath);
        if (!toValidation.Success)
        {
            return toValidation;
        }

        var rawText = File.ReadAllText(fromPath);
        var (_, body) = OkfParser.ParseConcept(rawText);

        var headingPattern = new Regex(
            $@"^#{{1,6}}\s+{Regex.Escape(section)}\s*$",
            RegexOptions.IgnoreCase | RegexOptions.Multiline);
        var linkLine = $"- [{linkText}](/{toId}.md)";

        var match = headingPattern.Match(body);
        string newBody;
        if (match.Success)
        {
            var lines = body.Split('\n').ToList();
            var headingLineIndex = body[..match.Index].Count(c => c == '\n');
            var insertAt = headingLineIndex + 1;
            while (insertAt < lines.Count && lines[insertAt].Trim().Length == 0)
            {
                insertAt++;
            }

            lines.Insert(insertAt, linkLine);

            // Keep a blank line between the inserted link and whatever
            // follows it, unless that's another list item (so several
            // links added under the same heading stack as one list).
            var nextIndex = insertAt + 1;
            if (nextIndex < lines.Count && lines[nextIndex].Trim().Length > 0 && !lines[nextIndex].TrimStart().StartsWith('-'))
            {
                lines.Insert(nextIndex, "");
            }

            newBody = string.Join('\n', lines);
        }
        else
        {
            newBody = body.TrimEnd('\n') + $"\n\n# {section}\n\n{linkLine}\n";
        }

        var updateResult = UpdateBody(bundleRoot, fromId, newBody, actor, atUtc);
        if (!updateResult.Success)
        {
            return updateResult;
        }

        // Links may point at a concept that doesn't exist yet — SPEC.md §6.1
        // requires consumers to tolerate broken links rather than reject them.
        var note = File.Exists(toPath)
            ? ""
            : $" Note: '{toId}' does not currently exist in this bundle — the link is left dangling.";
        return new WriteResult(true, $"Linked '{fromId}' to '{toId}' under '# {section}'.{note}");
    }

    public static WriteResult FlagForReview(string bundleRoot, string id, string reason, string actor, DateTime atUtc)
    {
        var validation = ValidateId(bundleRoot, id, out var path);
        if (!validation.Success)
        {
            return validation;
        }

        if (!File.Exists(path))
        {
            return new WriteResult(false, $"No concept with id '{id}'.");
        }

        ConceptLog.Append(bundleRoot, $"**Flag** — `{id}` flagged for review by {actor}: {reason}", atUtc);
        return new WriteResult(true, $"Flagged '{id}' for review.");
    }

    private static WriteResult ValidateId(string bundleRoot, string id, out string fullPath)
    {
        fullPath = "";

        if (string.IsNullOrWhiteSpace(id) || id.Contains("..") || id.Contains('\\') || id.StartsWith('/'))
        {
            return new WriteResult(false, $"'{id}' is not a valid concept id.");
        }

        var root = Path.GetFullPath(bundleRoot);
        var rootWithSeparator = root.EndsWith(Path.DirectorySeparatorChar) ? root : root + Path.DirectorySeparatorChar;
        var candidate = Path.GetFullPath(Path.Combine(root, id + ".md"));

        if (!candidate.StartsWith(rootWithSeparator, StringComparison.Ordinal))
        {
            return new WriteResult(false, $"'{id}' resolves outside the bundle.");
        }

        fullPath = candidate;
        return new WriteResult(true, "");
    }

    private static void ReplaceOrAddLine(List<string> lines, string pattern, string replacement)
    {
        var regex = new Regex(pattern);
        var index = lines.FindIndex(l => regex.IsMatch(l));
        if (index >= 0)
        {
            lines[index] = replacement;
        }
        else
        {
            lines.Add(replacement);
        }
    }

    private static string Slugify(string value)
    {
        var lowered = value.ToLowerInvariant();
        var slug = Regex.Replace(lowered, "[^a-z0-9]+", "-").Trim('-');
        return string.IsNullOrEmpty(slug) ? "item" : slug;
    }

    private static string Pluralize(string type)
    {
        var lower = type.ToLowerInvariant().Replace(' ', '-');
        return lower.EndsWith('s') ? lower : lower + "s";
    }

    private static string YamlString(string s)
    {
        var clean = Regex.Replace(s, @"\s+", " ").Trim();
        var escaped = clean.Replace("\\", "\\\\").Replace("\"", "\\\"");
        return $"\"{escaped}\"";
    }
}
