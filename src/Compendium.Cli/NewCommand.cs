using Compendium.Okf;

namespace Compendium.Cli;

public static class NewCommand
{
    public static Task<int> RunAsync(string[] args)
    {
        var path = args.Length > 1 ? args[1] : null;
        if (string.IsNullOrWhiteSpace(path))
        {
            Console.WriteLine("Usage: compendium new <path>");
            return Task.FromResult(1);
        }

        var result = BundleScaffold.Create(path, DateTime.UtcNow);
        Console.WriteLine(result.Message);
        if (!result.Success)
        {
            return Task.FromResult(1);
        }

        Console.WriteLine();
        Console.WriteLine("Next steps:");
        Console.WriteLine($"  cat {path}/index.md");
        Console.WriteLine($"  compendium ingest --source <path> --bundle {path}   # grow it from source docs");
        Console.WriteLine($"  compendium chat --bundle {path}                     # or start chatting with it");
        return Task.FromResult(0);
    }
}
