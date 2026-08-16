using XstReader;

namespace Compendium.Ingest.Extractors;

// One record per message across every folder in the mailbox. A corrupt or
// unreadable message is skipped rather than aborting the whole file — OST
// mailboxes are large and real-world files vary in how cleanly they parse.
public sealed class OstExtractor : IDocumentExtractor
{
    public IReadOnlyList<ExtractedRecord> Extract(string filePath)
    {
        using var xstFile = new XstFile(filePath);
        var records = new List<ExtractedRecord>();

        foreach (var folder in EnumerateFolders(xstFile.RootFolder))
        {
            foreach (var message in folder.Messages)
            {
                try
                {
                    records.Add(ToRecord(message));
                }
                catch
                {
                    // Skip this one message; the rest of the mailbox still ingests.
                }
            }
        }

        return records;
    }

    // A malformed or adversarial mailbox could produce an unexpectedly deep
    // or cyclic folder tree; a depth cap plus a visited-set turns that into
    // a bounded, catchable failure instead of a StackOverflowException,
    // which .NET cannot catch and would crash the whole ingest process.
    private const int MaxFolderDepth = 256;

    private static IEnumerable<XstFolder> EnumerateFolders(XstFolder root)
    {
        var visited = new HashSet<XstFolder>();
        var stack = new Stack<(XstFolder Folder, int Depth)>();
        stack.Push((root, 0));

        while (stack.Count > 0)
        {
            var (folder, depth) = stack.Pop();
            if (depth > MaxFolderDepth || !visited.Add(folder))
            {
                continue;
            }

            yield return folder;

            foreach (var sub in folder.Folders)
            {
                stack.Push((sub, depth + 1));
            }
        }
    }

    private static ExtractedRecord ToRecord(XstMessage message)
    {
        var metadata = new Dictionary<string, string>();
        if (!string.IsNullOrWhiteSpace(message.From))
        {
            metadata["From"] = message.From;
        }
        if (!string.IsNullOrWhiteSpace(message.To))
        {
            metadata["To"] = message.To;
        }
        if (message.Date.HasValue)
        {
            metadata["Date"] = message.Date.Value.ToString("u");
        }

        var text = message.Body?.Text ?? "";
        var title = string.IsNullOrWhiteSpace(message.Subject) ? "(no subject)" : message.Subject;

        var mirrorText = string.Join(
            "\n",
            metadata.Select(kv => $"{kv.Key}: {kv.Value}").Append("").Append(text));

        return new ExtractedRecord(title, text, metadata, MirrorText: mirrorText);
    }
}
