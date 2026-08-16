# Installation

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Git (for version control)
- An OpenAI-compatible LLM provider (OpenAI, Azure OpenAI, litellm proxy, etc.)

## From Source

### Linux/macOS

```bash
git clone https://github.com/fuseraft/compendium.git
cd compendium
./build.sh
```

The CLI will be available at `./bin/cli/Compendium.Cli`.

### Windows

```powershell
git clone https://github.com/fuseraft/compendium.git
cd compendium
.\build.ps1
```

The CLI will be available at `.\bin\cli\Compendium.Cli.exe`.

## From GitHub Releases

Download pre-built binaries from the [releases page](https://github.com/fuseraft/compendium/releases).

### Linux/macOS

```bash
# Download and extract
curl -L -o compendium-cli.tar.gz https://github.com/fuseraft/compendium/releases/latest/download/compendium-cli-<version>-<platform>.tar.gz
tar -xzf compendium-cli.tar.gz

# Run
./Compendium.Cli --version
```

### Windows

```powershell
# Download and extract
Invoke-WebRequest -Uri "https://github.com/fuseraft/compendium/releases/latest/download/compendium-cli-<version>-win-x64.zip" -OutFile compendium-cli.zip
Expand-Archive compendium-cli.zip

# Run
.\Compendium.Cli.exe --version
```

## Add to PATH (Optional)

### Linux/macOS

```bash
# Add to ~/.bashrc or ~/.zshrc
export PATH="$PATH:/path/to/compendium/bin/cli"
```

### Windows

Add `C:\path\to\compendium\bin\cli` to your system PATH environment variable.

## Verify Installation

```bash
compendium --version
```

## Next Steps

- [Quick Start Guide](quickstart.md) — Set up your first bundle
- [Configuration](configuration.md) — Configure LLM provider
