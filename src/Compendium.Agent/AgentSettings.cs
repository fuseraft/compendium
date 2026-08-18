using Compendium.Agent.KeyStore;
using Compendium.Agent.Storage;

namespace Compendium.Agent;

public sealed class AgentSettings
{
    public required string BaseUrl { get; init; }
    public required string ApiKey { get; init; }
    public required string Model { get; init; }

    // Loads settings from the store shared with the Web UI's Settings page
    // (~/.compendium/llm-config.json + the OS credential store) — whichever
    // surface the user configured through, `compendium init` or the Web UI,
    // both read and write here. Falls back to LITELLM_* environment
    // variables for scripting/CI use. Returns null if neither is set.
    public static AgentSettings? TryLoad()
    {
        var persisted = LlmConfigStore.Load();
        if (persisted is { IsConfigured: true })
        {
            var apiKey = ApiKeyStoreFactory.Create().RetrieveAsync().GetAwaiter().GetResult();
            if (!string.IsNullOrEmpty(apiKey))
            {
                return new AgentSettings { BaseUrl = persisted.BaseUrl, ApiKey = apiKey, Model = persisted.ModelName };
            }
        }

        var baseUrl = Environment.GetEnvironmentVariable("LITELLM_BASE_URL");
        var envApiKey = Environment.GetEnvironmentVariable("LITELLM_API_KEY");
        if (!string.IsNullOrEmpty(baseUrl) && !string.IsNullOrEmpty(envApiKey))
        {
            var model = Environment.GetEnvironmentVariable("LITELLM_MODEL") ?? "anthropic.claude-sonnet-5";
            return new AgentSettings { BaseUrl = baseUrl, ApiKey = envApiKey, Model = model };
        }

        return null;
    }
}
