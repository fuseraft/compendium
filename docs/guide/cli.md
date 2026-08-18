# CLI Reference

The Compendium CLI provides command-line tools for initializing configuration, creating bundles, ingesting documents, and interacting with the system agent through chat.

## Installation

See [Installation Guide](../getting-started/installation.md) for build and setup instructions.

## Available Commands

Compendium currently supports four core commands:

```bash
compendium init                                              # Configure your model provider
compendium new <path>                                        # Create a new OKF bundle
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
- **Provider base URL** — the OpenAI-compatible API endpoint
- **API key** — entered masked; leave blank to keep the existing key when
  reconfiguring
- **Model** — a numbered pick-list fetched from the provider's `/models`
  endpoint, or type a model id directly if that fetch fails

There's no separate overwrite flag — re-running `init` shows your current
values as defaults, so it doubles as "reconfigure."

Configuration is saved to a `.env` file at the repo root
(`LITELLM_BASE_URL`, `LITELLM_API_KEY`, `LITELLM_MODEL`), not a JSON config
file.

#### Example

```bash
$ compendium init
Compendium setup — configure your model provider.

Provider base URL: https://api.openai.com/v1
API key: ****************
Available models:
  1. gpt-4-turbo-preview
  2. gpt-4
  3. gpt-3.5-turbo
Model (number or id): 1

Saved to /home/you/compendium/.env
Run `dotnet run --project src/Compendium.Cli -- chat` to start.
```

---

### `new`

Create a new OKF bundle.

```bash
compendium new <path>
```

Scaffolds:

- `.compendium/config.json` — a starter spec declaring `System`, `Process`,
  and `Integration` concept types in `"propose"` mode (see
  [OKF: Bundle Spec](../features/okf.md#bundle-spec-compendiumconfigjson))
- `index.md` — a short orientation page
- `log.md` — the bundle's provenance log
- `references/` — empty, for source documents you later ingest
- `systems/example-system.md` — a seed concept showing the expected
  frontmatter shape; replace or delete it

Fails without writing anything if `<path>` already exists and is not empty.

#### Example

```bash
$ compendium new my-catalog
Created bundle at '/home/you/my-catalog'.

Next steps:
  cat my-catalog/index.md
  compendium ingest --source <path> --bundle my-catalog   # grow it from source docs
  compendium chat --bundle my-catalog                     # or start chatting with it
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
--type <string>        Concept type recorded in frontmatter, and the name
                        of the folder concepts are written into (lowercased,
                        spaces→hyphens, pluralized). Default: Document
```

A directory passed to `--source` is always walked recursively; there's no
`--recursive`, `--overwrite`, or `--dry-run` flag today — re-ingesting the
same source writes new concept files rather than updating in place.

#### Examples

```bash
# Ingest a single file
compendium ingest --source docs/arch.pdf --bundle my-catalog

# Ingest directory with specific type
compendium ingest --source datamaps/ --bundle my-catalog --type "Data Map"
```

#### Output

```
$ compendium ingest --source docs/ --bundle my-catalog
Processed 150 file(s), wrote 142 concept(s) to my-catalog
Skipped 5 unsupported file(s):
  - docs/notes.txt.bak
  - docs/archive.zip
  ...
Failed to ingest 3 file(s):
  - docs/corrupt.pdf: <error message>
  ...
```

The "Skipped" and "Failed" blocks only print when there's something to
report.

---

### `chat`

Start an interactive chat session with the system agent.

```bash
compendium chat --bundle <path> [options]
```

#### Arguments

- `--bundle <path>` — OKF bundle to load. Optional — defaults to the
  repo's `catalog/sample` bundle if omitted.

#### Options

```bash
--allow-write          Enable agent curation tools (CreateConcept, UpdateConceptBody, AddLink, FlagForReview)
```

There's no `--model`, `--temperature`, or `--max-tokens` flag — the model
comes from `LITELLM_MODEL` in `.env` (set via `compendium init`).

#### Examples

```bash
# Read-only session
compendium chat --bundle my-catalog

# With write permissions
compendium chat --bundle my-catalog --allow-write
```

#### Ending a session

Type `exit` (case-insensitive), or press Enter on an empty line. There are
no in-session slash commands (`/help`, `/clear`, etc.) today — every line
you type that isn't blank or `exit` is sent to the agent as a query.

#### Example Session

```bash
$ compendium chat --bundle my-catalog
Compendium — 4 concept(s) loaded from /home/you/my-catalog
Read-only session — pass --allow-write to let the agent create or modify concepts.
Ask a question, or type 'exit' to quit.

> What does the Billing Service do?
The Billing Service issues invoices, collects payment via the payment
processor, and is the system of record for whether an order has been paid.

Source: systems/billing-service.md (stable)

> exit
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

`.env` at the repo root (found by walking up from the running executable
to `Compendium.slnx`). There's no `--config` flag or `COMPENDIUM_CONFIG`
variable to override the path.

### Config File Format

Plain `KEY=value` lines, written by `compendium init`:

```
LITELLM_BASE_URL=https://api.openai.com/v1
LITELLM_API_KEY=sk-...
LITELLM_MODEL=gpt-4-turbo-preview
```

### Environment Variables

The same three variables can be set directly in the environment instead of
(or to override) `.env`:

```bash
export LITELLM_BASE_URL="https://api.openai.com/v1"
export LITELLM_API_KEY="sk-..."
export LITELLM_MODEL="gpt-4"
```

`LITELLM_MODEL` falls back to `anthropic.claude-sonnet-5` if unset;
`LITELLM_BASE_URL` and `LITELLM_API_KEY` are required — `chat` exits with
an error telling you to run `init` if either is missing.

## Exit Codes

- **0** — Success
- **1** — Error (missing/invalid arguments, provider not configured,
  source not found, or — for `new` — the target directory already exists
  and isn't empty)

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
