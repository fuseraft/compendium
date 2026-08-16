# Contributing to Compendium

Thank you for considering contributing to Compendium! This guide will help you get started with contributing code, documentation, and ideas.

## Ways to Contribute

- **Code** — Bug fixes, new features, performance improvements
- **Documentation** — Improve guides, fix typos, add examples
- **Bug Reports** — Identify issues and edge cases
- **Feature Requests** — Propose new capabilities
- **Testing** — Try Compendium on your data and report findings
- **Examples** — Share bundle templates or workflows

## Getting Started

### 1. Fork and Clone

```bash
# Fork the repository on GitHub
# Then clone your fork
git clone https://github.com/YOUR_USERNAME/compendium.git
cd compendium
```

### 2. Set Up Development Environment

**Prerequisites:**
- .NET 10 SDK
- Git
- Your favorite editor (VS Code, Visual Studio, Rider)

**Build:**

```bash
./build.sh --target=Build  # Linux/macOS
.\build.ps1 -Target Build   # Windows
```

**Run Tests:**

```bash
./build.sh --target=Test
```

### 3. Create a Branch

```bash
git checkout -b feature/your-feature-name
```

Use descriptive branch names:
- `feature/sharepoint-connector`
- `fix/pdf-extraction-crash`
- `docs/update-installation-guide`

## Development Workflow

### 1. Make Changes

Edit files in `src/` or `docs/`.

### 2. Test Your Changes

```bash
# Run affected tests
dotnet test tests/Compendium.Core.Tests/

# Run all tests
./build.sh --target=Test

# Manual testing
./bin/cli/Compendium.Cli chat --bundle catalog/sample
```

### 3. Check Code Quality

```bash
# Format code
dotnet format

# Lint
./build.sh --target=Lint
```

### 4. Commit

Write clear commit messages:

```bash
git add .
git commit -m "Add SharePoint connector with OAuth2 auth"
```

**Good commit messages:**
- `Fix PDF extraction crash on malformed files`
- `Add data lineage graph visualization`
- `Update installation docs for macOS`

**Bad commit messages:**
- `fix bug`
- `updates`
- `wip`

### 5. Push and Create Pull Request

```bash
git push origin feature/your-feature-name
```

Open a pull request on GitHub with:
- **Title** — Clear, concise description
- **Description** — What changed and why
- **Screenshots** — For UI changes
- **Related Issues** — Link to issues this PR addresses

## Code Guidelines

### C# Style

Follow standard C# conventions:

```csharp
// Good
public class ConceptReader
{
    private readonly string _bundlePath;
    
    public ConceptReader(string bundlePath)
    {
        _bundlePath = bundlePath ?? throw new ArgumentNullException(nameof(bundlePath));
    }
    
    public async Task<Concept> ReadAsync(string id)
    {
        // Implementation
    }
}

// Bad
public class conceptreader
{
    public string bundlepath;
    
    public Concept read(string ID)
    {
        // Implementation
    }
}
```

**Key Points:**
- PascalCase for public members
- camelCase for parameters
- `_camelCase` for private fields
- Use `async`/`await` for I/O
- Throw `ArgumentNullException` for null parameters
- Add XML documentation for public APIs

### Project Structure

Place files in appropriate projects:

- **Core logic** → `Compendium.Core`
- **Agent tools** → `Compendium.Agent`
- **Ingestion readers** → `Compendium.Ingest/Readers`
- **CLI commands** → `Compendium.Cli/Commands`
- **Web pages** → `Compendium.Web/Pages`
- **Tests** → `tests/ProjectName.Tests`

### Testing

Write tests for new features:

```csharp
[Fact]
public void ConceptReader_ReadsConcept_Successfully()
{
    // Arrange
    var reader = new ConceptReader("catalog/sample");
    
    // Act
    var concept = reader.ReadAsync("systems/order-management").Result;
    
    // Assert
    Assert.NotNull(concept);
    Assert.Equal("Order Management System", concept.Title);
    Assert.Equal("System", concept.Type);
}
```

**Test Guidelines:**
- One test class per source class
- Descriptive test names (MethodName_Scenario_ExpectedBehavior)
- Arrange-Act-Assert pattern
- Test both success and error cases
- Use xUnit for tests

### Documentation

Document public APIs:

```csharp
/// <summary>
/// Reads an OKF concept from the bundle.
/// </summary>
/// <param name="id">Concept ID (e.g., "systems/order-management")</param>
/// <returns>The parsed concept</returns>
/// <exception cref="FileNotFoundException">If concept file doesn't exist</exception>
public async Task<Concept> ReadAsync(string id)
{
    // Implementation
}
```

Update docs when adding features:
- User-facing features → `docs/guide/` or `docs/features/`
- Developer features → `docs/development/`
- API changes → `docs/reference/api.md`

## Pull Request Process

### 1. Before Submitting

- [ ] Tests pass (`./build.sh --target=Test`)
- [ ] Code formatted (`dotnet format`)
- [ ] Documentation updated
- [ ] No merge conflicts with `main`
- [ ] Commit history is clean (squash if needed)

