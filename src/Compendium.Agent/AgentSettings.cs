namespace Compendium.Agent;

public sealed class AgentSettings
{
    public required string BaseUrl { get; init; }
    public required string ApiKey { get; init; }
    public required string Model { get; init; }

    public static AgentSettings FromEnvironment()
    {
        var baseUrl = Environment.GetEnvironmentVariable("LITELLM_BASE_URL")
            ?? throw new InvalidOperationException("LITELLM_BASE_URL is not set.");
        var apiKey = Environment.GetEnvironmentVariable("LITELLM_API_KEY")
            ?? throw new InvalidOperationException("LITELLM_API_KEY is not set.");
        var model = Environment.GetEnvironmentVariable("LITELLM_MODEL") ?? "anthropic.claude-sonnet-5";

        return new AgentSettings { BaseUrl = baseUrl, ApiKey = apiKey, Model = model };
    }
}
