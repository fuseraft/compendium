using Compendium.Ingest;

namespace Compendium.Web.Services;

public class IngestionService
{
    private readonly string _tempUploadDir;

    public IngestionService()
    {
        _tempUploadDir = Path.Combine(Path.GetTempPath(), "compendium-uploads");
        Directory.CreateDirectory(_tempUploadDir);
    }

    public async Task<string> SaveUploadedFileAsync(Stream fileStream, string fileName)
    {
        var filePath = Path.Combine(_tempUploadDir, Guid.NewGuid().ToString() + Path.GetExtension(fileName));
        using var fileOutput = File.Create(filePath);
        await fileStream.CopyToAsync(fileOutput);
        return filePath;
    }

    public IngestionResult IngestFiles(IEnumerable<string> filePaths, string bundleRoot, string conceptType)
    {
        var pipeline = new IngestionPipeline();
        var allResults = new List<IngestionResult>();

        foreach (var filePath in filePaths)
        {
            var result = pipeline.Ingest(filePath, bundleRoot, conceptType);
            allResults.Add(result);
        }

        // Aggregate results
        return new IngestionResult(
            FilesProcessed: allResults.Sum(r => r.FilesProcessed),
            ConceptsWritten: allResults.Sum(r => r.ConceptsWritten),
            SkippedFiles: allResults.SelectMany(r => r.SkippedFiles).ToList(),
            FailedFiles: allResults.SelectMany(r => r.FailedFiles).ToList()
        );
    }

    public IngestionResult IngestDirectory(string directoryPath, string bundleRoot, string conceptType)
    {
        var pipeline = new IngestionPipeline();
        return pipeline.Ingest(directoryPath, bundleRoot, conceptType);
    }

    public void CleanupTempFile(string filePath)
    {
        try
        {
            if (File.Exists(filePath))
                File.Delete(filePath);
        }
        catch
        {
            // Best effort cleanup
        }
    }

    public IEnumerable<string> GetSupportedExtensions()
    {
        return new[]
        {
            ".txt", ".md", ".json", ".xml", ".csv",
            ".pdf", ".docx", ".xlsx",
            ".eml", ".msg", ".ost",
            ".drawio", ".vsdx", ".archimate"
        };
    }
}
