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
}
