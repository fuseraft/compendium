using Compendium.Okf;

namespace Compendium.Ingest.Tests;

public class IngestionPipelineTests
{
    [Fact]
    public void IngestsSupportedFilesAndSkipsUnsupportedOnes()
    {
        var sourceDir = CreateTempDir();
        var bundleDir = CreateTempDir();
        try
        {
            File.WriteAllText(Path.Combine(sourceDir, "notes.txt"), "Some notes.");
            File.WriteAllText(Path.Combine(sourceDir, "data.csv"), "Name,Owner\nWidget,Team A\nGadget,Team B");
            File.WriteAllText(Path.Combine(sourceDir, "unsupported.bin"), "binary-ish");

            var result = new IngestionPipeline().Ingest(sourceDir, bundleDir);

            Assert.Equal(2, result.FilesProcessed);
            Assert.Equal(3, result.ConceptsWritten); // 1 from notes.txt + 2 rows from data.csv
            Assert.Single(result.SkippedFiles);
            Assert.Empty(result.FailedFiles);

            Assert.True(File.Exists(Path.Combine(bundleDir, "references", "notes.txt")));
            Assert.True(File.Exists(Path.Combine(bundleDir, "references", "data.csv")));

            var bundle = BundleLoader.LoadBundle(bundleDir);
            Assert.Equal(3, bundle.Concepts.Count);

            var widget = bundle.Concepts.Values.Single(c => c.Title == "Widget");
            Assert.Equal("Document", widget.Type);
            Assert.Equal("draft", widget.Frontmatter["status"]);
        }
        finally
        {
            Directory.Delete(sourceDir, recursive: true);
            Directory.Delete(bundleDir, recursive: true);
        }
    }

    [Fact]
    public void UsesTypeOptionForConceptFolderAndFrontmatter()
    {
        var sourceDir = CreateTempDir();
        var bundleDir = CreateTempDir();
        try
        {
            File.WriteAllText(Path.Combine(sourceDir, "report.txt"), "A report.");

            var result = new IngestionPipeline().Ingest(sourceDir, bundleDir, conceptType: "Report");

            Assert.Equal(1, result.ConceptsWritten);
            Assert.True(Directory.Exists(Path.Combine(bundleDir, "reports")));
        }
        finally
        {
            Directory.Delete(sourceDir, recursive: true);
            Directory.Delete(bundleDir, recursive: true);
        }
    }

    private static string CreateTempDir()
    {
        var path = Path.Combine(Path.GetTempPath(), "compendium-ingest-tests-" + Guid.NewGuid());
        Directory.CreateDirectory(path);
        return path;
    }
}
