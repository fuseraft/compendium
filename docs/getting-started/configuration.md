# Configuration

## LLM Provider Configuration

Compendium works with any OpenAI-compatible API endpoint.

### Interactive Configuration

```bash
compendium init
```

### Manual Configuration

`compendium init` writes a `.env` file at the repo root — you can edit it
directly instead:

```
LITELLM_BASE_URL=https://api.openai.com/v1
LITELLM_API_KEY=sk-...
LITELLM_MODEL=gpt-4
```

### Supported Providers

#### OpenAI

```
LITELLM_BASE_URL=https://api.openai.com/v1
LITELLM_API_KEY=sk-...
LITELLM_MODEL=gpt-4-turbo-preview
```

#### Azure OpenAI

```
LITELLM_BASE_URL=https://YOUR_RESOURCE.openai.azure.com/openai/deployments/YOUR_DEPLOYMENT
LITELLM_API_KEY=...
LITELLM_MODEL=gpt-4
```

#### litellm Proxy

```
LITELLM_BASE_URL=http://localhost:4000
LITELLM_API_KEY=sk-1234
LITELLM_MODEL=gpt-4
```

#### Anthropic (via litellm)

```
LITELLM_BASE_URL=http://localhost:4000
LITELLM_API_KEY=sk-ant-...
LITELLM_MODEL=claude-3-opus-20240229
```

## Web UI Configuration

Edit `src/Compendium.Web/appsettings.json`:

```json
{
  "Compendium": {
    "DefaultBundlePath": "catalog/sample"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

## Environment Variables

`LITELLM_BASE_URL`, `LITELLM_API_KEY`, and `LITELLM_MODEL` can be set
directly in the environment instead of (or to override) `.env`:

```bash
export LITELLM_API_KEY="sk-..."
export LITELLM_BASE_URL="https://api.openai.com/v1"
export LITELLM_MODEL="gpt-4"

compendium chat --bundle my-catalog
```

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
