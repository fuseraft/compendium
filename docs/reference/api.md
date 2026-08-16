# API Reference

The Compendium Web server exposes a REST API for programmatic access to the knowledge catalog.

## Base URL

```
http://localhost:5050/api
```

Configure in `src/Compendium.Web/appsettings.json`:

```json
{
  "Urls": "http://localhost:5050"
}
```

## Authentication

**Current:** No authentication (intended for internal/trusted networks).

**Recommendation:** Deploy behind reverse proxy with authentication for production use.

## Content Type

All requests and responses use `application/json`.

## Endpoints

### List Concepts

```http
GET /api/concepts
```

List all concepts in the bundle.

**Query Parameters:**

| Parameter | Type | Description |
|-----------|------|-------------|
| `type` | string | Filter by concept type |
| `tag` | string | Filter by tag |
| `status` | string | Filter by status (draft, stable, deprecated) |
| `limit` | integer | Max results (default: 100) |
| `offset` | integer | Skip first N results (for pagination) |

**Example Requests:**

```bash
# All concepts
curl http://localhost:5050/api/concepts

# Systems only
curl http://localhost:5050/api/concepts?type=System

# Stable concepts
curl http://localhost:5050/api/concepts?status=stable

# Tagged as critical
curl http://localhost:5050/api/concepts?tag=critical

# Pagination
curl http://localhost:5050/api/concepts?limit=10&offset=20
```

**Response:**

```json
[
  {
    "id": "systems/order-management",
    "type": "System",
    "title": "Order Management System",
    "description": "Handles customer orders from placement through fulfillment",
    "status": "stable",
    "tags": ["critical", "ecommerce"],
    "generated": {
      "by": "process:compendium-ingest/0.1",
      "at": "2026-08-16T10:30:00Z"
    }
  },
  {
    "id": "systems/payment-gateway",
    "type": "System",
    "title": "Payment Gateway",
    "description": "Processes payments via Stripe API",
    "status": "stable",
    "tags": ["critical", "payments"],
    "generated": {
      "by": "agent:compendium/0.1",
      "at": "2026-08-16T12:00:00Z"
    }
  }
]
```

---

### Get Concept

```http
GET /api/concepts/{id}
```

Retrieve a specific concept by ID.

**Path Parameters:**

| Parameter | Type | Description |
|-----------|------|-------------|
| `id` | string | Concept ID (e.g., `systems/order-management`) |

**Example Request:**

```bash
curl http://localhost:5050/api/concepts/systems/order-management
```

**Response:**

```json
{
  "id": "systems/order-management",
  "type": "System",
  "title": "Order Management System",
  "description": "Handles customer orders from placement through fulfillment",
  "status": "stable",
  "tags": ["critical", "ecommerce"],
  "frontmatter": {
    "type": "System",
    "title": "Order Management System",
    "description": "Handles customer orders from placement through fulfillment",
    "status": "stable",
    "tags": ["critical", "ecommerce"],
    "generated": {
      "by": "process:compendium-ingest/0.1",
      "at": "2026-08-16T10:30:00Z"
    },
    "sources": [
      {
        "id": "wiki",
        "resource": "/references/oms-wiki.html",
        "title": "OMS Wiki Page"
      }
    ]
  },
  "body": "# Overview\n\nThe Order Management System (OMS) handles...",
  "sources": [
    {
      "id": "wiki",
      "resource": "/references/oms-wiki.html",
      "title": "OMS Wiki Page"
    }
  ],
  "links": [
    {
      "id": "systems/payment-gateway",
      "title": "Payment Gateway"
    }
  ]
}
```

**Error Response (404):**

```json
{
  "error": "Concept not found",
  "id": "systems/nonexistent"
}
```

---

### Search Concepts

```http
GET /api/concepts/search
```

Full-text search across concepts.

**Query Parameters:**

| Parameter | Type | Description |
|-----------|------|-------------|
| `q` | string | Search query (required) |
| `type` | string | Filter by concept type |
| `tag` | string | Filter by tag |
| `limit` | integer | Max results (default: 10) |

**Example Requests:**

