# Compendium

An OKF-first knowledge catalog for enterprise architecture.

## What is Compendium?

Compendium helps enterprise architects connect AI agents to the documents their organization already has — scattered across SharePoint sites, Confluence spaces, wikis, and shared drives — and turn them into a living, agent-curated map of the business.

Most enterprise knowledge already exists, written for humans skimming a page. Compendium's job is to turn that sprawl into a single, coherent knowledge catalog: a directory of interlinked concepts describing the enterprise's architecture, systems, business processes, and integrations.

## Key Features

### 📄 OKF-First

Every concept is a plain markdown file with YAML frontmatter, conforming to the [Open Knowledge Format](https://github.com/GoogleCloudPlatform/knowledge-catalog/blob/main/okf/SPEC.md). This means:

- **Readable without Compendium** — Browse in Obsidian, a static file server, or hand to any LLM as context
- **Version-controlled** — Lives in git with diffs, blame, and pull requests
- **Portable** — No proprietary store between you and your metadata
- **Trustable at scale** — Provenance, trust, and lifecycle fields keep knowledge honest

### 🔌 Multi-Source Connectors

Connect to existing knowledge sources:

- SharePoint sites
- Confluence spaces
- Markdown wikis
- Local file systems
- Git repositories

### 📊 Data Lineage Tracking

Built-in support for **data maps** that document field-level data flows:

- Automatic detection and grouping by integration
- Source and destination system tracking
- Transformation logic preservation
- Graph-based lineage queries

### 🤖 AI Agent

The Compendium agent can:

- **Curate** — Ingest documents, create concepts, link relationships, flag stale knowledge
- **Answer** — Field questions grounded in the catalog with traceable sources
- **Search** — Find concepts across the entire knowledge base

### 📁 14+ File Formats

Ingest from:

- Documents: `.txt`, `.md`, `.pdf`, `.docx`
- Data: `.json`, `.xml`, `.csv`, `.xlsx`
- Email: `.eml`, `.msg`, `.ost`
- Diagrams: `.drawio`, `.vsdx`
- Architecture: `.archimate`

## Quick Start

```bash
# Build from source
./build.sh              # Linux/macOS
.\build.ps1             # Windows

# Configure LLM provider
compendium init

# Start web UI
./bin/web/Compendium.Web

# Or use CLI
compendium chat --bundle catalog/sample
```

See the [Getting Started](getting-started/installation.md) guide for detailed instructions.

## Architecture

```
   Enterprise sources                    Compendium                      Consumers
  ┌──────────────────┐            ┌─────────────────────────┐        ┌──────────────────┐
  │ SharePoint       │            │  Connectors             │        │ Compendium       │
  │ Confluence       │ ──ingest──▶│  Enrichment / curation │──────▶ │ system agent     │
  │ Markdown wikis   │            │  OKF bundle (git)       │        │ (Q&A + curation) │
  │ ...              │            └─────────────────────────┘        └──────────────────┘
  └──────────────────┘
```

## Use Cases

- **Enterprise Architecture Documentation** — Catalog systems, processes, and integrations
- **Data Governance** — Track data lineage and field-level flows
- **Knowledge Management** — Connect scattered documentation into a coherent graph
- **AI-Assisted Research** — Enable agents to answer questions with source attribution
- **Onboarding** — Help new team members understand the enterprise landscape

## Status

Compendium is in early preview. See the [GitHub repository](https://github.com/fuseraft/compendium) for current progress and roadmap.

## License

Open source software. See [LICENSE](https://github.com/fuseraft/compendium/blob/main/LICENSE) for details.
