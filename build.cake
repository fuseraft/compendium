///////////////////////////////////////////////////////////////////////////////
// Compendium Cake Build Script
//
// Usage (after running `dotnet tool restore`):
//   dotnet cake build.cake                              # Default (Publish CLI)
//   dotnet cake build.cake --target=Build
//   dotnet cake build.cake --target=PublishWeb
//   dotnet cake build.cake --target=Pack --runtime=linux-x64
//   dotnet cake build.cake --configuration=Debug
//   dotnet cake build.cake --target=Lint
//
// Or via the bootstrappers:
//   ./build.sh [--target=X] [--configuration=Y] [--runtime=Z]
//   .\build.ps1 [-Target X] [-Configuration Y] [-Runtime Z]
///////////////////////////////////////////////////////////////////////////////

// Arguments
var target        = Argument("target",        "Default");
var configuration = Argument("configuration", "Release");
var runtime       = Argument("runtime",       "");          // e.g. "linux-x64"
var skipTests     = Argument("skipTests",     false);

// Paths
var cliProject     = "src/Compendium.Cli/Compendium.Cli.csproj";
var webProject     = "src/Compendium.Web/Compendium.Web.csproj";
var artifactsDir   = Directory("artifacts");
var publishCliDir  = Directory("bin/cli");
var publishWebDir  = Directory("bin/web");
var packDir        = artifactsDir + Directory("packages");
var testResultsDir = artifactsDir + Directory("test-results");

// Version — computed once at script start from minver-cli so every task uses the same value.
var version = GetVersion();

// Helpers

// Ask minver-cli for the exact version it will stamp into the assembly.
// minver-cli is registered as a local dotnet tool in .config/dotnet-tools.json
// and restored by build.sh before Cake runs.
string GetVersion()
{
    try
    {
        IEnumerable<string> lines;
        var exit = StartProcess("dotnet", new ProcessSettings
        {
            Arguments = "minver --tag-prefix v",
            RedirectStandardOutput = true,
            RedirectStandardError  = true   // suppress "no tags" warnings
        }, out lines);

        if (exit == 0)
        {
            var v = string.Concat(lines).Trim();
            if (!string.IsNullOrEmpty(v)) return v;
        }
    }
    catch { /* minver unavailable — fall through */ }

    return "0.0.1";
}

string GetGitHash()
{
    try
    {
        IEnumerable<string> lines;
        if (StartProcess("git", new ProcessSettings
        {
            Arguments = "rev-parse --short HEAD",
            RedirectStandardOutput = true
        }, out lines) == 0)
            return string.Concat(lines).Trim();
    }
    catch { /* git unavailable */ }
    return "unknown";
}

// Lifecycle hooks
Setup(ctx =>
{
    Information("╔══════════════════════════════════════════════════════╗");
    Information("║           Compendium · OKF Knowledge Catalog         ║");
    Information("╠══════════════════════════════════════════════════════╣");
    Information($"║  Version        {version,-37}║");
    Information($"║  Configuration  {configuration,-37}║");
    Information($"║  Runtime        {(string.IsNullOrEmpty(runtime) ? "(framework-dependent)" : runtime),-37}║");
    Information($"║  Target         {target,-37}║");
    Information($"║  Git commit     {GetGitHash(),-37}║");
    Information("╚══════════════════════════════════════════════════════╝");
});

Teardown(ctx =>
{
    if (ctx.Successful)
        Information($"\n✓  '{target}' succeeded.");
    else
        Error($"\n✗  '{target}' failed: {ctx.ThrownException?.Message}");
});

///////////////////////////////////////////////////////////////////////////////
// TASKS
///////////////////////////////////////////////////////////////////////////////

//
// Clean: removes artifacts/ and dotnet bin/obj trees
//
Task("Clean")
    .Description("Remove build artifacts and clean dotnet output directories.")
    .Does(() =>
    {
        if (DirectoryExists(artifactsDir))
            CleanDirectory(artifactsDir);

        if (DirectoryExists(publishCliDir))
            CleanDirectory(publishCliDir);

        if (DirectoryExists(publishWebDir))
            CleanDirectory(publishWebDir);

        foreach (var project in new[] { cliProject, webProject })
        {
            DotNetClean(project, new DotNetCleanSettings
            {
                Configuration = configuration,
                Verbosity     = DotNetVerbosity.Minimal
            });
        }

        Information("Clean complete.");
    });

