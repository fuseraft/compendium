using Compendium.Okf;

namespace Compendium.Okf.Tests;

public class BundleLoaderTests
{
    private static readonly string Sample = Path.Combine(
        AppContext.BaseDirectory, "..", "..", "..", "..", "..", "catalog", "sample");

    [Fact]
    public void LoadsAllConceptsExcludingReservedFiles()
    {
        var bundle = BundleLoader.LoadBundle(Sample);

        Assert.False(bundle.Concepts.ContainsKey("index"));
        Assert.True(bundle.Concepts.ContainsKey("systems/billing-service"));
        Assert.True(bundle.Concepts.ContainsKey("systems/inventory-service"));
        Assert.True(bundle.Concepts.ContainsKey("integrations/billing-to-inventory"));
        Assert.True(bundle.Concepts.ContainsKey("processes/order-fulfillment"));
    }

    [Fact]
    public void ConceptFrontmatterFields()
    {
        var bundle = BundleLoader.LoadBundle(Sample);
        var billing = bundle.Concepts["systems/billing-service"];

        Assert.Equal("System", billing.Type);
        Assert.Equal("Billing Service", billing.Title);
    }

    [Fact]
    public void OutgoingLinksResolveWithinBundle()
    {
        var bundle = BundleLoader.LoadBundle(Sample);

        var links = Links.OutgoingLinks(bundle, "processes/order-fulfillment");

        Assert.Contains("systems/billing-service", links);
        Assert.Contains("integrations/billing-to-inventory", links);
    }

    [Fact]
    public void BrokenLinkIsTolerated()
    {
        var bundle = BundleLoader.LoadBundle(Sample);

        var resolved = Links.ResolveLink(bundle, "systems/billing-service", "/systems/does-not-exist.md");

        Assert.Null(resolved);
    }
}