```bash
# Search all concepts
curl "http://localhost:5050/api/concepts/search?q=payment"

# Search systems only
curl "http://localhost:5050/api/concepts/search?q=database&type=System"

# Limit results
curl "http://localhost:5050/api/concepts/search?q=API&limit=5"
```

**Response:**

```json
{
  "query": "payment",
  "count": 3,
  "results": [
    {
      "id": "systems/payment-gateway",
      "type": "System",
      "title": "Payment Gateway",
      "description": "Processes payments via Stripe API",
      "snippet": "...processes <mark>payments</mark> via Stripe API..."
    },
    {
      "id": "integrations/order-to-payment",
      "type": "Integration",
      "title": "Order to Payment",
      "description": "Sends order data to payment processor",
      "snippet": "...sends order data to <mark>payment</mark> processor..."
    },
    {
      "id": "processes/payment-reconciliation",
      "type": "Process",
      "title": "Payment Reconciliation",
      "description": "Daily reconciliation of payment transactions",
      "snippet": "...reconciliation of <mark>payment</mark> transactions..."
    }
  ]
}
```

---

### Create Concept

```http
POST /api/concepts
```

Create a new concept.

**Request Body:**

```json
{
  "type": "System",
  "title": "Analytics API",
  "description": "Real-time analytics service",
  "body": "# Overview\n\nThe Analytics API provides...",
  "tags": ["api", "analytics"]
}
```

**Required Fields:**
- `type`
- `title`
- `body`

**Optional Fields:**
- `description` (auto-generated if omitted)
- `tags`
- `status` (defaults to `draft`)

**Example Request:**

```bash
curl -X POST http://localhost:5050/api/concepts \
  -H "Content-Type: application/json" \
  -d '{
    "type": "System",
    "title": "Analytics API",
    "body": "# Overview\n\nReal-time analytics service.",
    "tags": ["api", "analytics"]
  }'
```

**Response (201 Created):**

```json
{
  "id": "systems/analytics-api",
  "message": "Concept created successfully"
}
```

**Error Response (400):**

```json
{
  "error": "Missing required field: title"
}
```

**Error Response (409):**

```json
{
  "error": "Concept already exists",
  "id": "systems/analytics-api"
}
```

---

### Update Concept Body

```http
PUT /api/concepts/{id}/body
```

Update an existing concept's body content.

**Path Parameters:**

| Parameter | Type | Description |
|-----------|------|-------------|
| `id` | string | Concept ID |

**Request Body:**

```json
{
  "body": "# Overview\n\nUpdated content..."
}
```

**Example Request:**

```bash
curl -X PUT http://localhost:5050/api/concepts/systems/analytics-api/body \
  -H "Content-Type: application/json" \
  -d '{
    "body": "# Overview\n\nUpdated real-time analytics service"
  }'
```

**Response (200 OK):**

```json
{
  "message": "Concept body updated successfully"
}
```

---

### Add Link Between Concepts

```http
POST /api/concepts/{id}/links
```

Add a link from one concept to another.

**Path Parameters:**

| Parameter | Type | Description |
|-----------|------|-------------|
| `id` | string | Source concept ID |

**Request Body:**

```json
{
  "toId": "systems/payment-gateway",
  "linkText": "Payment Gateway",
  "section": "Dependencies"
}
```

**Example Request:**

```bash
curl -X POST http://localhost:5050/api/concepts/systems/order-management/links \
  -H "Content-Type: application/json" \
  -d '{
    "toId": "systems/payment-gateway",
    "linkText": "Payment Gateway",
    "section": "Dependencies"
  }'
```

**Response (200 OK):**

```json
{
  "message": "Link added successfully"
}
```

---

### Flag Concept for Review

```http
POST /api/concepts/{id}/flag
```

Flag a concept for human review.

**Path Parameters:**

| Parameter | Type | Description |
|-----------|------|-------------|
| `id` | string | Concept ID to flag |

**Request Body:**

```json
{
  "reason": "This concept appears to be stale - mentions decommissioned servers"
}
```

**Example Request:**

