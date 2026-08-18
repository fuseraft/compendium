using Compendium.Okf;

namespace Compendium.Web.Services;

public class BundleService
{
    private Bundle? _bundle;
    private string? _bundlePath;

    public Bundle? CurrentBundle => _bundle;
    public string? BundlePath => _bundlePath;

    public void LoadBundle(string path)
    {
        _bundlePath = path;
        _bundle = BundleLoader.LoadBundle(path);
    }

    public void ReloadBundle()
    {
        if (_bundlePath != null)
        {
            _bundle = BundleLoader.LoadBundle(_bundlePath);
        }
    }

    public List<string> GetConceptTypes()
    {
        if (_bundle == null)
            return new List<string>();

        return _bundle.Concepts.Values
            .Select(c => c.Type)
            .Distinct()
            .OrderBy(t => t)
            .ToList();
    }

    // The bundle's .compendium/config.json spec, if it has one — read fresh
    // from disk each call since it isn't part of the in-memory Bundle model.
    public BundleConfig GetBundleConfig() =>
        _bundlePath is null ? BundleConfig.Unconstrained : BundleConfig.Load(_bundlePath);
}