//
// Restore: fetch NuGet packages
//
Task("Restore")
    .Description("Restore NuGet packages.")
    .IsDependentOn("Clean")
    .Does(() =>
    {
        foreach (var project in new[] { cliProject, webProject })
        {
            DotNetRestore(project, new DotNetRestoreSettings
            {
                Verbosity = DotNetVerbosity.Minimal
            });
        }

        foreach (var testProject in GetFiles("tests/**/*.csproj"))
            DotNetRestore(testProject.ToString(), new DotNetRestoreSettings
            {
                Verbosity = DotNetVerbosity.Minimal
            });

        Information("Restore complete.");
    });

//
// Build: compile projects in the requested configuration
//
Task("Build")
    .Description("Compile all projects.")
    .IsDependentOn("Restore")
    .Does(() =>
    {
        foreach (var project in new[] { cliProject, webProject })
        {
            DotNetBuild(project, new DotNetBuildSettings
            {
                Configuration = configuration,
                NoRestore     = true,
                Verbosity     = DotNetVerbosity.Minimal,
                MSBuildSettings = new DotNetMSBuildSettings()
                    .WithProperty("Version",            version)
                    .WithProperty("InformationalVersion", version)
                    .WithProperty("SourceRevisionId",   GetGitHash())
                    .WithProperty("MinVerSkip",         "true")   // minver-cli already computed the version above
            });
        }

        Information("Build complete.");
    });

//
// Test: discover and run all test projects under tests/
//
Task("Test")
    .Description("Run all test projects found under tests/.")
    .IsDependentOn("Build")
    .Does(() =>
    {
        if (skipTests)
        {
            Warning("--skipTests flag is set. Skipping.");
            return;
        }

        var testProjects = GetFiles("tests/**/*.csproj");

        if (!testProjects.Any())
        {
            Warning("No test projects found under tests/. Skipping.");
            return;
        }

        EnsureDirectoryExists(testResultsDir);

        foreach (var testProject in testProjects)
        {
            Information($"Testing: {testProject.GetFilename()}");

            DotNetTest(testProject.ToString(), new DotNetTestSettings
            {
                Configuration    = configuration,
                NoRestore        = true,
                ResultsDirectory = testResultsDir,
                Loggers          = new[] { "trx" },
                Verbosity        = DotNetVerbosity.Minimal,
                MSBuildSettings  = new DotNetMSBuildSettings()
                    .WithProperty("Version",              version)
                    .WithProperty("InformationalVersion", version)
                    .WithProperty("SourceRevisionId",     GetGitHash())
                    .WithProperty("MinVerSkip",           "true")
            });
        }

        Information("Tests complete.");
    });

//
// PublishCli: produce a deployable CLI output
//
Task("PublishCli")
    .Description("Publish CLI to bin/cli/. Pass --runtime=<rid> for a self-contained binary.")
    .IsDependentOn("Test")
    .Does(() =>
    {
        EnsureDirectoryExists(publishCliDir);

        var settings = new DotNetPublishSettings
        {
            Configuration = configuration,
            OutputDirectory = publishCliDir,
            NoRestore     = true,
            NoBuild       = true,
            Verbosity     = DotNetVerbosity.Minimal,
            MSBuildSettings = new DotNetMSBuildSettings()
                .WithProperty("Version",            version)
                .WithProperty("InformationalVersion", version)
                .WithProperty("MinVerSkip",         "true")
        };

        if (!string.IsNullOrEmpty(runtime))
        {
            settings.NoRestore     = false;
            settings.NoBuild       = false;
            settings.Runtime       = runtime;
            settings.SelfContained = true;
            settings.MSBuildSettings
                .WithProperty("PublishSingleFile",                    "true")
                .WithProperty("IncludeNativeLibrariesForSelfExtract", "true")
                .WithProperty("EnableCompressionInSingleFile",        "true")
                .WithProperty("DebugType",                            "none")
                .WithProperty("DebugSymbols",                         "false");

            Information($"Self-contained single-file publish for: {runtime}");
        }
        else
        {
            Information("Framework-dependent publish (no --runtime specified).");
        }

        DotNetPublish(cliProject, settings);

        Information($"CLI publish complete → {publishCliDir}");
    });

