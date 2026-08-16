namespace Compendium.Cli;

// Locates repo-relative paths (.env, the sample bundle) regardless of
// where the built exe runs from, using Compendium.slnx as the root marker.
public static class RepoLocator
{
    public static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Compendium.slnx")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException(
            "Could not locate the Compendium repo root (no Compendium.slnx found above the running executable).");
    }

    public static string EnvPath() => Path.Combine(FindRepoRoot(), ".env");

    public static string DefaultBundlePath() => Path.Combine(FindRepoRoot(), "catalog", "sample");
}
