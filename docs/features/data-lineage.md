# Data Lineage Tracking

Compendium tracks field-level data lineage through data maps, enabling you to trace how data flows from source systems through transformations to destinations. This is essential for data governance, impact analysis, and regulatory compliance.

## What is Data Lineage?

Data lineage documents the journey of data through an organization's systems:

```
Source System          Integration/ETL         Destination System
┌──────────────┐      ┌────────────────┐      ┌──────────────┐
│ ODS Database │      │ ProjectsToCalero│      │ CSV File     │
│              │ ───▶ │                │ ───▶ │              │
│ Project.ID   │      │ Transform:     │      │ ProjectId    │
│ Project.Name │      │ - Rename       │      │ Name         │
└──────────────┘      │ - Format       │      └──────────────┘
                      └────────────────┘
```

Compendium captures this as structured OKF concepts, enabling queries like:

- "Which integrations read from the ODS database?"
- "What happens to customer email addresses?"
- "Which reports use the SalesAmount field?"
- "What's the impact if we change the Customer table schema?"

## Data Map Format

Data maps are ingested from Excel (`.xlsx`) or CSV (`.csv`) files with specific columns. See [Data Maps Guide](../guide/data-maps.md) for the complete format specification.

### Key Columns

- **`Int Name`** — Integration or batch job name (groups field mappings)
- **`SRC DB`** / **`SRC Schema`** / **`SRC Table`** / **`SRC Column`** — Source location
- **`DST DB`** / **`DST Schema`** / **`DST Table`** / **`DST Column`** — Destination location
- **`Details`** — Transformation logic, business rules

### Example

| Int Name | Record Type | SRC DB | SRC Table | SRC Column | DST DB | DST Table | DST Column | Details |
|----------|-------------|--------|-----------|------------|--------|-----------|------------|---------|
| SalesReport | 2-Field Mapping | ODS | Sales | SalesAmount | ReportDB | DailySales | Amount | Rounded to 2 decimals |
| SalesReport | 2-Field Mapping | ODS | Sales | OrderDate | ReportDB | DailySales | Date | Converted to UTC |

## Automatic Lineage Extraction

When you ingest a data map file, Compendium automatically:

1. **Detects data maps** — Identifies files with the required column structure
2. **Groups by integration** — One concept per `Int Name` value
3. **Extracts systems** — Identifies source and destination systems
4. **Categorizes destinations** — Infers destination types (Database, File, Email, API, etc.)
5. **Preserves transformations** — Captures logic from the Details column

### Generated Metadata

Each data map concept includes structured metadata:

```yaml
type: Data Map
title: "SalesReport"
source_systems: "ODS"
destination_systems: "ReportDB"
destination_types: "Database"
field_count: "23"
```

This enables precise filtering and graph queries.

## Querying Lineage

### Via Agent

Ask natural language questions:

```
Which integrations read from the ODS database?
Show me all data flows that write to files
What transformations are applied to the SalesAmount field?
Trace the lineage of customer email addresses
Which systems feed data into the reporting database?
```

### Via CLI

```bash
# Find integrations by source system
compendium search --bundle my-catalog --query "source_systems:ODS"

# Find file outputs
compendium list --bundle my-catalog --type "Data Map" --format json | \
  jq '.[] | select(.destination_types | contains("File"))'

# Export lineage to CSV
compendium export --bundle my-catalog --type "Data Map" --format csv --output lineage.csv
```

### Via API

```bash
# Get all data maps
curl http://localhost:5050/api/concepts?type=Data%20Map

# Search by source system
curl "http://localhost:5050/api/concepts/search?q=source_systems:ODS"
```

## Lineage Visualization

### Concept View

Each data map concept displays:

- **Overview** — High-level integration description
- **Field Mappings Table** — All source → destination mappings
- **Source Systems** — Distinct source databases
- **Destination Systems** — Categorized destinations

Example:

```markdown
# SalesReport

## Overview
Generates daily sales report from ODS database to ReportDB.

## Field Mappings

| Source | Destination | Details |
|--------|-------------|---------|
| ODS.dbo.Sales.SalesAmount | ReportDB.dbo.DailySales.Amount | Rounded to 2 decimals |
| ODS.dbo.Sales.OrderDate | ReportDB.dbo.DailySales.Date | Converted to UTC |

## Source Systems
- ODS

## Destination Systems
- ReportDB (Database)
```

### Graph Visualization (Future)

Planned feature: Generate visual lineage diagrams showing data flows across systems.

```bash
compendium lineage graph --bundle my-catalog --output lineage.svg
```

Will produce a directed graph:

```
ODS ──▶ ProjectsToCalero ──▶ CSV File
ODS ──▶ SalesReport ──▶ ReportDB ──▶ PowerBI
Warehouse ──▶ InventorySync ──▶ SFTP
```

## Use Cases

### 1. Impact Analysis

**Question:** "If we change the Customer table schema, what breaks?"

**Approach:**

```bash
# Find all integrations reading from Customer table
compendium search --bundle my-catalog --query "SRC Table:Customer"
```

**Result:** List of impacted integrations and downstream systems.

### 2. Data Governance