//
// PublishWeb: produce a deployable web server output
//
Task("PublishWeb")
    .Description("Publish Web to bin/web/. Pass --runtime=<rid> for a self-contained binary.")
    .IsDependentOn("Test")
    .Does(() =>
    {
        EnsureDirectoryExists(publishWebDir);

        var settings = new DotNetPublishSettings
        {
            Configuration = configuration,
            OutputDirectory = publishWebDir,
            NoRestore     = true,
            NoBuild       = true,
            Verbosity     = DotNetVerbosity.Minimal,
            MSBuildSettings = new DotNetMSBuildSettings()
                .WithProperty("Version",            version)
                .WithProperty("InformationalVersion", version)
                .WithProperty("MinVerSkip",         "true")
        };

        if (!string.IsNullOrEmpty(runtime))
        {
            settings.NoRestore     = false;
            settings.NoBuild       = false;
            settings.Runtime       = runtime;
            settings.SelfContained = true;
            settings.MSBuildSettings
                .WithProperty("PublishSingleFile",                    "true")
                .WithProperty("IncludeNativeLibrariesForSelfExtract", "true")
                .WithProperty("EnableCompressionInSingleFile",        "true")
                .WithProperty("DebugType",                            "none")
                .WithProperty("DebugSymbols",                         "false");

            Information($"Self-contained single-file publish for: {runtime}");
        }
        else
        {
            Information("Framework-dependent publish (no --runtime specified).");
        }

        DotNetPublish(webProject, settings);

        Information($"Web publish complete → {publishWebDir}");
    });

//
// PublishAll: publish both CLI and Web
//
Task("PublishAll")
    .Description("Publish both CLI and Web projects.")
    .IsDependentOn("PublishCli")
    .IsDependentOn("PublishWeb");

//
// Pack: zip published outputs into versioned archives
//
Task("Pack")
    .Description("Zip the published outputs into versioned archives under artifacts/packages/.")
    .IsDependentOn("PublishAll")
    .Does(() =>
    {
        EnsureDirectoryExists(packDir);

        var rtSuffix = string.IsNullOrEmpty(runtime) ? "portable" : runtime;

        // Pack CLI
        var cliZipName = $"compendium-cli-{version}-{rtSuffix}.zip";
        var cliZipPath = packDir + File(cliZipName);
        Zip(publishCliDir, cliZipPath);
        var cliKb = new System.IO.FileInfo(cliZipPath.ToString()).Length / 1024;
        Information($"CLI package ready: {cliZipName}  ({cliKb:N0} KB)");

        // Pack Web
        var webZipName = $"compendium-web-{version}-{rtSuffix}.zip";
        var webZipPath = packDir + File(webZipName);
        Zip(publishWebDir, webZipPath);
        var webKb = new System.IO.FileInfo(webZipPath.ToString()).Length / 1024;
        Information($"Web package ready: {webZipName}  ({webKb:N0} KB)");
    });

//
// Lint: verify code formatting without modifying files
//
Task("Lint")
    .Description("Check code style with 'dotnet format --verify-no-changes'.")
    .Does(() =>
    {
        var projects = new[] { cliProject, webProject };

        foreach (var project in projects)
        {
            Information($"Linting {project}...");
            var exitCode = StartProcess("dotnet", new ProcessSettings
            {
                Arguments = $"format \"{project}\" --verify-no-changes --severity warn"
            });

            if (exitCode != 0)
                throw new CakeException(
                    $"Code formatting issues detected in {project}. Run 'dotnet format' to fix them.");
        }

        Information("Lint passed.");
    });

//
// Default: full pipeline publishing CLI only
//
Task("Default")
    .Description("Full pipeline: Clean → Restore → Build → Test → PublishCli.")
    .IsDependentOn("PublishCli");

RunTarget(target);
