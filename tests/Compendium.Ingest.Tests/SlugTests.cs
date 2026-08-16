namespace Compendium.Ingest.Tests;

public class SlugTests
{
    [Fact]
    public void LowercasesAndDashesNonAlphanumerics()
    {
        Assert.Equal("hello-world", Slug.Of("Hello, World!"));
    }

    [Fact]
    public void FallsBackToItemForEmptyInput()
    {
        Assert.Equal("item", Slug.Of("###"));
    }

    [Fact]
    public void DedupesWithNumericSuffix()
    {
        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        Assert.Equal("foo", Slug.Unique("Foo", used));
        Assert.Equal("foo-2", Slug.Unique("Foo", used));
        Assert.Equal("foo-3", Slug.Unique("Foo", used));
    }
}
