using Compendium.Agent;
using Compendium.Okf;
using DotNetEnv;

namespace Compendium.Cli;

public static class ChatCommand
{
    public static async Task<int> RunAsync(string[] args)
    {
        var envPath = RepoLocator.EnvPath();
        if (File.Exists(envPath))
        {
            Env.Load(envPath);
        }

        AgentSettings settings;
        try
        {
            settings = AgentSettings.FromEnvironment();
        }
        catch (InvalidOperationException)
        {
            Console.WriteLine("Not configured yet — run `dotnet run --project src/Compendium.Cli -- init` first.");
            return 1;
        }

        var bundlePath = ParseBundlePath(args) ?? RepoLocator.DefaultBundlePath();
        var allowWrite = args.Contains("--allow-write");
        var bundle = BundleLoader.LoadBundle(bundlePath);
        var tools = new CompendiumTools(bundle);
        var agent = CompendiumAgentFactory.Create(settings, tools, allowWrite);

        Console.WriteLine($"Compendium — {bundle.Concepts.Count} concept(s) loaded from {bundle.RootPath}");
        Console.WriteLine(allowWrite
            ? "Write access enabled — the agent may create/update concepts, add links, and flag concepts for review."
            : "Read-only session — pass --allow-write to let the agent create or modify concepts.");
        Console.WriteLine("Ask a question, or type 'exit' to quit.");

        var session = await agent.CreateSessionAsync();

        while (true)
        {
            Console.Write("\n> ");
            var input = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(input) || input.Trim().Equals("exit", StringComparison.OrdinalIgnoreCase))
            {
                break;
            }

            var response = await agent.RunAsync(input, session);
            Console.WriteLine(response.Text);
        }

        return 0;
    }

    private static string? ParseBundlePath(string[] cliArgs)
    {
        for (var i = 0; i < cliArgs.Length - 1; i++)
        {
            if (cliArgs[i] == "--bundle")
            {
                return cliArgs[i + 1];
            }
        }

        return null;
    }
}
