using System.Net.Http.Headers;
using System.Text.Json;
using Compendium.Agent.KeyStore;
using Compendium.Agent.Storage;

namespace Compendium.Web.Services;

public class LlmConfigService
{
    private readonly IApiKeyStore _keyStore;
    private string? _baseUrl;
    private string? _apiKey;
    private string? _modelName;

    public string? BaseUrl => _baseUrl;
    public string? ApiKey => _apiKey;
    public string? ModelName => _modelName;

    public bool IsConfigured => !string.IsNullOrEmpty(_baseUrl) &&
                               !string.IsNullOrEmpty(_apiKey) &&
                               !string.IsNullOrEmpty(_modelName);

    public LlmConfigService()
    {
        _keyStore = ApiKeyStoreFactory.Create();
    }

    public async Task LoadAsync()
    {
        // Load from persistent storage
        var config = LlmConfigStore.Load();
        if (config != null)
        {
            _baseUrl = config.BaseUrl;
            _modelName = config.ModelName;
            _apiKey = await _keyStore.RetrieveAsync();
        }
    }

    public void LoadFromConfiguration(IConfiguration configuration)
    {
        // Fallback to appsettings.json if no persistent config
        if (!IsConfigured)
        {
            _baseUrl = configuration["LLM:BaseUrl"]?.TrimEnd('/');
            _apiKey = configuration["LLM:ApiKey"];
            _modelName = configuration["LLM:ModelName"];
        }
    }

    public async Task ConfigureAsync(string baseUrl, string apiKey, string modelName)
    {
        _baseUrl = baseUrl?.TrimEnd('/');
        _apiKey = apiKey;
        _modelName = modelName;

        // Persist to disk
        var config = new LlmConfig
        {
            BaseUrl = _baseUrl!,
            ModelName = _modelName!
        };
        LlmConfigStore.Save(config);

        // Store API key in secure storage
        await _keyStore.StoreAsync(apiKey);
    }

    public string GetKeyStoreInfo()
    {
        return $"{_keyStore.StoreName}";
    }

    public async Task<List<string>> FetchAvailableModelsAsync(string endpoint, string apiKey, CancellationToken cancellationToken = default)
    {
        var normalizedEndpoint = endpoint.TrimEnd('/');

        // Detect if this is Ollama by checking the URL or trying Ollama's endpoint first
        var isOllama = endpoint.Contains("11434") || endpoint.Contains("ollama");
        var url = isOllama ? $"{normalizedEndpoint}/api/tags" : $"{normalizedEndpoint}/models";

        using var http = new HttpClient();
        http.Timeout = TimeSpan.FromSeconds(10);

        if (!string.IsNullOrEmpty(apiKey))
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        HttpResponseMessage response;
        try
        {
            response = await http.GetAsync(url, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            throw new InvalidOperationException($"Could not connect to {url}: {ex.Message}", ex);
        }
        catch (TaskCanceledException)
        {
            throw new InvalidOperationException($"Request to {url} timed out. Is the server running?");
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var snippet = body.Length > 200 ? body[..200] + "…" : body;

            // If Ollama endpoint failed, try OpenAI-style endpoint
            if (isOllama && response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return await FetchOpenAIStyleModels(normalizedEndpoint, apiKey, cancellationToken);
            }

            throw new InvalidOperationException($"HTTP {(int)response.StatusCode}: {snippet}");
        }

        try
        {
            var json = JsonDocument.Parse(body);
            return isOllama
                ? ExtractOllamaModels(json)
                : ExtractOpenAIModels(json);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Could not parse models response: {ex.Message}", ex);
        }
    }

    private async Task<List<string>> FetchOpenAIStyleModels(string endpoint, string apiKey, CancellationToken cancellationToken)
    {
        var url = $"{endpoint}/models";
        using var http = new HttpClient();
        http.Timeout = TimeSpan.FromSeconds(10);

        if (!string.IsNullOrEmpty(apiKey))
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        var response = await http.GetAsync(url, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var snippet = body.Length > 200 ? body[..200] + "…" : body;
            throw new InvalidOperationException($"HTTP {(int)response.StatusCode}: {snippet}");
        }

        var json = JsonDocument.Parse(body);
        return ExtractOpenAIModels(json);
    }

    private List<string> ExtractOllamaModels(JsonDocument json)
    {
        return json.RootElement.GetProperty("models")
            .EnumerateArray()
            .Select(m => m.TryGetProperty("name", out var n) ? n.GetString() : null)
            .Where(id => !string.IsNullOrEmpty(id))
            .Select(id => id!)
            .OrderBy(id => id)
            .ToList();
    }

    private List<string> ExtractOpenAIModels(JsonDocument json)
    {
        return json.RootElement.GetProperty("data")
            .EnumerateArray()
            .Select(m => m.TryGetProperty("id", out var n) ? n.GetString() : null)
            .Where(id => !string.IsNullOrEmpty(id))
            .Select(id => id!)
            .OrderBy(id => id)
            .ToList();
    }
}
