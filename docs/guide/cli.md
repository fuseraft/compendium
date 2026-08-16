# CLI Reference

The Compendium CLI provides command-line tools for initializing configuration, ingesting documents, and interacting with the system agent through chat.

## Installation

See [Installation Guide](../getting-started/installation.md) for build and setup instructions.

## Available Commands

Compendium currently supports three core commands:

```bash
compendium init                                              # Configure your model provider
compendium chat --bundle <path>                              # Chat with the Compendium agent
compendium ingest --source <path> --bundle <path> [--type <Type>]  # Convert source documents into OKF concepts
```

## Commands

### `init`

Initialize Compendium configuration interactively.

```bash
compendium init
```

Prompts for:
- **Base URL** — LLM API endpoint
- **API Key** — Authentication key
- **Model** — Model name (fetches available models from provider)

Configuration saved to `~/.compendium/config.json`.

#### Options

```bash
--force, -f         Overwrite existing configuration
```

#### Example

```bash
$ compendium init
Base URL: https://api.openai.com/v1
API Key: sk-...
Fetching available models...
Select model:
  1. gpt-4-turbo-preview
  2. gpt-4
  3. gpt-3.5-turbo
Choice: 1

Configuration saved to ~/.compendium/config.json
```

---

### `ingest`

Transform source documents into OKF concepts.

```bash
compendium ingest --source <path> --bundle <path> [options]
```

#### Required Arguments

- `--source <path>` — File or directory to ingest
- `--bundle <path>` — Target OKF bundle directory

#### Options

```bash
--type <string>        Concept type (default: Document)
--recursive, -r        Recurse into subdirectories (default: true)
--overwrite            Overwrite existing concepts
--dry-run              Show what would be ingested without writing
```

#### Examples

```bash
# Ingest a single file
compendium ingest --source docs/arch.pdf --bundle my-catalog

# Ingest directory with specific type
compendium ingest --source datamaps/ --bundle my-catalog --type "Data Map"

# Dry run to preview
compendium ingest --source docs/ --bundle my-catalog --dry-run

# Overwrite existing concepts
compendium ingest --source docs/ --bundle my-catalog --overwrite
```

#### Output

```
Scanning source: docs/
Found: 150 files
Supported: 142 files
Skipped: 8 unsupported formats

Processing...
[=====================================] 100% (142/142)

Results:
  Written: 139 concepts
  Updated: 0 concepts
  Failed: 3 files

See my-catalog/ for generated concepts
```

---

### `chat`

Start an interactive chat session with the system agent.

```bash
compendium chat --bundle <path> [options]
```

#### Required Arguments

- `--bundle <path>` — OKF bundle to load

#### Options

```bash
--allow-write          Enable agent curation tools (CreateConcept, UpdateConceptBody, etc.)
--model <name>         Override configured model
--temperature <0-2>    Sampling temperature (default: 0.7)
--max-tokens <int>     Max response tokens (default: 4000)
```

#### Examples

```bash
# Read-only session
compendium chat --bundle my-catalog

# With write permissions
compendium chat --bundle my-catalog --allow-write

# Custom model
compendium chat --bundle my-catalog --model gpt-4-turbo-preview
```

#### Interactive Commands

Within a chat session:

```
/help              Show available commands
/exit, /quit       Exit session
/clear             Clear conversation history
/status            Show loaded bundle and model info
/write on|off      Toggle write mode
```

#### Example Session

```bash
$ compendium chat --bundle my-catalog
Loaded bundle: my-catalog (42 concepts)
Model: gpt-4-turbo-preview
Mode: read-only

> List all systems
Found 15 system concepts:
- Order Management System (systems/order-management.md)
- Payment Gateway (systems/payment-gateway.md)
- Inventory System (systems/inventory.md)
...

> What does the Order Management System do?
The Order Management System (OMS) handles customer orders from placement 
through fulfillment. It integrates with the Payment Gateway for payments
and the Warehouse Management System for shipping.

Source: systems/order-management.md (stable, verified 2026-08-10)

> /exit
Goodbye!
```

