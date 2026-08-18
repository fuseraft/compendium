# Configuration

## LLM Provider Configuration

Compendium works with any OpenAI-compatible API endpoint.

There is no config file to hand-edit. LLM settings are configured through
one of two interactive entry points, and **either one configures both the
CLI and the Web UI** — they share the same underlying store, so you only
need to do this once:

### Option 1: CLI

```bash
compendium init
```

Prompts for a provider base URL, API key, and model (offering a pick-list
fetched from the provider's `/models` endpoint when available).

### Option 2: Web UI

Start the web server (`./bin/web/Compendium.Web`) and open the
[Settings page](../guide/web-ui.md#6-settings-page) at `/settings`. Same
fields, same shared result.

### Where it's stored

- Base URL and model name: `~/.compendium/llm-config.json`
- API key: the OS-native credential store where available (Windows
  Credential Manager today; see [issue tracker](https://github.com/fuseraft/compendium/issues)
  for macOS Keychain / Linux Secret Service support), falling back to a
  plain-text file in `~/.compendium/` on platforms without one yet

### Supported Providers

Any OpenAI-compatible endpoint works. Enter these values when prompted by
`compendium init` or the Web UI Settings page:

| Provider | Base URL | Model example |
|----------|----------|----------------|
| OpenAI | `https://api.openai.com/v1` | `gpt-4-turbo-preview` |
| Azure OpenAI | `https://YOUR_RESOURCE.openai.azure.com/openai/deployments/YOUR_DEPLOYMENT` | `gpt-4` |
| litellm Proxy | `http://localhost:4000` | `gpt-4` |
| Anthropic (via litellm) | `http://localhost:4000` | `claude-3-opus-20240229` |
| Ollama (local) | `http://localhost:11434/v1` | `llama3.1` |

## Environment Variables (CI / scripting)

For non-interactive contexts — CI pipelines, containers, scripted runs —
the CLI also accepts `LITELLM_BASE_URL`, `LITELLM_API_KEY`, and
`LITELLM_MODEL` as a fallback when nothing has been configured via
`compendium init` or the Web UI:

```bash
export LITELLM_API_KEY="sk-..."
export LITELLM_BASE_URL="https://api.openai.com/v1"
export LITELLM_MODEL="gpt-4"

compendium chat --bundle my-catalog
```

This only applies to the CLI. The Web UI always reads from the shared
store described above.

## Web UI Default Bundle

The Web UI loads a bundle at startup from `Compendium:BundlePath` in
`src/Compendium.Web/appsettings.json`:

```json
{
  "Compendium": {
    "BundlePath": "catalog/sample"
  }
}
```

This is a build-time default, not something the CLI's `compendium init`
sets — point it at your catalog before publishing the web server.

## Bundle Configuration

Each bundle can have a `.compendium/config.json` declaring the concept
types it recognizes — `compendium new <path>` scaffolds one automatically:

```json
{
  "name": "Enterprise Architecture Catalog",
  "description": "Systems, processes, and integrations",
  "types": {
    "System": { "directory": "systems", "description": "An application, service, or database." },
    "Integration": { "directory": "integrations", "description": "A data flow between two systems." },
    "Process": { "directory": "processes", "description": "A business workflow." }
  },
  "allow_new_types": "propose"
}
```

See [OKF: Bundle Spec](../features/okf.md#bundle-spec-compendiumconfigjson)
for the full schema and what `allow_new_types` controls.
