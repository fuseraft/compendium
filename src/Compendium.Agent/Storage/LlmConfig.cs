using System.Text.Json.Serialization;

namespace Compendium.Agent.Storage;

public class LlmConfig
{
    [JsonPropertyName("baseUrl")]
    public string BaseUrl { get; set; } = string.Empty;

    [JsonPropertyName("modelName")]
    public string ModelName { get; set; } = string.Empty;

    public bool IsConfigured => !string.IsNullOrEmpty(BaseUrl) && !string.IsNullOrEmpty(ModelName);
}
