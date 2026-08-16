namespace Compendium.Okf;

// Appends to a bundle's log.md per SPEC.md §9: a flat, date-grouped,
// newest-first list of prose entries. BundleLoader already treats log.md as
// a reserved filename it skips, so appending here never surfaces as a concept.
public static class ConceptLog
{
    public static void Append(string bundleRoot, string entry, DateTime atUtc)
    {
        var path = Path.Combine(bundleRoot, "log.md");
        var dateHeading = $"## {atUtc:yyyy-MM-dd}";
        var line = $"- {entry}";

        if (!File.Exists(path))
        {
            File.WriteAllText(path, $"# Log\n\n{dateHeading}\n\n{line}\n");
            return;
        }

        var text = File.ReadAllText(path);
        var lines = text.Split('\n').ToList();

        var dateLineIndex = lines.FindIndex(l => l.TrimEnd('\r') == dateHeading);
        if (dateLineIndex >= 0)
        {
            // Same day already has a heading — append underneath it, right
            // after the heading line (and the blank line that follows it).
            var insertAt = dateLineIndex + 1;
            while (insertAt < lines.Count && lines[insertAt].Trim().Length == 0)
            {
                insertAt++;
            }

            lines.Insert(insertAt, line);
        }
        else
        {
            // No heading for today yet — insert a new date group right
            // after the top-level "# Log" title so entries stay newest-first.
            var titleIndex = lines.FindIndex(l => l.TrimStart().StartsWith("# "));
            var insertAt = titleIndex >= 0 ? titleIndex + 1 : 0;
            while (insertAt < lines.Count && lines[insertAt].Trim().Length == 0)
            {
                insertAt++;
            }

            lines.InsertRange(insertAt, new[] { "", dateHeading, "", line });
        }

        File.WriteAllText(path, string.Join('\n', lines));
    }
}
