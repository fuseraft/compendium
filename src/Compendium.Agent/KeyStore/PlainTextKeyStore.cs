namespace Compendium.Agent.KeyStore;

/// <summary>
/// Fallback keystore that stores API keys in plain text in the user config directory.
/// Only used when platform-specific secure storage is unavailable.
/// </summary>
internal sealed class PlainTextKeyStore : IApiKeyStore
{
    private readonly string _keyFilePath;

    public PlainTextKeyStore()
    {
        var configDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".compendium"
        );
        Directory.CreateDirectory(configDir);
        _keyFilePath = Path.Combine(configDir, ".api-key");
    }

    public string StoreName => "plain text file (insecure fallback)";
    public bool IsAvailable => true;

    public Task<string?> RetrieveAsync()
    {
        try
        {
            if (!File.Exists(_keyFilePath))
                return Task.FromResult<string?>(null);
            var key = File.ReadAllText(_keyFilePath).Trim();
            return Task.FromResult<string?>(string.IsNullOrEmpty(key) ? null : key);
        }
        catch
        {
            return Task.FromResult<string?>(null);
        }
    }

    public Task StoreAsync(string apiKey)
    {
        File.WriteAllText(_keyFilePath, apiKey);
        return Task.CompletedTask;
    }

    public Task DeleteAsync()
    {
        try
        {
            if (File.Exists(_keyFilePath))
                File.Delete(_keyFilePath);
        }
        catch
        {
            // Best effort
        }
        return Task.CompletedTask;
    }
}
