# Supported File Formats

Compendium supports ingestion from 14+ file formats. This reference documents how each format is processed and what metadata is extracted.

## Format Support Matrix

| Category | Format | Extension | Status |
|----------|--------|-----------|--------|
| **Documents** | Plain Text | `.txt` | ✅ Full |
| | Markdown | `.md` | ✅ Full |
| | PDF | `.pdf` | ✅ Full |
| | Word | `.docx` | ✅ Full |
| **Data** | JSON | `.json` | ✅ Full |
| | XML | `.xml` | ✅ Full |
| | CSV | `.csv` | ✅ Full (data map detection) |
| | Excel | `.xlsx` | ✅ Full (data map detection) |
| **Email** | Email Message | `.eml` | ✅ Full |
| | Outlook Message | `.msg` | ⚠️ Unverified against real exports |
| | Outlook Mailbox | `.ost` | ⚠️ Unverified against real exports |
| **Diagrams** | draw.io | `.drawio` | ⚠️ Uncompressed: Full, Compressed: Unverified |
| | Visio | `.vsdx` | ✅ Full |
| **Architecture** | ArchiMate (Archi) | `.archimate` | ⚠️ Unverified against real Archi exports |

**Legend:**
- ✅ **Full** — Verified against real-world files
- ⚠️ **Unverified** — Implemented per public spec, not verified against real exports
- 🚧 **Partial** — Some features missing
- ❌ **Unsupported** — Not implemented

## Documents

### Plain Text (`.txt`)

**One concept per:** whole file

**Concept text:** raw text content

**Metadata extracted:**
- Filename → title
- First sentence or 240 chars → description

**Example:**

Input: `architecture-notes.txt`
```
This is the architecture document for the Order Management System.
It describes the high-level design and component interactions.
```

Output: `documents/architecture-notes.md`
```yaml
---
type: Document
title: "architecture-notes"
description: "This is the architecture document for the Order Management System."
tags: [imported, txt]
status: draft
sources:
  - id: original
    resource: /references/architecture-notes.txt
---

This is the architecture document for the Order Management System.
It describes the high-level design and component interactions.
```

### Markdown (`.md`)

**One concept per:** whole file

**Concept text:** raw markdown content

**Metadata extracted:**
- First `# Heading` → title
- Otherwise, filename → title
- First sentence or 240 chars → description

**Frontmatter handling:** If source `.md` already has YAML frontmatter, it's **not** preserved during ingestion (to avoid conflicts). To preserve metadata, migrate source files directly into the bundle instead of ingesting.

### PDF (`.pdf`)

**One concept per:** whole file

**Concept text:** extracted text content

**Metadata extracted:**
- PDF title metadata → title (if present)
- Otherwise, filename → title
- First sentence or 240 chars → description
- Page count → `page_count` in frontmatter

**Limitations:**
- Text-based PDFs only (OCR not supported)
- Layout preserved as plain text (no formatting)
- Images not extracted

### Word (`.docx`)

**One concept per:** whole file

**Concept text:** extracted text content

**Metadata extracted:**
- Document title property → title (if present)
- Otherwise, filename → title
- First sentence or 240 chars → description
- Author → `author` in frontmatter (if present)

**Limitations:**
- Formatting not preserved
- Images not extracted
- Comments and track changes ignored

## Structured Data

### JSON (`.json`)

**One concept per:**
- **Array:** one per array element
- **Object:** one per file

**Concept text:** pretty-printed JSON

**Metadata extracted:**
- Scalar properties → frontmatter fields
- `title`, `name`, or `id` property → title
- Array indices → part of title if no identifier field

**Example (object):**

Input: `config.json`
```json
{
  "service": "Order Management",
  "version": "2.3",
  "database": "CoreDB"
}
```

Output: `documents/config.md`
```yaml
---
type: Document
title: "config"
service: "Order Management"
version: "2.3"
database: "CoreDB"
---

```json
{
  "service": "Order Management",
  "version": "2.3",
  "database": "CoreDB"
}
```
```

**Example (array):**

Input: `systems.json`
```json
[
  { "name": "OMS", "type": "Application" },
  { "name": "Gateway", "type": "Service" }
]
```

Output: Two concepts:
- `documents/oms.md`
- `documents/gateway.md`

### XML (`.xml`)

**One concept per:**
- **Repeating children:** one per child
- **Single root:** one per file

**Concept text:** pretty-printed XML

**Metadata extracted:**
- Element attributes → frontmatter
- Text content of child elements → frontmatter (if simple)
- `name`, `title`, `id` attribute/child → title

**Example:**

Input: `systems.xml`
```xml
<systems>
  <system name="OMS" type="Application">
    <description>Order Management System</description>
  </system>
  <system name="Gateway" type="Service">
    <description>Payment Gateway</description>
  </system>
</systems>
```

Output: Two concepts (one per `<system>`)

### CSV (`.csv`)

**One concept per:** row (unless detected as data map)

**Concept text:** each column rendered as `Header: value`

**Metadata extracted:**
- Every column → frontmatter field
- `Name` or `Title` column → title
- Otherwise: filename + row number → title

**Data Map Detection:** If columns include `Int Name`, `SRC Column`, `DST Column`, treated as data map (grouped by `Int Name`). See [Data Maps Guide](../guide/data-maps.md).

