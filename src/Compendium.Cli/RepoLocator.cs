namespace Compendium.Cli;

// Locates repo-relative paths (.env, the sample bundle) when running from
// a source checkout, using Compendium.slnx as the root marker. Neither
// exists in an installed/published build (see install.sh — the release
// archive ships only the publish output), so every member here is
// best-effort: callers must treat a null/next-to-exe result as normal,
// not as an error.
public static class RepoLocator
{
    public static string? TryFindRepoRoot()
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

        return null;
    }

    // .env next to the repo root when running from source, or next to the
    // running executable otherwise. Always optional — callers should
    // check File.Exists before loading.
    public static string EnvPath() =>
        Path.Combine(TryFindRepoRoot() ?? AppContext.BaseDirectory, ".env");

    // The repo's sample bundle. Only exists in a source checkout — an
    // installed build ships no bundle, so callers must require --bundle
    // explicitly when this returns null.
    public static string? DefaultBundlePath() =>
        TryFindRepoRoot() is { } root ? Path.Combine(root, "catalog", "sample") : null;
}
