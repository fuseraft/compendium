# Quick Start

Get up and running with Compendium in 5 minutes.

## 1. Initialize Configuration

Configure your LLM provider (OpenAI, Azure OpenAI, or any OpenAI-compatible endpoint):

```bash
compendium init
```

This interactive command will prompt you for:
- Base URL (API endpoint)
- API key
- Model name

The configuration is saved to a `.env` file at the repo root.

## 2. Create a Bundle

A bundle is a directory containing OKF concepts. Create one:

```bash
compendium new my-catalog
cd my-catalog
```

This scaffolds `.compendium/config.json` (the concept types this bundle
recognizes), an `index.md`, and one seed concept
(`systems/example-system.md`) showing the expected shape — replace or
delete it once you have real concepts.

## 3. Ingest Documents

Ingest existing documentation into the bundle:

```bash
compendium ingest --source /path/to/docs --bundle . --type Document
```

This creates OKF concepts from your source files.

## 4. Start the Web UI

Launch the Blazor web interface:

```bash
# Build the web server (first time only)
./build.sh --target=PublishWeb    # Linux/macOS
.\build.ps1 -Target PublishWeb    # Windows

# Run the server
./bin/web/Compendium.Web          # Linux/macOS
.\bin\web\Compendium.Web.exe      # Windows
```

Open http://localhost:5050 in your browser.

## 5. Chat with the Agent

### Via Web UI

1. Navigate to http://localhost:5050
2. Click "Load Bundle" and select your bundle directory
3. Go to "Settings" and configure your LLM
4. Go to "Chat" and start asking questions

### Via CLI

```bash
compendium chat --bundle my-catalog
```

Example queries:

- "List all concepts"
- "What systems are documented?"
- "Show me integrations that read from the ODS database"

## Common Workflows

### Ingest Data Maps

```bash
compendium ingest --source datamaps/ --bundle my-catalog --type "Data Map"
```

### Ingest from Multiple Sources

```bash
# SharePoint exports
compendium ingest --source sharepoint-export/ --bundle my-catalog --type Document

# Architecture diagrams
compendium ingest --source diagrams/ --bundle my-catalog --type "Architecture Diagram"

# Email archives
compendium ingest --source emails.ost --bundle my-catalog --type Email
```

### Review and Approve Drafts

1. Open http://localhost:5050/review
2. Review auto-generated concepts
3. Approve (marks as `stable`) or reject

### Enable Agent Curation

By default, the agent has read-only access. To enable write access:

```bash
compendium chat --bundle my-catalog --allow-write
```

This gives the agent access to:
- `CreateConcept` — mint new concepts
- `UpdateConceptBody` — edit concept content
- `AddLink` — create relationships
- `FlagForReview` — mark concepts for human review

!!! warning "Agent-created content stays `draft`"
    The agent can never promote concepts to `stable` — only humans can do that.

## Next Steps

- [Configuration Guide](configuration.md) — Advanced LLM settings
- [User Guide](../guide/concepts.md) — Understanding OKF concepts
- [Data Maps](../guide/data-maps.md) — Field-level lineage tracking
