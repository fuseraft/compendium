using System.Text.Json;

namespace Compendium.Agent.Storage;

public static class LlmConfigStore
{
    private static string ConfigDir =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".compendium");

    public static string ConfigPath => Path.Combine(ConfigDir, "llm-config.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
    };

    public static LlmConfig? Load()
    {
        if (!File.Exists(ConfigPath))
            return null;

        try
        {
            var json = File.ReadAllText(ConfigPath);
            return JsonSerializer.Deserialize<LlmConfig>(json, JsonOptions);
        }
        catch
        {
            return null;
        }
    }

    public static void Save(LlmConfig config)
    {
        Directory.CreateDirectory(ConfigDir);
        var json = JsonSerializer.Serialize(config, JsonOptions);
        File.WriteAllText(ConfigPath, json);
    }

    public static void Delete()
    {
        try
        {
            if (File.Exists(ConfigPath))
                File.Delete(ConfigPath);
        }
        catch
        {
            // Best effort
        }
    }
}
