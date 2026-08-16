# Compendium.Web

A lightweight Blazor Server web interface for the Compendium knowledge catalog.

## Features

- **Catalog Browser**: Browse all concepts with filtering by type and search
- **Concept Viewer**: View individual concepts with rendered markdown and full metadata
- **Chat Interface**: Chat with the Compendium agent to ask questions about the knowledge catalog
- **REST API**: HTTP endpoints for all CompendiumTools operations

## Configuration

Edit `appsettings.json` or create `appsettings.Development.json`:

```json
{
  "Compendium": {
    "BundlePath": "catalog/sample"
  },
  "LLM": {
    "BaseUrl": "http://localhost:11434/v1",
    "ApiKey": "not-needed",
    "ModelName": "llama3.2"
  }
}
```

Or set environment variables:
- `Compendium__BundlePath`: Path to your OKF bundle
- `LLM__BaseUrl`: OpenAI-compatible API base URL
- `LLM__ApiKey`: API key for the LLM provider
- `LLM__ModelName`: Model to use for the chat agent

## Running

First, build the web server:

```bash
# From the repository root
./build.sh --target=PublishWeb    # Linux/macOS
.\build.ps1 -Target PublishWeb    # Windows
```

Then run the server:

```bash
./bin/web/Compendium.Web          # Linux/macOS
.\bin\web\Compendium.Web.exe      # Windows
```

Then open http://localhost:5050 or https://localhost:5051 (or the URL shown in the console).

## API Endpoints

All endpoints are under `/api/concepts`:

- `GET /api/concepts` - List all concepts (optional `?type=System` filter)
- `GET /api/concepts/{id}` - Read a concept by id
- `GET /api/concepts/search?query=text` - Search concepts
- `POST /api/concepts` - Create a new concept
- `PUT /api/concepts/{id}/body` - Update a concept's body
- `POST /api/concepts/{id}/links` - Add a link between concepts
- `POST /api/concepts/{id}/flag` - Flag a concept for review

## Pages

- `/` - Catalog browser showing all concepts
- `/concept/{id}` - View a single concept
- `/chat` - Chat with the Compendium agent

## Architecture

- **Blazor Server** for interactive UI with minimal client-side JavaScript
- **BundleService** singleton loads and caches the OKF bundle
- **ConceptsController** exposes REST API wrapping CompendiumTools
- **Markdig** for rendering concept markdown
- **CompendiumAgentFactory** creates chat agents on demand