**Question:** "Where does customer PII flow in our systems?"

**Approach:**

```bash
# Search for PII field names
compendium search --bundle my-catalog --query "Email OR SSN OR Phone"
```

**Result:** Full lineage of sensitive data across integrations.

### 3. Compliance Reporting

**Question:** "Document all data flows for GDPR audit."

**Approach:**

```bash
# Export all data maps to CSV
compendium export --bundle my-catalog --type "Data Map" --format csv --output gdpr-lineage.csv
```

**Result:** Comprehensive lineage report for auditors.

### 4. System Decommissioning

**Question:** "Can we safely decommission the Legacy CRM database?"

**Approach:**

```bash
# Check if anything still reads from it
compendium search --bundle my-catalog --query "source_systems:LegacyCRM"
```

**Result:** List of dependencies to migrate before decommissioning.

### 5. Data Quality Investigation

**Question:** "Why is the sales report showing incorrect totals?"

**Approach:**

```bash
# Read the SalesReport data map
compendium read --bundle my-catalog --id data-maps/salesreport
```

**Result:** See transformation logic and identify potential issues.

## Lineage Chaining (Future Feature)

Planned capability: Automatically chain lineage across multiple integrations.

When Integration A's destination matches Integration B's source, Compendium will:

1. Detect the connection
2. Link the concepts
3. Enable multi-hop lineage queries

Example:

```
ODS ─▶ ProjectsToCalero ─▶ CSV ─▶ S3Upload ─▶ S3 Bucket ─▶ DataWarehouse ─▶ PowerBI
```

Query: "Trace ODS.Project.Name to PowerBI"

Result: Full chain showing every transformation along the way.

## Best Practices

### 1. Consistent System Names

Use the same names for systems across all data maps:

✅ **Good:**
- Always use "ODS" (not "ODS DB", "ODS_PROD", "Operational Data Store")

❌ **Bad:**
- "ODS" in one map, "ODS_Database" in another, "Operational Store" in a third

### 2. Document Transformations

Capture business logic in the Details column:

✅ **Good:**
- "Rounded to 2 decimals"
- "Null → 0"
- "Uppercase + trim whitespace"
- "Calculated as Price * Quantity"

❌ **Bad:**
- "Transformed"
- "See code"
- (empty)

### 3. Include High-Level Overviews

Use `Record Type: 1-High-Level Overview` rows:

```
Int Name: SalesReport
Record Type: 1-High-Level Overview
Details: Generates daily sales report from ODS to ReportDB, runs at 2am, includes previous day's sales
```

This context is valuable when reviewing lineage.

### 4. Track Non-Database Destinations

For file outputs, APIs, emails, etc., use descriptive DST Column values:

✅ **Good:**
- DST Column: "CSV Column: ProjectId"
- DST Column: "API Field: customerId"
- DST Column: "Email: body"

❌ **Bad:**
- DST DB: (empty)
- DST Column: (empty)

### 5. Version Control Data Maps

Keep data map source files in git alongside the bundle:

```
my-catalog/
├── data-maps/          # Generated concepts
│   ├── salesreport.md
│   └── inventorysync.md
├── references/         # Original data map files
│   ├── lineage-v1.xlsx
│   └── lineage-v2.xlsx
└── source/             # Version-controlled source files
    └── data-lineage.xlsx
```

Commit updates:

```bash
git add source/data-lineage.xlsx
git commit -m "Update SalesReport lineage: add TaxAmount field"
```

### 6. Regular Sync

Re-ingest data maps regularly to keep lineage current:

```bash
# Weekly cron job
0 3 * * 1 compendium ingest --source source/data-lineage.xlsx --bundle my-catalog --type "Data Map"
```

## Troubleshooting

### Data Map Not Detected

**Problem:** CSV ingested as individual row concepts instead of grouped by integration

**Solution:** Verify required columns exist:
- `Int Name`
- `Record Type`
- `SRC Column` or `DST Column`

### Missing Source Systems

**Problem:** `source_systems` metadata is empty

**Solution:** Ensure `SRC DB` column is populated (use "N/A" if not applicable, not empty)

### Destination Type Not Inferred

**Problem:** `destination_types` shows "Unknown"

**Solution:** Use descriptive `DST Column` values for non-database destinations:
- "CSV Column: FieldName"
- "API Field: fieldName"
- "Email: subject"

See [Data Maps Guide](../guide/data-maps.md#non-database-destinations) for details.

## Future Enhancements

Planned features:

- **Lineage chaining** — Auto-link concepts when destinations match sources
- **Impact analysis tool** — CLI command to trace all downstream effects
- **Graph visualization** — SVG/PNG diagrams of data flows
- **Column-level lineage** — Track individual field transformations across multiple hops
- **Lineage validation** — Detect broken connections (destination doesn't exist)
- **Merge tracking** — Handle multiple source files contributing to the same integration

## Next Steps

- [Data Maps Guide](../guide/data-maps.md) — Complete format specification
- [Ingestion Guide](../guide/ingestion.md) — How to ingest data maps
- [Agent Guide](agent.md) — Querying lineage with natural language
- [CLI Reference](../guide/cli.md) — Command-line lineage queries
