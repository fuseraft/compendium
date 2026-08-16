using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using DotNetEnv;

namespace Compendium.Cli;

// Interactive setup: prompts for the provider base URL, API key, and
// model (offering a pick-list fetched from the provider's /models
// endpoint when available), then writes .env.
public static class InitCommand
{
    public static async Task<int> RunAsync()
    {
        var envPath = RepoLocator.EnvPath();
        if (File.Exists(envPath))
        {
            Env.Load(envPath);
        }

        Console.WriteLine("Compendium setup — configure your model provider.");
        Console.WriteLine();

        var baseUrl = PromptBaseUrl(Environment.GetEnvironmentVariable("LITELLM_BASE_URL"));
        var apiKey = PromptApiKey(Environment.GetEnvironmentVariable("LITELLM_API_KEY"));
        var model = await PromptModelAsync(baseUrl, apiKey, Environment.GetEnvironmentVariable("LITELLM_MODEL"));

        await File.WriteAllTextAsync(
            envPath,
            $"LITELLM_BASE_URL={baseUrl}\nLITELLM_API_KEY={apiKey}\nLITELLM_MODEL={model}\n");

        Console.WriteLine();
        Console.WriteLine($"Saved to {envPath}");
        Console.WriteLine("Run `dotnet run --project src/Compendium.Cli -- chat` to start.");
        return 0;
    }

    private static string PromptBaseUrl(string? current)
    {
        while (true)
        {
            Console.Write(string.IsNullOrEmpty(current) ? "Provider base URL: " : $"Provider base URL [{current}]: ");
            var input = Console.ReadLine()?.Trim();
            var value = string.IsNullOrEmpty(input) ? current : input;

            if (!string.IsNullOrEmpty(value) && Uri.TryCreate(value, UriKind.Absolute, out _))
            {
                return value.TrimEnd('/');
            }

            Console.WriteLine("Enter a valid absolute URL, e.g. https://litellm.example.com");
        }
    }

    private static string PromptApiKey(string? current)
    {
        var hasCurrent = !string.IsNullOrEmpty(current);
        while (true)
        {
            Console.Write(hasCurrent ? "API key [leave blank to keep existing]: " : "API key: ");
            var input = ReadSecret();

            if (!string.IsNullOrEmpty(input))
            {
                return input;
            }

            if (hasCurrent)
            {
                return current!;
            }

            Console.WriteLine("An API key is required.");
        }
    }

    private static string ReadSecret()
    {
        if (Console.IsInputRedirected)
        {
            return Console.ReadLine()?.Trim() ?? string.Empty;
        }

        var value = new StringBuilder();
        while (true)
        {
            var key = Console.ReadKey(intercept: true);
            if (key.Key == ConsoleKey.Enter)
            {
                Console.WriteLine();
                break;
            }

            if (key.Key == ConsoleKey.Backspace)
            {
                if (value.Length > 0)
                {
                    value.Remove(value.Length - 1, 1);
                    Console.Write("\b \b");
                }

                continue;
            }

            if (!char.IsControl(key.KeyChar))
            {
                value.Append(key.KeyChar);
                Console.Write('*');
            }
        }

        return value.ToString();
    }

    private static async Task<string> PromptModelAsync(string baseUrl, string apiKey, string? current)
    {
        var models = await TryFetchModelsAsync(baseUrl, apiKey);

        if (models.Count > 0)
        {
            Console.WriteLine("Available models:");
            for (var i = 0; i < models.Count; i++)
            {
                Console.WriteLine($"  {i + 1}. {models[i]}");
            }
        }
        else
        {
            Console.WriteLine("Couldn't fetch a model list from the provider — enter a model id manually.");
        }

        while (true)
        {
            Console.Write(string.IsNullOrEmpty(current) ? "Model (number or id): " : $"Model (number or id) [{current}]: ");
            var input = Console.ReadLine()?.Trim();

            if (string.IsNullOrEmpty(input))
            {
                if (!string.IsNullOrEmpty(current))
                {
                    return current;
                }

                Console.WriteLine("A model id is required.");
                continue;
            }

            if (int.TryParse(input, out var index) && index >= 1 && index <= models.Count)
            {
                return models[index - 1];
            }

            return input;
        }
    }

    private static async Task<List<string>> TryFetchModelsAsync(string baseUrl, string apiKey)
    {
        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            using var response = await client.GetAsync($"{baseUrl}/models");
            if (!response.IsSuccessStatusCode)
            {
                return [];
            }

            var json = await response.Content.ReadAsStringAsync();
            return ParseModelIds(json);
        }
        catch (Exception)
        {
            return [];
        }
    }

    public static List<string> ParseModelIds(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Array)
            {
                return [];
            }

            var ids = new List<string>();
            foreach (var entry in data.EnumerateArray())
            {
                if (entry.TryGetProperty("id", out var idProp) && idProp.ValueKind == JsonValueKind.String)
                {
                    var id = idProp.GetString();
                    if (!string.IsNullOrEmpty(id))
                    {
                        ids.Add(id);
                    }
                }
            }

            ids.Sort(StringComparer.OrdinalIgnoreCase);
            return ids;
        }
        catch (JsonException)
        {
            return [];
        }
    }
}
