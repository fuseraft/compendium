using Compendium.Ingest.Extractors;

namespace Compendium.Ingest.Tests.Extractors;

public class ArchimateExtractorTests
{
    [Fact]
    public void ExtractsOneRecordPerConceptElementExcludingViews()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Fixtures", "fixture.archimate");

        var records = new ArchimateExtractor().Extract(path);

        Assert.Equal(4, records.Count);
        Assert.DoesNotContain(records, r => r.Title == "Overview");
    }

    [Fact]
    public void RendersTypeLayerAndRelationshipsInBothDirections()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Fixtures", "fixture.archimate");
        var records = new ArchimateExtractor().Extract(path);

        var orderService = records.Single(r => r.Title == "Order Service");
        Assert.Contains("Type: ApplicationComponent (layer: application)", orderService.Text);
        Assert.Contains("- Order Service -> Customer (serves)", orderService.Text);
        Assert.Contains("- Payment Gateway -> Order Service (Serving)", orderService.Text);
        Assert.Contains("- App Server -> Order Service (Assignment)", orderService.Text);
        Assert.Equal("3", orderService.Metadata["relationship_count"]);
        Assert.Equal("ApplicationComponent", orderService.Metadata["archimate_type"]);
        Assert.Equal("application", orderService.Metadata["layer"]);

        var customer = records.Single(r => r.Title == "Customer");
        Assert.Contains("- Order Service -> Customer (serves)", customer.Text);
        Assert.Equal("1", customer.Metadata["relationship_count"]);
    }

    [Fact]
    public void FallsBackToRelationshipTypeNameWhenUnnamed()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Fixtures", "fixture.archimate");
        var records = new ArchimateExtractor().Extract(path);

        var paymentGateway = records.Single(r => r.Title == "Payment Gateway");
        Assert.Contains("- Payment Gateway -> Order Service (Serving)", paymentGateway.Text);
    }
}
