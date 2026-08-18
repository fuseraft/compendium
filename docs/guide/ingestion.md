# Ingestion

The `compendium ingest` command transforms source documents into OKF concepts. It's format-aware: what constitutes "one concept" depends on the file type — a whole document, a row, a message, a diagram page, or a modeling element.

## Basic Usage

```bash
compendium ingest --source <file-or-dir> --bundle <bundlePath> [--type <ConceptType>]
```

### Parameters

- **`--source`** — A single file or a directory (walked recursively)
- **`--bundle`** — The OKF bundle directory to write into (created if it doesn't exist, though bare — for a bundle with a starter type spec and seed concept already in place, run `compendium new <path>` first)
- **`--type`** — The concept type recorded in frontmatter (defaults to "Document")

### Examples

```bash
# Ingest a single file
compendium ingest --source docs/architecture.pdf --bundle my-catalog --type Document

# Ingest a directory recursively
compendium ingest --source sharepoint-export/ --bundle my-catalog --type Document

# Ingest data maps
compendium ingest --source datamaps/ --bundle my-catalog --type "Data Map"

# Ingest architecture diagrams
compendium ingest --source diagrams/ --bundle my-catalog --type "Architecture Diagram"
```

## Supported File Formats

### Documents

| Format | Extension | One Concept Per | Concept Text |
|--------|-----------|-----------------|--------------|
| Plain text | `.txt` | whole file | raw text content |
| Markdown | `.md` | whole file | raw markdown content |
| PDF | `.pdf` | whole file | extracted text |
| Word | `.docx` | whole file | extracted text |

### Structured Data

| Format | Extension | One Concept Per | Concept Text |
|--------|-----------|-----------------|--------------|
| JSON | `.json` | whole file (or array element) | pretty-printed value |
| XML | `.xml` | whole file (or repeated child) | pretty-printed value |
| CSV | `.csv` | row | each column as "Header: value" |
| Excel | `.xlsx` | row per worksheet | each column as "Header: value" |

For JSON and XML: if the root is an array (JSON) or has repeating same-named children (XML), one concept is created per element/child.

### Email

| Format | Extension | One Concept Per | Concept Text |
|--------|-----------|-----------------|--------------|
| Email message | `.eml`, `.msg` | message | email body |
| Outlook mailbox | `.ost` | message inside mailbox | email body |

Email metadata (from/to/subject/date) becomes frontmatter metadata.

### Diagrams

| Format | Extension | One Concept Per | Concept Text |
|--------|-----------|-----------------|--------------|
| draw.io | `.drawio` | page/tab | shape list + connections |
| Visio | `.vsdx` | page | shape list + connections |

Diagrams preserve graph structure: "Shapes: A, B, C" / "Connections: A → B (label)"

### Architecture Models

| Format | Extension | One Concept Per | Concept Text |
|--------|-----------|-----------------|--------------|
| ArchiMate (Archi) | `.archimate` | ArchiMate element | element type, layer, relationships |

Supports Archi's native XML format. Each element (Application Component, Business Actor, Node, etc.) becomes a concept with typed relationships preserved.

## How Concepts Are Generated

Every ingested concept receives consistent OKF frontmatter:

```yaml
---
type: Document                              # From --type parameter
title: "Architecture Overview"              # Extracted from content
description: "High-level system design..."  # First sentence or 240 chars
tags: [imported, pdf]                       # Format tag added automatically
status: draft                               # Always starts as draft
generated:
  by: process:compendium-ingest/0.1
  at: 2026-08-16T10:30:00Z
sources:
  - id: original-file
    resource: /references/architecture.pdf   # Mirrored to references/
    title: "architecture.pdf"
---

# Content

[Extracted content here...]
```

### Key Fields

- **`type`** — From `--type` parameter
- **`title`** — Extracted from filename, first heading, subject line, or content
- **`description`** — Auto-summarized from first sentence or first 240 characters
- **`tags`** — Includes `imported` and format tag (e.g., `pdf`, `csv`, `archimate`)
- **`status: draft`** — All ingested content starts unverified until human review
- **`generated`** — Attribution to the ingestion process
- **`sources`** — Points to original file mirrored in `<bundle>/references/`

### Format-Specific Metadata

Additional fields based on file type:

- **CSV/Excel**: Column values become frontmatter metadata
- **Email**: `from`, `to`, `subject`, `date` fields
- **Diagrams**: `shape_count`, `page_name`
- **ArchiMate**: `archimate_type`, `layer`, `element_type`
- **Data Maps**: `source_systems`, `destination_systems`, `field_count`

## File Organization

Ingested concepts are organized by type:

```
my-bundle/
├── documents/              # --type Document
│   ├── architecture.md
│   └── design-spec.md
├── data-maps/              # --type "Data Map"
│   ├── projectsync.md
│   └── contractssync.md
├── architecture-diagrams/  # --type "Architecture Diagram"
│   ├── system-overview.md
│   └── network-topology.md
└── references/             # Original files preserved
    ├── architecture.pdf
    ├── projectsync.xlsx
    └── system-overview.drawio
```

Type names are lowercased, spaces become hyphens, and pluralized.

## Special Handling: Data Maps

Data maps (CSV/Excel files with columns like `Int Name`, `SRC Column`, `DST Column`) are automatically detected and grouped by integration:

- One concept per `Int Name` value
- All field mappings grouped together
- Source and destination systems extracted
- See [Data Maps Guide](data-maps.md) for details

## Batch Processing

Process multiple sources with different types:

```bash
# SharePoint exports
compendium ingest --source sharepoint-export/ --bundle my-catalog --type Document

# Architecture diagrams
compendium ingest --source diagrams/ --bundle my-catalog --type "Architecture Diagram"

# Email archives
compendium ingest --source emails.ost --bundle my-catalog --type Email

# Data lineage maps
compendium ingest --source datamaps/ --bundle my-catalog --type "Data Map"
```

## Error Handling

The ingestion process is resilient:

- **Unsupported formats** — Skipped with a warning
- **Parse failures** — Logged but don't abort the batch
- **Missing metadata** — Filled with defaults
- **Summary report** — Shows processed/written/skipped/failed counts

Example output:

```
Processed: 150 files
Written: 142 concepts
Skipped: 5 unsupported formats
Failed: 3 parse errors
```

## Verification and Review

After ingestion:

1. **Check references/** — Original files are mirrored for provenance
2. **Review drafts** — Use the web UI review page or CLI to approve concepts
3. **Promote to stable** — Edit frontmatter to change `status: draft` → `status: stable`
4. **Add verification** — Manually add `verified: {by: user:yourname, at: timestamp}`

## Advanced Options

### Custom Concept Types

Define your own types based on domain needs:

```bash
compendium ingest --source apis/ --bundle my-catalog --type "API Endpoint"
compendium ingest --source reports/ --bundle my-catalog --type "Business Report"
compendium ingest --source teams/ --bundle my-catalog --type "Team"
```

### Incremental Updates

Re-run ingestion on the same source to update concepts:

```bash
# Initial ingest
compendium ingest --source docs/ --bundle my-catalog

# Later, after source files change
compendium ingest --source docs/ --bundle my-catalog
```

New files are added, updated files regenerate concepts with new `generated.at` timestamps.

## Limitations

- **No cross-linking** — Ingestion doesn't resolve relationships between concepts (e.g., system aliases). That's a subsequent enrichment step.
- **No domain logic** — Generic extraction only. Domain-specific processing (e.g., normalizing system names) happens separately.
- **Draft status** — All ingested content stays `draft` until manually reviewed.

## Next Steps

After ingestion:

- [Review concepts](../features/agent.md#curation) with the agent or web UI
- [Link related concepts](concepts.md#linking-concepts) together
- [Configure stale dates](concepts.md#concept-lifecycle) for time-sensitive knowledge
- [Enable agent curation](../guide/chat.md) to maintain the catalog over time