!!! note "Additional CLI Commands Planned"
    Commands for `list`, `read`, `search`, `create`, `update`, `delete`, `validate`, `export`, and `stats` are planned for future releases. Currently, these operations can be performed through:
    
    - **Chat interface** (`compendium chat`) - Use the agent to list, read, search, create, and update concepts
    - **Web UI** (`./bin/web/Compendium.Web`) - Visual interface for all operations
    - **REST API** (`/api/concepts`) - Programmatic access
    - **Direct file editing** - Concepts are plain markdown files

---

## Configuration

### Config File Location

Default: `~/.compendium/config.json`

Override: `--config <path>` or `COMPENDIUM_CONFIG` environment variable

### Config File Format

```json
{
  "baseUrl": "https://api.openai.com/v1",
  "apiKey": "sk-...",
  "model": "gpt-4-turbo-preview",
  "maxTokens": 4000,
  "temperature": 0.7
}
```

### Environment Variables

Override config file settings:

```bash
export COMPENDIUM_BASE_URL="https://api.openai.com/v1"
export COMPENDIUM_API_KEY="sk-..."
export COMPENDIUM_MODEL="gpt-4"
export COMPENDIUM_MAX_TOKENS="8000"
export COMPENDIUM_TEMPERATURE="0.5"
```

Environment variables take precedence over config file.

## Exit Codes

- **0** — Success
- **1** — General error
- **2** — Invalid arguments
- **3** — Configuration error
- **4** — Bundle not found
- **5** — Concept not found
- **6** — Validation failed

## Shell Completion

Generate shell completion scripts:

```bash
# Bash
compendium completion bash > /etc/bash_completion.d/compendium

# Zsh
compendium completion zsh > ~/.zsh/completion/_compendium

# PowerShell
compendium completion powershell > $PROFILE
```

## Working with Concepts

### Listing Concepts

Use the chat interface or directly read bundle directories:

```bash
# Via chat
compendium chat --bundle my-catalog
> List all concepts

# Via filesystem
ls my-catalog/systems/
ls my-catalog/integrations/
find my-catalog -name "*.md" -not -path "*/references/*"
```

### Reading Concepts

```bash
# Via chat
compendium chat --bundle my-catalog
> Read the concept systems/order-management

# Via filesystem
cat my-catalog/systems/order-management.md

# Via API (if web server running)
curl http://localhost:5050/api/concepts/systems/order-management
```

### Searching Concepts

```bash
# Via chat
compendium chat --bundle my-catalog
> Search for "payment"

# Via filesystem
grep -r "payment" my-catalog/ --include="*.md" --exclude-dir=references

# Via API
curl "http://localhost:5050/api/concepts/search?q=payment"
```

### Batch Operations

Use shell loops with direct file operations:

```bash
# Count concepts by type
find my-catalog -name "*.md" -not -path "*/references/*" -exec grep -l "^type:" {} \; | \
  xargs grep "^type:" | cut -d: -f3 | sort | uniq -c

# Find draft concepts
grep -l "^status: draft" my-catalog/**/*.md

# Update all drafts to stable (manual verification recommended)
for file in $(grep -l "^status: draft" my-catalog/**/*.md); do
  sed -i 's/^status: draft/status: stable/' "$file"
done
```

### Scripting

Example script to ingest and review:

```bash
#!/bin/bash
BUNDLE="my-catalog"
SOURCE="new-docs/"

# Ingest
echo "Ingesting from $SOURCE..."
compendium ingest --source "$SOURCE" --bundle "$BUNDLE"

# Count new drafts
DRAFTS=$(grep -l "^status: draft" "$BUNDLE"/**/*.md | wc -l)
echo "Created $DRAFTS draft concepts"

# Interactive review (if any drafts)
if [ "$DRAFTS" -gt 0 ]; then
  echo "Starting chat for review..."
  compendium chat --bundle "$BUNDLE" --allow-write
fi
```

## Next Steps

- [Chat Interface Guide](chat.md)
- [Ingestion Guide](ingestion.md)
- [Configuration Reference](../getting-started/configuration.md)
- [OKF Format](../features/okf.md)
