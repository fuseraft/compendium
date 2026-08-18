# Web UI

The Compendium web UI provides a visual interface for browsing, reviewing, and interacting with your knowledge catalog. Built with Blazor Server, it offers a responsive interface for catalog management and agent interaction.

## Starting the Web Server

### Build and Run

```bash
# Build the web server (first time)
./build.sh --target=PublishWeb    # Linux/macOS
.\build.ps1 -Target PublishWeb    # Windows

# Run the server
./bin/web/Compendium.Web          # Linux/macOS
.\bin\web\Compendium.Web.exe      # Windows
```

The server starts on http://localhost:5050 by default.

### Configuration

The default bundle and listen URL are set in
`src/Compendium.Web/appsettings.json`:

```json
{
  "Compendium": {
    "BundlePath": "catalog/sample"
  },
  "Urls": "http://localhost:5050",
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  }
}
```

LLM provider settings are **not** configured here — see
[Settings Page](#6-settings-page) below, or
[Configuration](../getting-started/configuration.md).

## Features

### 1. Catalog Browser

**URL**: http://localhost:5050/

Browse all concepts in your bundle with:

- **Filter by type** — Systems, Integrations, Processes, Data Maps, etc.
- **Search** — Full-text search across titles and descriptions
- **Sort** — By title, type, or last modified
- **Status badges** — Visual indicators for draft/stable/deprecated

Click any concept to view its full content.

### 2. Concept Viewer

**URL**: http://localhost:5050/concept/{id}

View individual concepts with:

- **Rendered markdown** — Body content displayed with syntax highlighting
- **Frontmatter display** — All metadata visible at the top
- **Source links** — Direct links to original files in `references/`
- **Related concepts** — Linked concepts shown in sidebar
- **Edit link** — Quick access to source file (for local bundles)

Example: `http://localhost:5050/concept/systems/order-management`

### 3. Chat Interface

**URL**: http://localhost:5050/chat

Interactive chat with the Compendium agent:

- **Natural language queries** — Ask questions about your catalog
- **Tool call visibility** — See which catalog operations the agent performs
- **Citation links** — Click concept references to view them
- **Conversation history** — Full session history maintained
- **Copy/export** — Copy conversation to clipboard

Features:
- Auto-scrolling to latest message
- Multi-line input support (Shift+Enter for new line)
- Markdown rendering in agent responses
- Loading indicator during agent processing

### 4. Review Interface

**URL**: http://localhost:5050/review

Review and approve draft concepts:

- **Draft list** — All `status: draft` concepts
- **Side-by-side view** — Preview concept content before approval
- **Bulk actions** — Approve or reject multiple concepts
- **Metadata editing** — Edit frontmatter before promoting to stable
- **Source preview** — View original source document

Actions:
- **Approve** — Promotes to `status: stable` and adds `verified` metadata
- **Reject** — Deletes the concept
- **Edit** — Opens inline editor for frontmatter and body
- **Defer** — Keep as draft for later review

### 5. Ingestion Interface

**URL**: http://localhost:5050/ingest

Upload and ingest documents:

- **File upload** — Drag-and-drop or browse
- **Directory support** — Upload multiple files at once
- **Type selection** — Choose concept type
- **Progress tracking** — Real-time ingestion progress
- **Results summary** — Processed/written/skipped/failed counts

Supported formats: See [Ingestion Guide](ingestion.md)

### 6. Settings Page

**URL**: http://localhost:5050/settings

Configure the LLM provider — base URL, API key, and model, with a
"Fetch Available Models" lookup against the provider's `/models` endpoint.

This is the same configuration `compendium init` writes on the CLI side —
whichever one you use, the other picks it up automatically. Settings save
immediately (no restart) to `~/.compendium/llm-config.json` plus the OS
credential store for the API key. See
[Configuration](../getting-started/configuration.md#llm-provider-configuration)
for where things are stored.

## API Endpoints

The web server exposes a REST API:

### List Concepts

```bash
GET /api/concepts
GET /api/concepts?type=System
GET /api/concepts?tag=critical
```

Response:
```json
[
  {
    "id": "systems/order-management",
    "type": "System",
    "title": "Order Management System",
    "description": "Handles customer orders...",
    "status": "stable",
    "tags": ["critical", "ecommerce"]
  }
]
```

### Get Concept

```bash
GET /api/concepts/{id}
```

Response:
```json
{
  "id": "systems/order-management",
  "type": "System",
  "title": "Order Management System",
  "frontmatter": { ... },
  "body": "# Overview\n\n...",
  "sources": [ ... ],
  "links": [ ... ]
}
```

### Search Concepts

```bash
GET /api/concepts/search?q=payment
```

Response:
```json
{
  "query": "payment",
  "results": [
    {
      "id": "systems/payment-gateway",
      "title": "Payment Gateway",
      "snippet": "...processes payments via Stripe..."
    }
  ]
}
```

### Create Concept (requires write mode)

```bash
POST /api/concepts
Content-Type: application/json

{
  "type": "System",
  "title": "New System",
  "body": "# Overview\n\nDescription here..."
}
```

## Keyboard Shortcuts

### Global

- **`/`** — Focus search
- **`Ctrl+K`** — Quick command palette
- **`Escape`** — Clear search/close modal

### Chat Interface

- **`Enter`** — Send message
- **`Shift+Enter`** — New line
- **`Ctrl+L`** — Clear conversation
- **`Ctrl+C`** — Copy conversation

### Catalog Browser

- **`↑`/`↓`** — Navigate list
- **`Enter`** — Open selected concept
- **`Ctrl+F`** — Focus filter

## Responsive Design

The UI adapts to different screen sizes:

- **Desktop** — Full sidebar with filters
- **Tablet** — Collapsible sidebar
- **Mobile** — Drawer-based navigation

## Deployment

### Production Build

```bash
./build.sh --target=PublishWeb --configuration=Release
```

### Running as a Service

#### Linux (systemd)

Create `/etc/systemd/system/compendium-web.service`:

```ini
[Unit]
Description=Compendium Web Server
After=network.target

[Service]
Type=simple
User=compendium
WorkingDirectory=/opt/compendium
ExecStart=/opt/compendium/bin/web/Compendium.Web
Restart=on-failure

[Install]
WantedBy=multi-user.target
```

Enable and start:

```bash
sudo systemctl enable compendium-web
sudo systemctl start compendium-web
```

#### Windows (NSSM)

```powershell
nssm install CompendiumWeb "C:\compendium\bin\web\Compendium.Web.exe"
nssm set CompendiumWeb AppDirectory "C:\compendium"
nssm start CompendiumWeb
```

### Reverse Proxy

#### nginx

```nginx
server {
    listen 80;
    server_name compendium.example.com;

    location / {
        proxy_pass http://localhost:5050;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection "upgrade";
        proxy_set_header Host $host;
        proxy_cache_bypass $http_upgrade;
    }
}
```

#### Apache

```apache
<VirtualHost *:80>
    ServerName compendium.example.com
    
    ProxyPreserveHost On
    ProxyPass / http://localhost:5050/
    ProxyPassReverse / http://localhost:5050/
    
    RewriteEngine on
    RewriteCond %{HTTP:Upgrade} websocket [NC]
    RewriteCond %{HTTP:Connection} upgrade [NC]
    RewriteRule ^/?(.*) "ws://localhost:5050/$1" [P,L]
</VirtualHost>
```

## Security Considerations

### Authentication

The web UI does not include built-in authentication. For production:

1. **Use a reverse proxy** with authentication (nginx + Basic Auth, OAuth proxy, etc.)
2. **Run on internal network** only (not exposed to public internet)
3. **Use firewall rules** to restrict access

### HTTPS

Configure HTTPS in `appsettings.json`:

```json
{
  "Urls": "https://localhost:5443",
  "Kestrel": {
    "Endpoints": {
      "Https": {
        "Url": "https://localhost:5443",
        "Certificate": {
          "Path": "cert.pfx",
          "Password": "..."
        }
      }
    }
  }
}
```

### API Keys

Never commit API keys to version control. Use:

- Environment variables
- User-specific config files (not in git)
- Secret management services (Azure Key Vault, AWS Secrets Manager)

## Troubleshooting

### Server Won't Start

**Problem**: Port 5050 already in use

**Solution**: Change port in `appsettings.json` or environment variable:

```bash
export ASPNETCORE_URLS="http://localhost:8080"
./bin/web/Compendium.Web
```

### Bundle Not Loading

**Problem**: "Bundle not found" error

**Solution**: Check bundle path:

```json
{
  "Compendium": {
    "BundlePath": "/full/path/to/my-catalog"
  }
}
```

### Chat Not Working

**Problem**: Agent returns errors

**Solutions**:
- Verify LLM configuration in Settings
- Check API key is valid
- Ensure bundle is loaded
- Check browser console for errors (F12)

### Slow Performance

**Problem**: UI is sluggish with large bundles

**Solutions**:
- Enable pagination in Settings
- Index the bundle (future feature)
- Archive old concepts
- Use CLI for large operations

## Browser Support

Tested and supported:

- Chrome/Edge 90+
- Firefox 88+
- Safari 14+

Features using WebSockets (SignalR):
- Chat interface
- Real-time updates
- Progress tracking

## Next Steps

- [Configure LLM settings](../getting-started/configuration.md)
- [Use the chat interface](chat.md)
- [Review and approve concepts](../features/agent.md#curation)
- [Ingest more documents](ingestion.md)
