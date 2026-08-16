# Building from Source

This guide covers building Compendium from source code, including prerequisites, build scripts, and distribution packaging.

## Prerequisites

### Required

- **.NET 10 SDK** — [Download here](https://dotnet.microsoft.com/download/dotnet/10.0)
- **Git** — For cloning the repository

Verify installation:

```bash
dotnet --version  # Should show 10.x.x
git --version
```

### Optional

- **PowerShell 7+** — For Windows build scripts (Windows PowerShell 5.1 also works)
- **Make** — Alternative build tool (Linux/macOS)
- **Docker** — For containerized builds

## Quick Build

### Linux/macOS

```bash
git clone https://github.com/fuseraft/compendium.git
cd compendium
./build.sh
```

### Windows

```powershell
git clone https://github.com/fuseraft/compendium.git
cd compendium
.\build.ps1
```

The CLI binary will be available at:
- Linux/macOS: `./bin/cli/Compendium.Cli`
- Windows: `.\bin\cli\Compendium.Cli.exe`

## Build Targets

The build scripts support multiple targets:

```bash
# Linux/macOS
./build.sh --target=<target>

# Windows
.\build.ps1 -Target <target>
```

### Available Targets

| Target | Description |
|--------|-------------|
| `Build` | Compile all projects (Debug configuration) |
| `Test` | Run all unit and integration tests |
| `PublishCli` | Publish CLI tool (default) |
| `PublishWeb` | Publish web server |
| `PublishAll` | Publish both CLI and Web |
| `Pack` | Create distribution archives |
| `Lint` | Check code formatting |
| `Clean` | Remove build artifacts |

### Examples

```bash
# Build and run tests
./build.sh --target=Test

# Build web server
./build.sh --target=PublishWeb

# Build everything
./build.sh --target=PublishAll

# Clean and rebuild
./build.sh --target=Clean
./build.sh
```

## Build Configurations

### Debug Build (Default)

```bash
./build.sh --configuration=Debug
```

- Includes debug symbols
- No optimizations
- Larger binaries
- Better for development and debugging

### Release Build

```bash
./build.sh --configuration=Release
```

- Optimized code
- Smaller binaries
- No debug symbols
- Production-ready

## Runtime Identifiers

Build for specific platforms using runtime identifiers (RIDs):

```bash
# Linux x64
./build.sh --runtime=linux-x64

# Windows x64
.\build.ps1 -Runtime win-x64

# macOS ARM64
./build.sh --runtime=osx-arm64
```

### Common RIDs

| RID | Platform |
|-----|----------|
| `linux-x64` | Linux x86-64 |
| `linux-arm64` | Linux ARM64 |
| `win-x64` | Windows x86-64 |
| `win-arm64` | Windows ARM64 |
| `osx-x64` | macOS Intel |
| `osx-arm64` | macOS Apple Silicon |

### Self-Contained vs Framework-Dependent

#### Framework-Dependent (Default)

```bash
./build.sh --target=PublishCli
```

- Requires .NET runtime installed on target system
- Smaller binaries (~10 MB)
- Shares runtime with other .NET apps

#### Self-Contained

```bash
./build.sh --target=Pack --runtime=linux-x64
```

- Bundles .NET runtime
- Larger binaries (~60 MB)
- No runtime installation required
- Better for distribution

## Project Structure

```
compendium/
├── src/
│   ├── Compendium.Cli/         # CLI application
│   ├── Compendium.Web/         # Blazor Server web UI
│   ├── Compendium.Core/        # Core OKF logic
│   ├── Compendium.Agent/       # AI agent system
│   ├── Compendium.Ingest/      # Document ingestion
│   └── Compendium.Connectors/  # Source connectors
├── tests/
│   ├── Compendium.Core.Tests/
│   ├── Compendium.Ingest.Tests/
│   └── Compendium.Agent.Tests/
├── build/                      # Build scripts and targets
├── bin/                        # Build output
│   ├── cli/
│   └── web/
└── dist/                       # Distribution packages
```

## Testing

### Run All Tests

```bash
./build.sh --target=Test
```

### Run Specific Test Projects

```bash
dotnet test tests/Compendium.Core.Tests/
dotnet test tests/Compendium.Ingest.Tests/
```

### With Code Coverage

```bash
dotnet test --collect:"XPlat Code Coverage"
```

Coverage reports generated in `tests/*/TestResults/`.

## Packaging

### Create Distribution Archives

```bash
# Linux
./build.sh --target=Pack --runtime=linux-x64

# Windows
.\build.ps1 -Target Pack -Runtime win-x64

# macOS
./build.sh --target=Pack --runtime=osx-arm64
```

Creates archives in `dist/`:
- `compendium-cli-{version}-{runtime}.tar.gz` (Linux/macOS)
- `compendium-cli-{version}-{runtime}.zip` (Windows)
- `compendium-web-{version}-{runtime}.tar.gz` (Linux/macOS)
- `compendium-web-{version}-{runtime}.zip` (Windows)

### Manual Packaging

```bash
# Publish first
./build.sh --target=PublishAll --configuration=Release --runtime=linux-x64

# Create archive
cd bin/cli
tar -czf ../../dist/compendium-cli-custom.tar.gz .
```

## Docker Build

### Build Image

```dockerfile
# Dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN ./build.sh --target=PublishAll --configuration=Release

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /src/bin/ ./bin/
ENTRYPOINT ["/app/bin/cli/Compendium.Cli"]
```

Build:

```bash
docker build -t compendium:latest .
```

### Run Container

```bash
# CLI
docker run -it --rm -v $(pwd)/my-catalog:/catalog compendium:latest chat --bundle /catalog

# Web UI
docker run -p 5050:5050 -v $(pwd)/my-catalog:/catalog compendium:web
```

## Development Workflow

### 1. Clone and Setup

```bash
git clone https://github.com/fuseraft/compendium.git
cd compendium
./build.sh --target=Build
```

### 2. Make Changes

Edit source files in `src/`.

### 3. Test

```bash
./build.sh --target=Test
```

### 4. Run Locally

```bash
# CLI
./bin/cli/Compendium.Cli --version

# Web
./bin/web/Compendium.Web
```

### 5. Format Code

```bash
./build.sh --target=Lint
```

### 6. Commit

```bash
git add .
git commit -m "Description of changes"
git push
```

## IDE Integration

### Visual Studio Code

Install recommended extensions:
- C# Dev Kit
- .NET Extension Pack

Tasks are pre-configured in `.vscode/tasks.json`:
- Press `Ctrl+Shift+B` to build
- Press `F5` to debug

### Visual Studio

Open `Compendium.sln` and press `F5` to build and debug.

### Rider

Open `Compendium.sln` and use the built-in build/debug tools.

## Troubleshooting

### "SDK not found" Error

**Problem:** `.NET SDK 10.x is required`

**Solution:** Install .NET 10 SDK from https://dotnet.microsoft.com/download/dotnet/10.0

### Build Fails on Windows

**Problem:** PowerShell script execution disabled

**Solution:** Enable script execution:

```powershell
Set-ExecutionPolicy -Scope CurrentUser -ExecutionPolicy RemoteSigned
```

### Permission Denied on Linux/macOS

**Problem:** `./build.sh: Permission denied`

**Solution:** Make script executable:

```bash
chmod +x build.sh
./build.sh
```

### Out of Disk Space

**Problem:** Build fails with "No space left on device"

**Solution:** Clean intermediate files:

```bash
./build.sh --target=Clean
rm -rf bin/ obj/
```

### Dependency Resolution Fails

**Problem:** NuGet restore errors

**Solution:** Clear package cache:

```bash
dotnet nuget locals all --clear
./build.sh --target=Build
```

## Performance Tips

### Faster Incremental Builds

Use `dotnet build` directly for faster incremental builds during development:

```bash
dotnet build src/Compendium.Cli/
```

### Parallel Builds

Enable parallel builds (default, but can be explicit):

```bash
dotnet build -m
```

### Skip Tests During Development

```bash
./build.sh --target=PublishCli  # Skips tests
```

Run tests separately when needed:

```bash
./build.sh --target=Test
```

## Continuous Integration

Example GitHub Actions workflow:

```yaml
name: Build and Test

on: [push, pull_request]

jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '10.0.x'
      - name: Build
        run: ./build.sh --target=Build --configuration=Release
      - name: Test
        run: ./build.sh --target=Test
      - name: Pack
        run: ./build.sh --target=Pack --runtime=linux-x64
      - name: Upload artifacts
        uses: actions/upload-artifact@v3
        with:
          name: compendium-cli
          path: dist/*.tar.gz
```

## Next Steps

- [Architecture Overview](architecture.md) — Understand the codebase structure
- [Contributing Guide](contributing.md) — Submit changes
- [Development Setup](../getting-started/installation.md) — First-time setup
