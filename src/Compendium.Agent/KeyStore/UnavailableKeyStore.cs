namespace Compendium.Agent.KeyStore;

/// <summary>
/// Thrown by <see cref="UnavailableKeyStore.StoreAsync"/> when a caller attempts to persist
/// an API key but no OS keychain is available. Compendium never falls back to writing secrets
/// to disk in plaintext — callers should catch this and point the user at the LITELLM_API_KEY
/// environment variable instead.
/// </summary>
public sealed class KeyStoreUnavailableException(string message) : Exception(message);

// Returned when no native OS keychain is reachable (e.g. Linux without a running secret
// service, or any platform where the native store threw). Compendium does not store API keys
// in plaintext on disk under any circumstances, so this store refuses to persist anything.
internal sealed class UnavailableKeyStore : IApiKeyStore
{
    public string StoreName => "no OS keychain available";

    public bool IsAvailable => false;

    public Task<string?> RetrieveAsync() => Task.FromResult<string?>(null);

    public Task StoreAsync(string apiKey) =>
        throw new KeyStoreUnavailableException(
            "No OS keychain is available on this system, and Compendium does not store API " +
            "keys in plaintext on disk. Set LITELLM_API_KEY (and LITELLM_BASE_URL, " +
            "LITELLM_MODEL) as environment variables instead — see the Configuration guide.");

    public Task DeleteAsync() => Task.CompletedTask;
}