```bash
curl -X POST http://localhost:5050/api/concepts/systems/legacy-system/flag \
  -H "Content-Type: application/json" \
  -d '{
    "reason": "This concept appears to be stale"
  }'
```

**Response (200 OK):**

```json
{
  "message": "Concept flagged for review"
}
```

!!! note "Additional Endpoints Planned"
    The following endpoints are planned for future releases:
    
    - `DELETE /api/concepts/{id}` - Delete a concept
    - `GET /api/types` - List concept types
    - `GET /api/tags` - List all tags
    - `GET /api/health` - Health check endpoint
    
    Currently, concept deletion and advanced queries can be performed through the Web UI or by directly editing bundle files

---

## Error Responses

All error responses follow this format:

```json
{
  "error": "Error message here",
  "details": "Additional context (optional)"
}
```

**HTTP Status Codes:**

| Code | Meaning |
|------|---------|
| 200 | Success |
| 201 | Created |
| 400 | Bad Request (invalid input) |
| 404 | Not Found |
| 409 | Conflict (resource already exists) |
| 500 | Internal Server Error |

## Rate Limiting

**Current:** No rate limiting.

**Recommendation:** Implement rate limiting at reverse proxy level for production.

## CORS

**Current:** CORS not enabled by default.

**Enable:** Edit `src/Compendium.Web/Program.cs`:

```csharp
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// ...

app.UseCors();
```

## Examples

### JavaScript (Fetch API)

```javascript
// List concepts
fetch('http://localhost:5050/api/concepts')
  .then(res => res.json())
  .then(concepts => console.log(concepts));

// Search
fetch('http://localhost:5050/api/concepts/search?q=payment')
  .then(res => res.json())
  .then(results => console.log(results));

// Create concept
fetch('http://localhost:5050/api/concepts', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({
    type: 'System',
    title: 'New System',
    body: '# Overview\n\nDescription here...'
  })
})
  .then(res => res.json())
  .then(data => console.log(data));
```

### Python (requests)

```python
import requests

# List concepts
response = requests.get('http://localhost:5050/api/concepts')
concepts = response.json()
print(concepts)

# Search
response = requests.get('http://localhost:5050/api/concepts/search', params={'q': 'payment'})
results = response.json()
print(results)

# Create concept
response = requests.post('http://localhost:5050/api/concepts', json={
    'type': 'System',
    'title': 'New System',
    'body': '# Overview\n\nDescription here...'
})
result = response.json()
print(result)
```

### cURL

```bash
# List all concepts
curl http://localhost:5050/api/concepts

# Get specific concept
curl http://localhost:5050/api/concepts/systems/order-management

# Search
curl "http://localhost:5050/api/concepts/search?q=payment&limit=5"

# Create concept
curl -X POST http://localhost:5050/api/concepts \
  -H "Content-Type: application/json" \
  -d '{"type":"System","title":"New System","body":"# Overview\n\nDescription"}'

# Update concept body
curl -X PUT http://localhost:5050/api/concepts/systems/new-system/body \
  -H "Content-Type: application/json" \
  -d '{"body":"# Overview\n\nUpdated content"}'

# Add link
curl -X POST http://localhost:5050/api/concepts/systems/oms/links \
  -H "Content-Type: application/json" \
  -d '{"toId":"systems/payment","linkText":"Payment Gateway","section":"Dependencies"}'

# Flag for review
curl -X POST http://localhost:5050/api/concepts/systems/old-system/flag \
  -H "Content-Type: application/json" \
  -d '{"reason":"Appears stale"}'
```

## Future Enhancements

Planned API features:

- **Batch operations** — Create/update multiple concepts in one request
- **Relationships endpoint** — Query concept relationships
- **History endpoint** — View concept change history (via git)
- **Export endpoint** — Export filtered concepts as JSON/CSV
- **Webhook support** — Notify on concept changes
- **GraphQL API** — Alternative to REST for complex queries

## Next Steps

- [Web UI Guide](../guide/web-ui.md) — Browser-based interface
- [CLI Reference](../guide/cli.md) — Command-line alternative
- [Agent Guide](../features/agent.md) — AI-powered interaction
