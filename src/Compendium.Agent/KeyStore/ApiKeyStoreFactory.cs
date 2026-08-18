namespace Compendium.Agent.KeyStore;

public static class ApiKeyStoreFactory
{
    public static IApiKeyStore Create()
    {
        if (OperatingSystem.IsWindows())
            return new WindowsCredentialManagerStore();

        // TODO: Add macOS Keychain and Linux SecretTool support
        // For now, fallback to plain text on non-Windows platforms
        return new PlainTextKeyStore();
    }
}
