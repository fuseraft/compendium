using Compendium.Ingest;

namespace Compendium.Cli;

public static class IngestCommand
{
    public static Task<int> RunAsync(string[] args)
    {
        var source = ParseOption(args, "--source");
        var bundle = ParseOption(args, "--bundle");
        var type = ParseOption(args, "--type") ?? "Document";

        if (source is null || bundle is null)
        {
            Console.WriteLine("Usage: compendium ingest --source <file-or-dir> --bundle <path> [--type <ConceptType>]");
            return Task.FromResult(1);
        }

        if (!File.Exists(source) && !Directory.Exists(source))
        {
            Console.WriteLine($"Source not found: {source}");
            return Task.FromResult(1);
        }

        var pipeline = new IngestionPipeline();
        var result = pipeline.Ingest(source, bundle, type);

        Console.WriteLine($"Processed {result.FilesProcessed} file(s), wrote {result.ConceptsWritten} concept(s) to {bundle}");

        if (result.SkippedFiles.Count > 0)
        {
            Console.WriteLine($"Skipped {result.SkippedFiles.Count} unsupported file(s):");
            foreach (var file in result.SkippedFiles)
            {
                Console.WriteLine($"  - {file}");
            }
        }

        if (result.FailedFiles.Count > 0)
        {
            Console.WriteLine($"Failed to ingest {result.FailedFiles.Count} file(s):");
            foreach (var (file, error) in result.FailedFiles)
            {
                Console.WriteLine($"  - {file}: {error}");
            }
        }

        return Task.FromResult(0);
    }

    private static string? ParseOption(string[] cliArgs, string name)
    {
        for (var i = 0; i < cliArgs.Length - 1; i++)
        {
            if (cliArgs[i] == name)
            {
                return cliArgs[i + 1];
            }
        }

        return null;
    }
}
