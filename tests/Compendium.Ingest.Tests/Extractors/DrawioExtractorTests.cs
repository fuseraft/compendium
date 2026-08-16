using System.IO.Compression;
using System.Text;
using Compendium.Ingest.Extractors;

namespace Compendium.Ingest.Tests.Extractors;

public class DrawioExtractorTests
{
    [Fact]
    public void ExtractsShapesAndConnectionsFromUncompressedFixture()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Fixtures", "fixture.drawio");

        var records = new DrawioExtractor().Extract(path);

        Assert.Single(records);
        Assert.Equal("Architecture", records[0].Title);
        Assert.Contains("Load Balancer", records[0].Text);
        Assert.Contains("- Load Balancer -> Web Server (label: routes)", records[0].Text);
        Assert.Contains("- Web Server -> Database (label: reads/writes)", records[0].Text);
        Assert.Equal("3", records[0].Metadata["shape_count"]);
        Assert.Equal("2", records[0].Metadata["connector_count"]);
    }

    // draw.io's default save format deflates+base64-encodes the URI-encoded
    // XML instead of storing it raw. There's no real drawio-application
    // export available in this environment to verify against, so this test
    // only proves the extractor's Decompress step correctly reverses the
    // same encoding drawio's own JS pipeline documents
    // (encodeURIComponent -> raw deflate -> base64) — a self-consistency
    // round trip, not validation against a real compressed export.
    [Fact]
    public void ExtractsShapesFromCompressedDiagram()
    {
        const string xml = """
            <mxGraphModel>
              <root>
                <mxCell id="0" />
                <mxCell id="1" parent="0" />
                <mxCell id="a" value="A" style="rounded=0;" vertex="1" parent="1" />
                <mxCell id="b" value="B" style="rounded=0;" vertex="1" parent="1" />
                <mxCell id="e1" value="links" edge="1" parent="1" source="a" target="b" />
              </root>
            </mxGraphModel>
            """;

        var compressed = Compress(xml);
        var path = Path.GetTempFileName() + ".drawio";
        File.WriteAllText(path, $"""<mxfile><diagram name="Compressed">{compressed}</diagram></mxfile>""");

        try
        {
            var records = new DrawioExtractor().Extract(path);

            Assert.Single(records);
            Assert.Equal("Compressed", records[0].Title);
            Assert.Contains("Shapes: A, B", records[0].Text);
            Assert.Contains("- A -> B (label: links)", records[0].Text);
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static string Compress(string xml)
    {
        var uriEncoded = Uri.EscapeDataString(xml);
        var bytes = Encoding.UTF8.GetBytes(uriEncoded);

        using var output = new MemoryStream();
        using (var deflate = new DeflateStream(output, CompressionLevel.Optimal, leaveOpen: true))
        {
            deflate.Write(bytes);
        }

        return Convert.ToBase64String(output.ToArray());
    }
}
