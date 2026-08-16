using Compendium.Okf;

namespace Compendium.Okf.Tests;

public class OkfParserTests
{
    [Fact]
    public void ParsesFrontmatterAndBody()
    {
        var text = "---\ntype: System\ntitle: Foo\n---\n# Overview\nHello.\n";

        var (frontmatter, body) = OkfParser.ParseConcept(text);

        Assert.Equal("System", frontmatter["type"]);
        Assert.Equal("Foo", frontmatter["title"]);
        Assert.Equal("# Overview\nHello.\n", body);
    }

    [Fact]
    public void MissingFrontmatterRaises()
    {
        Assert.Throws<ParseError>(() => OkfParser.ParseConcept("# Just a heading\n"));
    }

    [Fact]
    public void UnterminatedFrontmatterRaises()
    {
        Assert.Throws<ParseError>(() => OkfParser.ParseConcept("---\ntype: System\n"));
    }

    [Fact]
    public void PreservesUnknownFields()
    {
        var text = "---\ntype: System\nfoo: bar\n---\nBody\n";

        var (frontmatter, _) = OkfParser.ParseConcept(text);

        Assert.Equal("bar", frontmatter["foo"]);
    }
}
