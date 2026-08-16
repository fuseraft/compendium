namespace Compendium.Ingest;

// One logical unit pulled out of a source document — a whole file for
// single-document formats (PDF/DOCX/TXT), or one row/message for container
// formats (CSV/XLSX rows, EML/MSG/OST messages).
//
// MirrorText: when set, the pipeline mirrors this text as the record's own
// reference file instead of pointing every record at one shared copy of the
// source file. Needed for OST, where the source is one large opaque binary
// mailbox that isn't a useful per-message resource link.
public sealed record ExtractedRecord(
    string Title,
    string Text,
    IReadOnlyDictionary<string, string> Metadata,
    string? MirrorText = null);
