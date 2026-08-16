using MsgReader.Outlook;

namespace Compendium.Ingest.Extractors;

public sealed class MsgExtractor : IDocumentExtractor
{
    public IReadOnlyList<ExtractedRecord> Extract(string filePath)
    {
        using var message = new Storage.Message(filePath);

        var metadata = new Dictionary<string, string>();
        var from = message.Sender?.DisplayName ?? message.Sender?.Email;
        if (!string.IsNullOrWhiteSpace(from))
        {
            metadata["From"] = from;
        }
        if (message.Recipients.Count > 0)
        {
            metadata["To"] = string.Join(", ", message.Recipients.Select(r => r.DisplayName ?? r.Email ?? ""));
        }
        if (message.SentOn.HasValue)
        {
            metadata["Date"] = message.SentOn.Value.ToString("u");
        }

        var text = message.BodyText ?? message.BodyHtml ?? "";
        var title = string.IsNullOrWhiteSpace(message.Subject) ? "(no subject)" : message.Subject;

        return new[] { new ExtractedRecord(title, text, metadata) };
    }
}
