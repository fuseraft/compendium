# Configuration

## LLM Provider Configuration

Compendium works with any OpenAI-compatible API endpoint.

### Interactive Configuration

```bash
compendium init
```

### Manual Configuration

Edit `~/.compendium/config.json`:

```json
{
  "baseUrl": "https://api.openai.com/v1",
  "apiKey": "sk-...",
  "model": "gpt-4"
}
```

### Supported Providers

#### OpenAI

```json
{
  "baseUrl": "https://api.openai.com/v1",
  "apiKey": "sk-...",
  "model": "gpt-4-turbo-preview"
}
```

#### Azure OpenAI

```json
{
  "baseUrl": "https://YOUR_RESOURCE.openai.azure.com/openai/deployments/YOUR_DEPLOYMENT",
  "apiKey": "...",
  "model": "gpt-4"
}
```

#### litellm Proxy

```json
{
  "baseUrl": "http://localhost:4000",
  "apiKey": "sk-1234",
  "model": "gpt-4"
}
```

#### Anthropic (via litellm)

```json
{
  "baseUrl": "http://localhost:4000",
  "apiKey": "sk-ant-...",
  "model": "claude-3-opus-20240229"
}
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

Override settings via environment variables:

```bash
export COMPENDIUM_API_KEY="sk-..."
export COMPENDIUM_BASE_URL="https://api.openai.com/v1"
export COMPENDIUM_MODEL="gpt-4"

compendium chat --bundle my-catalog
```

## Bundle Configuration

Each bundle can have a `.compendium/config.json`:

```json
{
  "name": "Enterprise Architecture Catalog",
  "description": "Systems, processes, and integrations",
  "conceptTypes": {
    "System": "systems",
    "Integration": "integrations",
    "Process": "processes",
    "Data Map": "data-maps"
  }
}
```