**Example (regular CSV):**

Input: `systems.csv`
```csv
Name,Type,Owner
Order Management,Application,Platform Team
Payment Gateway,Service,Finance Team
```

Output: Two concepts:
- `documents/order-management.md`
- `documents/payment-gateway.md`

### Excel (`.xlsx`)

**One concept per:** row per worksheet (unless detected as data map)

**Concept text:** each column rendered as `Header: value`

**Metadata extracted:**
- Same as CSV
- Worksheet name → `worksheet` in frontmatter

**Data Map Detection:** Same as CSV. See [Data Maps Guide](../guide/data-maps.md).

**Multi-sheet handling:** Each sheet processed independently.

## Email

### Email Message (`.eml`, `.msg`)

**One concept per:** message

**Concept text:** email body (plain text or HTML converted to markdown)

**Metadata extracted:**
- Subject → title
- From → `from` in frontmatter
- To → `to` in frontmatter
- Date → `date` in frontmatter
- Attachments → listed in `attachments` array

**Example:**

Input: `meeting-notes.eml`
```
From: alice@company.com
To: team@company.com
Subject: Architecture Meeting Notes
Date: 2026-08-15

Here are the key decisions from today's meeting...
```

Output: `documents/architecture-meeting-notes.md`
```yaml
---
type: Document
title: "Architecture Meeting Notes"
from: "alice@company.com"
to: "team@company.com"
date: "2026-08-15"
---

Here are the key decisions from today's meeting...
```

### Outlook Mailbox (`.ost`)

**One concept per:** message inside the mailbox

**Concept text:** email body (same as `.eml`)

**Metadata extracted:** Same as `.eml`/`.msg`

**Verification Status:** ⚠️ Parser implemented per public spec (via `MsgReader` library) but not verified against real Outlook `.ost` files. If you encounter issues, please report.

**Note:** Each message is mirrored individually to `references/` (not the entire mailbox file).

## Diagrams

### draw.io (`.drawio`)

**One concept per:** page/tab

**Concept text:** shape list + labeled connections

**Metadata extracted:**
- Page name → `page_name` in frontmatter
- Shape count → `shape_count` in frontmatter

**Format:**

```
Shapes:
- Shape A
- Shape B
- Shape C

Connections:
- Shape A → Shape B (labeled: "sends data")
- Shape B → Shape C
```

**Verification Status:**
- **Uncompressed `.drawio`** (common in git) — ✅ Fully verified
- **Compressed `.drawio`** (draw.io default) — ⚠️ Implemented per spec, unverified

Compressed format uses deflate + base64 + URI encoding. Round-trip self-tested but not verified against real draw.io exports.

### Visio (`.vsdx`)

**One concept per:** page

**Concept text:** shape list + connections

**Metadata extracted:**
- Page name → `page_name` in frontmatter
- Shape count → `shape_count` in frontmatter

**Format:** Same as draw.io

## Architecture Models

### ArchiMate (Archi) (`.archimate`)

**One concept per:** ArchiMate element (Application Component, Business Actor, Node, etc.)

**Concept text:**
- Element type and layer
- All typed relationships (Serving, Realization, Assignment, etc.)

**Metadata extracted:**
- Element type → `archimate_type` in frontmatter
- Layer → `layer` in frontmatter
- Element name → title

**Format:**

```
Type: ApplicationComponent
Layer: Application

Relationships:
- This component Serves Business Process A
- This component Is Realized By Infrastructure Node B
- This component Is Assigned To Team C
```

**Verification Status:** ⚠️ Parsed from Archi's documented native XML format, not verified against real Archi application exports (no sample file available during development).

**Note:** Only Archi's native format is supported. The Open Group's tool-interop exchange format is not implemented.

## Unsupported Formats

The following formats are **not supported** (skipped during ingestion):

- **Images** (`.png`, `.jpg`, `.gif`) — No text extraction
- **Videos** (`.mp4`, `.avi`, `.mov`) — No text extraction
- **Audio** (`.mp3`, `.wav`) — No transcription
- **Binaries** (`.exe`, `.dll`, `.so`) — Not documents
- **Compressed archives** (`.zip`, `.tar.gz`) — Extract first, then ingest

Unsupported files are logged and skipped (do not abort ingestion batch).

## Adding Format Support

To add support for a new format, see [Contributing Guide](../development/contributing.md#adding-a-new-format-reader).

**Steps:**

1. Implement `IDocumentReader` in `src/Compendium.Ingest/Readers/`
2. Register in `ReaderRegistry`
3. Add tests in `tests/Compendium.Ingest.Tests/Readers/`
4. Update this documentation

## Format-Specific Options (Future)

Planned: Per-format configuration for ingestion.

```json
{
  "formats": {
    "pdf": {
      "ocr": true,
      "extract_images": false
    },
    "excel": {
      "skip_empty_rows": true,
      "header_row": 1
    }
  }
}
```

## Next Steps

- [Ingestion Guide](../guide/ingestion.md) — How to ingest documents
- [Data Maps Guide](../guide/data-maps.md) — CSV/Excel data map format
- [Architecture](../development/architecture.md) — How readers work internally
