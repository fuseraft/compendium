using System.Text;

namespace Compendium.Okf;

public sealed record ScaffoldResult(bool Success, string Message);

// Creates a new, empty OKF bundle with a starter `.compendium/config.json`
// spec and one hand-written seed concept. SPEC.md doesn't require either —
// a bundle is "just a directory" — but an empty directory gives neither a
// human author nor a curation agent any shape to grow from.
public static class BundleScaffold
{
    public static ScaffoldResult Create(string path, DateTime atUtc)
    {
        var root = Path.GetFullPath(path);

        if (Directory.Exists(root) && Directory.EnumerateFileSystemEntries(root).Any())
        {
            return new ScaffoldResult(false, $"'{root}' already exists and is not empty.");
        }

        Directory.CreateDirectory(root);
        Directory.CreateDirectory(Path.Combine(root, ".compendium"));
        Directory.CreateDirectory(Path.Combine(root, "references"));
        Directory.CreateDirectory(Path.Combine(root, "systems"));

        var name = Path.GetFileName(root.TrimEnd(Path.DirectorySeparatorChar));

        File.WriteAllText(Path.Combine(root, ".compendium", "config.json"), ConfigJson(name));
        File.WriteAllText(Path.Combine(root, "index.md"), IndexMd(name));
        File.WriteAllText(Path.Combine(root, "log.md"), "# Log\n");
        File.WriteAllText(Path.Combine(root, "references", ".gitkeep"), "");
        File.WriteAllText(Path.Combine(root, "systems", "example-system.md"), SeedConcept(atUtc));

        return new ScaffoldResult(true, $"Created bundle at '{root}'.");
    }

    private static string ConfigJson(string name)
    {
        var sb = new StringBuilder();
        sb.AppendLine("{");
        sb.AppendLine($"  \"name\": \"{name}\",");
        sb.AppendLine("  \"description\": \"Describe what this bundle catalogs.\",");
        sb.AppendLine("  \"types\": {");
        sb.AppendLine("    \"System\": {");
        sb.AppendLine("      \"directory\": \"systems\",");
        sb.AppendLine("      \"description\": \"An application, service, or database.\"");
        sb.AppendLine("    },");
        sb.AppendLine("    \"Process\": {");
        sb.AppendLine("      \"directory\": \"processes\",");
        sb.AppendLine("      \"description\": \"A business workflow spanning one or more systems.\"");
        sb.AppendLine("    },");
        sb.AppendLine("    \"Integration\": {");
        sb.AppendLine("      \"directory\": \"integrations\",");
        sb.AppendLine("      \"description\": \"A data flow between two systems.\"");
        sb.AppendLine("    }");
        sb.AppendLine("  },");
        sb.AppendLine("  \"allow_new_types\": \"propose\"");
        sb.AppendLine("}");
        return sb.ToString();
    }

    private static string IndexMd(string name)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"# {name}");
        sb.AppendLine();
        sb.AppendLine("An [OKF](https://github.com/GoogleCloudPlatform/knowledge-catalog/blob/main/okf/SPEC.md) knowledge bundle.");
        sb.AppendLine();
        sb.AppendLine("- `.compendium/config.json` — the concept types this bundle recognizes.");
        sb.AppendLine("- `systems/example-system.md` — a worked example; replace or delete it.");
        sb.AppendLine("- `references/` — original source documents, preserved for provenance.");
        sb.AppendLine();
        sb.AppendLine("Grow this bundle by hand-writing concepts, or run:");
        sb.AppendLine();
        sb.AppendLine("    compendium ingest --source <path> --bundle .");
        return sb.ToString();
    }

    private static string SeedConcept(DateTime atUtc)
    {
        var sb = new StringBuilder();
        sb.AppendLine("---");
        sb.AppendLine("type: System");
        sb.AppendLine("title: Example System");
        sb.AppendLine("description: A placeholder concept showing the expected shape — replace or delete it.");
        sb.AppendLine("tags: [example]");
        sb.AppendLine("status: draft");
        sb.AppendLine($"generated: {{ by: human:you, at: {atUtc:yyyy-MM-ddTHH:mm:ssZ} }}");
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine("# Overview");
        sb.AppendLine();
        sb.AppendLine("Replace this with a real system, process, or integration. Keep the");
        sb.AppendLine("frontmatter shape — `type`, `title`, `description`, `tags`, `status`,");
        sb.AppendLine("and `generated` are what Compendium and other OKF tools expect.");
        return sb.ToString();
    }
}
