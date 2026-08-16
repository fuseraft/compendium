# Data Maps in Compendium

Data maps document field-level data lineage across enterprise integrations and batch jobs. Each data map tracks how data flows from source systems (databases, files, APIs) to destinations (databases, files, emails, reports, etc.).

## Data Map File Format

Data maps are typically stored as Excel (`.xlsx`) or CSV (`.csv`) files with these columns:

| Column | Description |
|--------|-------------|
| `Int Name` | Integration or batch job name (groups field mappings) |
| `Record Type` | Either "1-High-Level Overview" or "2-Field Mapping" |
| `SRC DB` | Source database or system name |
| `SRC Schema` | Source schema (use "N/A" if not applicable) |
| `SRC Table` | Source table name (use "N/A" if not applicable) |
| `SRC Column` | Source column/field name |
| `DST DB` | Destination database or system name |
| `DST Schema` | Destination schema (use "N/A" if not applicable) |
| `DST Table` | Destination table name (use "N/A" if not applicable) |
| `DST Column` | Destination column/field name |
| `Details` | Transformation logic, business rules, or notes |

### Record Types

**1-High-Level Overview:** One row per integration describing what it does at a high level.

**2-Field Mapping:** Multiple rows per integration, one for each field being mapped from source to destination.

## Automatic Detection

When you ingest a CSV or Excel file, Compendium automatically detects if it's a data map by checking for the required column headers (`Int Name`, `Record Type`, `SRC Column`, `DST Column`). 

- **Data map files** → One concept per integration (all field mappings grouped together)
- **Regular CSV files** → One concept per row (default behavior)

## Ingesting Data Maps

### CLI

```bash
compendium ingest --source datamaps/ --bundle catalog/datamaps --type "Data Map"
```

### Web UI

1. Navigate to http://localhost:5050/ingest
2. Select "Data Map" as the concept type
3. Upload your Excel or CSV file
4. Click "Ingest Documents"

## Generated Concept Structure

Each data map integration becomes a single OKF concept with:

### Frontmatter Metadata
- `source_systems`: Distinct source databases/systems
- `destination_systems`: Distinct destination systems (categorized as Database, File, Email, API, etc.)
- `destination_types`: Categories of destinations (Database, File, Email, SFTP, API, Report, SharePoint)
- `field_count`: Number of field mappings

### Body Sections
- **Overview**: High-level description of what the integration does
- **Field Mappings**: Table showing source → destination with transformation details
- **Source Systems**: List of all source databases/systems
- **Destination Systems**: List of all destinations with inferred types

## Example

Given this data map:

| Int Name | Record Type | SRC DB | SRC Schema | SRC Table | SRC Column | DST DB | DST Schema | DST Table | DST Column | Details |
|----------|-------------|--------|------------|-----------|------------|--------|------------|-----------|------------|---------|
| ProjectsToCalero | 1-High-Level Overview | N/A | N/A | N/A | N/A | N/A | N/A | N/A | N/A | Takes project data from ODS, transforms to CSV |
| ProjectsToCalero | 2-Field Mapping | ODS | dbo | Project | ProjectString | N/A | N/A | N/A | N/A | The CSV column is called "ProjectId" |
| ProjectsToCalero | 2-Field Mapping | ODS | dbo | Project | Name | N/A | N/A | N/A | N/A | The CSV column is called "Name" |

Compendium creates **one concept** named "ProjectsToCalero" with:

```yaml
---
type: Data Map
title: "ProjectsToCalero"
source_systems: "ODS"
destination_systems: "File (CSV)"
destination_types: "File"
field_count: "2"
---

# Overview

Takes project data from ODS, transforms to CSV

**Field Mappings:**

| Source | Destination | Details |
|--------|-------------|---------|
| ODS.dbo.Project.ProjectString | N/A | The CSV column is called "ProjectId" |
| ODS.dbo.Project.Name | N/A | The CSV column is called "Name" |

**Source Systems:**
- ODS

**Destination Systems:**
- File (CSV)
```

## Non-Database Destinations

When `DST DB` is empty or "N/A", Compendium infers the destination type from:
- `DST Column` values (e.g., "CSV Column: ProjectId")
- `Details` field content (e.g., "uploads to SFTP", "sends email")

Recognized destination types:
- **File (CSV)** - CSV file output
- **File (SFTP)** - SFTP file uploads
- **Email** - Email notifications or attachments
- **API** - REST/SOAP web service calls
- **Report** - Generated reports
- **SharePoint** - SharePoint uploads
- **File/External** - Generic external file output

## Querying Data Maps

With the Compendium system agent, you can ask questions like:

- "Which integrations read from the ODS database?"
- "Show me all data flows that output to files"
- "What integrations transform data to uppercase?"
- "Which systems feed data into the reporting database?"

The structured metadata (`source_systems`, `destination_types`) enables precise filtering and graph-based queries for data lineage analysis.

## Best Practices

1. **Use consistent system names** across data maps for better graph connectivity
2. **Document transformations** in the Details column (e.g., "uppercase", "null → 0", "calculated as X + Y")
3. **Specify destination types** clearly when not using databases (e.g., "CSV Column: FieldName" in DST Column)
4. **Group related mappings** under the same `Int Name` for logical coherence
5. **Keep overview concise** but informative about the integration's purpose

## Future Enhancements

Planned features:
- **Data lineage chaining**: Auto-link concepts when one integration's destination matches another's source
- **CSV export**: Export data map concepts back to normalized CSV for version control
- **Visualization**: Generate flow diagrams showing system-to-system data paths
- **Merge tracking**: Track when multiple source files contribute to the same integration concept
