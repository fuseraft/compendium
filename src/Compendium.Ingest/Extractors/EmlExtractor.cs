using MimeKit;

namespace Compendium.Ingest.Extractors;

public sealed class EmlExtractor : IDocumentExtractor
{
    public IReadOnlyList<ExtractedRecord> Extract(string filePath)
    {
        var message = MimeMessage.Load(filePath);

        var metadata = new Dictionary<string, string>();
        if (message.From.Count > 0)
        {
            metadata["From"] = message.From.ToString();
        }
        if (message.To.Count > 0)
        {
            metadata["To"] = message.To.ToString();
        }
        metadata["Date"] = message.Date.ToString("u");

        var text = message.TextBody ?? message.HtmlBody ?? "";
        var title = string.IsNullOrWhiteSpace(message.Subject) ? "(no subject)" : message.Subject;

        return new[] { new ExtractedRecord(title, text, metadata) };
    }
}
