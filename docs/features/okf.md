# Open Knowledge Format (OKF)

Compendium uses the [Open Knowledge Format (OKF)](https://github.com/GoogleCloudPlatform/knowledge-catalog/blob/main/okf/SPEC.md) v0.2 as its native bundle format. OKF provides a standardized, portable way to represent knowledge that's equally readable by humans and AI agents.

## What is OKF?

OKF is a specification for representing knowledge as **plain markdown files with YAML frontmatter**, organized into a **bundle** (a directory structure). It was designed for enterprise knowledge management where:

- Knowledge needs to be **version-controlled** (git-friendly)
- **Provenance and trust** matter (where did this come from? who verified it?)
- **Portability** is essential (no lock-in to proprietary formats)
- **Human and agent readers** both need access

## Why OKF?

### Human-Readable
Browse concepts in any text editor, Obsidian, or static file server. No special tooling required.

```bash
# Just read it
cat systems/order-management.md
```

### Version-Controlled
Knowledge lives in git with full history, diffs, and blame:

```bash
git log systems/order-management.md
git blame systems/order-management.md
git diff main feature/update-oms systems/
```

### Portable
A bundle is just a directory. Copy it, archive it, ship it — no proprietary database or export format.

```bash
# Backup
tar -czf catalog-backup.tar.gz my-catalog/

# Share
rsync -av my-catalog/ remote:/catalogs/
```

### Trustable at Scale
When agents generate most of your catalog, OKF's `generated`, `sources`, `verified`, and `status` fields keep knowledge honest:

- **`status: draft`** — Unverified, agent-generated
- **`status: stable`** — Human-reviewed and approved
- **`sources`** — Traceable back to original documents
- **`verified`** — Explicit verification metadata

## Bundle Structure

An OKF bundle is a directory with:

```
my-bundle/
├── systems/                # Type-specific directories
│   ├── order-management.md
│   └── payment-gateway.md
├── integrations/
│   └── orders-to-warehouse.md
├── processes/
│   └── order-fulfillment.md
├── references/             # Source documents preserved
│   ├── oms-wiki.html
│   └── integration-spec.pdf
└── .compendium/            # Bundle spec (optional — see below)
    └── config.json
```

`compendium new my-catalog` scaffolds this whole layout, including a
starter `.compendium/config.json` and one seed concept. It's still just a
directory afterward — nothing about it requires the CLI going forward.

### Type Directories

Concepts are organized by type into directories:

- `systems/` — Applications, services, databases
- `integrations/` — Data flows between systems
- `processes/` — Business workflows
- `data-maps/` — Field-level lineage
- Any custom types you define

### References

The `references/` directory stores original source documents:

- Preserves provenance
- Allows verification against source
- Never modified by agents

### Bundle Spec (`.compendium/config.json`)

This is a Compendium extension, not part of OKF SPEC.md — a bundle without
this file is fully unconstrained, exactly as bundles behaved before it
existed. When present, it's the taxonomy the system agent's `CreateConcept`
tool is checked against:

```json
{
  "name": "my-catalog",
  "description": "Describe what this bundle catalogs.",
  "types": {
    "System": {
      "directory": "systems",
      "description": "An application, service, or database."
    },
    "Process": {
      "directory": "processes",
      "description": "A business workflow spanning one or more systems."
    }
  },
  "allow_new_types": "propose"
}
```

- **`types`** — the recognized concept types, each with a `directory` and
  `description`. The agent's `ListConceptTypes` tool reads this so it can
  discover the taxonomy before creating a concept.
- **`allow_new_types`** — what happens when an agent asks to create a
  concept of a type not listed above:
    - `"open"` — allowed, no record kept (pre-spec behavior).
    - `"propose"` (default when scaffolded) — allowed, but a note is
      appended to `log.md` flagging the type as unrecognized, for a human
      to later add to the spec or reject.
    - `"closed"` — rejected outright; the agent is told the allowed types
      and asked to pick one.

`compendium new` scaffolds this file with `System`, `Process`, and
`Integration` in `"propose"` mode — permissive enough not to block an
agent from growing the bundle past its starting shape, but visible enough
that drift doesn't happen silently.

## OKF Concept Structure

Each concept is a markdown file with YAML frontmatter:

```markdown
---
# Required
type: System
title: "Order Management System"

# Recommended
description: "Handles customer orders from placement through fulfillment"
tags: [critical, ecommerce]
status: stable

# Provenance
generated:
  by: process:compendium-ingest/0.1
  at: 2026-08-16T10:30:00Z
sources:
  - id: wiki
    resource: /references/oms-wiki.html
    title: "OMS Wiki Page"

# Trust
verified:
  by: user:alice
  at: 2026-08-16T14:00:00Z

# Lifecycle
stale_after: 2027-02-16
---

# Overview

The Order Management System (OMS) is the core system for...

## Dependencies

- Payment Gateway (Stripe)
- Inventory System
```

### Frontmatter Fields

#### Required
- **`type`** — Concept type ("System", "Process", "Integration", etc.)
- **`title`** — Human-readable name

#### Provenance
- **`generated`** — Who/what created this concept
  - `by` — Agent, process, or user identifier
  - `at` — UTC timestamp
- **`sources`** — Original documents
  - `id` — Source identifier
  - `resource` — Path to file in `references/`
  - `title` — Human-readable source name

#### Trust
- **`status`** — Lifecycle state
  - `draft` — Unverified
  - `stable` — Reviewed and approved
  - `deprecated` — Superseded
- **`verified`** — Verification metadata
  - `by` — Who verified
  - `at` — When verified
- **`stale_after`** — Date to review for accuracy

#### Organization
- **`tags`** — Categorization labels
- **`description`** — One-line summary
- **`links`** — Relationships to other concepts

## OKF Principles

### 1. Markdown + YAML Only
No proprietary formats. Every concept is readable with `cat` or any text editor.

### 2. Explicit Provenance
Every concept must trace back to where it came from:

```yaml
sources:
  - id: confluence-123
    resource: /references/confluence-page-123.html
    title: "Architecture Overview - Confluence"
```

### 3. Trust Metadata
Distinguish agent-generated drafts from human-verified knowledge:

```yaml
status: draft                   # Unverified
generated:
  by: agent:compendium/0.1      # Who created it
  at: 2026-08-16T10:00:00Z
```

vs.

```yaml
status: stable                  # Human-approved
verified:
  by: user:architect-team
  at: 2026-08-16T14:00:00Z
```

### 4. Staleness Detection
Knowledge has a shelf life:

```yaml
stale_after: 2027-02-16
```

Agents can flag concepts past their review date for re-verification.

### 5. Portable Identifiers
Concept IDs are derived from file paths:

- File: `systems/order-management.md`
- ID: `systems/order-management`

No database-assigned UUIDs. IDs are stable and portable.

## Compendium's OKF Implementation

### Conformance

Compendium fully implements OKF v0.2:

- ✅ Markdown files with YAML frontmatter
- ✅ Type-based directory organization
- ✅ Required `type` and `title` fields
- ✅ `generated`, `sources`, `verified`, `status`, `stale_after` support
- ✅ References directory for source preservation

### Extensions

Compendium adds optional fields for specific use cases:

#### Data Maps
```yaml
source_systems: "ODS, Warehouse"
destination_systems: "Reports, SFTP"
destination_types: "File, Email"
field_count: "23"
```

#### Diagrams
```yaml
shape_count: "15"
page_name: "System Overview"
```

#### Architecture Models
```yaml
archimate_type: "ApplicationComponent"
layer: "Application"
```

These extensions are ignored by other OKF-compliant tools, preserving portability.

## Interoperability

Any OKF-conformant bundle works with Compendium:

- Bundles created by other tools can be ingested
- Compendium bundles can be consumed by other OKF tools
- No vendor lock-in

Example: Create a bundle with a Python script using OKF spec, then query it with Compendium's agent.

## Best Practices

### 1. Use Git
Version control your bundle:

```bash
compendium new my-catalog
cd my-catalog
git init
git add .
git commit -m "Initial catalog"
```

### 2. Keep Sources
Always preserve original files in `references/`:

```yaml
sources:
  - id: original
    resource: /references/architecture-doc.pdf
    title: "Architecture Document v2.3"
```

### 3. Review Before Stable
Don't promote concepts to `stable` until verified:

```bash
# Start as draft
status: draft

# After human review
status: stable
verified:
  by: user:yourname
  at: 2026-08-16T14:00:00Z
```

### 4. Set Staleness Dates
Time-sensitive knowledge needs review dates:

```yaml
stale_after: 2027-02-16  # Review in 6 months
```

### 5. Tag Consistently
Use consistent tags across similar concepts:

```yaml
tags: [imported, critical, security-sensitive]
```

## Tools and Ecosystem

### Compendium
- **Ingestion** — Convert documents to OKF concepts
- **Agent** — Query and curate OKF bundles
- **Web UI** — Browse and review concepts
- **CLI** — Terminal-based interaction

### Other OKF Tools
- [Google Cloud Knowledge Catalog](https://github.com/GoogleCloudPlatform/knowledge-catalog) — Reference implementation
- Any tool implementing the OKF spec

### Editor Integration
- **Obsidian** — Renders OKF concepts with frontmatter
- **VS Code** — YAML frontmatter syntax highlighting
- **vim/emacs** — Standard markdown editing

## Further Reading

- [OKF Specification](https://github.com/GoogleCloudPlatform/knowledge-catalog/blob/main/okf/SPEC.md) — Official spec
- [Concepts Guide](../guide/concepts.md) — How Compendium uses OKF
- [Ingestion Guide](../guide/ingestion.md) — Creating OKF concepts from source files