### 2. PR Description Template

```markdown
## Summary
Brief description of what this PR does.

## Changes
- Added X feature
- Fixed Y bug
- Updated Z documentation

## Testing
How this was tested:
- [ ] Unit tests added/updated
- [ ] Manual testing completed
- [ ] Integration tests pass

## Screenshots
(for UI changes)

## Related Issues
Closes #123
Relates to #456
```

### 3. Review Process

- Maintainers will review your PR
- Address feedback by pushing new commits
- Once approved, maintainers will merge

### 4. After Merge

- Delete your branch
- Pull latest `main`
- Your contribution is live!

## Feature Development Guidelines

### Adding a New Format Reader

1. Create `src/Compendium.Ingest/Readers/YourFormatReader.cs`

```csharp
public class YourFormatReader : IDocumentReader
{
    public bool CanRead(string extension)
    {
        return extension.Equals(".yourext", StringComparison.OrdinalIgnoreCase);
    }
    
    public async Task<DocumentContent> ReadAsync(string filePath)
    {
        // Extract content
        return new DocumentContent
        {
            Text = extractedText,
            Metadata = metadata
        };
    }
}
```

2. Register in `src/Compendium.Ingest/ReaderRegistry.cs`

3. Add tests in `tests/Compendium.Ingest.Tests/Readers/YourFormatReaderTests.cs`

4. Update `docs/guide/ingestion.md` with supported format

### Adding a New Agent Tool

1. Create `src/Compendium.Agent/Tools/YourTool.cs`

```csharp
public class YourTool : AgentTool
{
    public override string Name => "YourTool";
    public override string Description => "What this tool does";
    
    public override JsonObject Schema => new JsonObject
    {
        ["type"] = "object",
        ["properties"] = new JsonObject
        {
            ["param1"] = new JsonObject { ["type"] = "string" }
        },
        ["required"] = new JsonArray { "param1" }
    };
    
    public override async Task<string> ExecuteAsync(JsonObject parameters)
    {
        var param1 = parameters["param1"].GetValue<string>();
        // Implementation
        return result;
    }
}
```

2. Register in `src/Compendium.Agent/ToolRegistry.cs`

3. Add tests in `tests/Compendium.Agent.Tests/Tools/YourToolTests.cs`

4. Update `docs/features/agent.md` with tool documentation

### Adding a Web UI Page

1. Create `src/Compendium.Web/Pages/YourPage.razor`

```razor
@page "/your-page"
@inject BundleManager BundleManager

<h1>Your Page</h1>

@code {
    private List<Concept> concepts;
    
    protected override async Task OnInitializedAsync()
    {
        concepts = await BundleManager.ListConceptsAsync();
    }
}
```

2. Add navigation link in `src/Compendium.Web/Shared/NavMenu.razor`

3. Add tests (if applicable)

4. Update `docs/guide/web-ui.md`

## Bug Reports

### Before Reporting

- Search existing issues for duplicates
- Try latest version (`git pull`, rebuild)
- Collect reproduction steps

### Report Template

```markdown
## Description
Clear description of the bug.

## Steps to Reproduce
1. Run command `compendium ingest --source ...`
2. Observe error message
3. ...

## Expected Behavior
What should happen.

## Actual Behavior
What actually happens.

## Environment
- OS: Windows 11 / Ubuntu 22.04 / macOS 14
- .NET Version: 10.0.x
- Compendium Version: 0.1.0

## Logs/Screenshots
(attach if available)
```

## Feature Requests

### Before Requesting

- Check if feature already exists
- Search existing feature requests

### Request Template

```markdown
## Problem
What problem does this feature solve?

## Proposed Solution
How should this feature work?

## Alternatives
Other ways to solve this problem.

## Use Case
Real-world scenario where this is needed.
```

## Documentation Contributions

Documentation is as important as code!

### What to Document

- **Tutorials** — Step-by-step guides
- **How-tos** — Specific task instructions
- **Concepts** — Explain architecture and design
- **Reference** — API documentation

### Documentation Style

- **Clear and concise** — Short sentences
- **Examples** — Show, don't just tell
- **Screenshots** — For UI features
- **Code blocks** — With syntax highlighting
- **Links** — Cross-reference related docs

### Building Docs Locally

```bash
# Install MkDocs
pip install mkdocs-material mkdocs-git-revision-date-localized-plugin

# Serve locally
mkdocs serve

# Open http://localhost:8000
```

## Code of Conduct

Be respectful and welcoming:

- **Be kind** — Constructive feedback, not criticism
- **Be patient** — Everyone is learning
- **Be inclusive** — Welcome all contributors
- **Be professional** — Focus on the work, not the person

## Questions?

- **GitHub Issues** — For bugs and features
- **Discussions** — For questions and ideas
- **Pull Requests** — For code contributions

## License

By contributing, you agree that your contributions will be licensed under the same license as Compendium (see LICENSE file).

## Recognition

Contributors are recognized in:
- GitHub contributors page
- Release notes
- Project README

Thank you for contributing to Compendium!
