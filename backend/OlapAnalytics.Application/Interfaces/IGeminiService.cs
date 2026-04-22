using OlapAnalytics.Application.DTOs.Upload;

namespace OlapAnalytics.Application.Interfaces;

public interface IGeminiService
{
    /// <summary>
    /// Full pipeline: Gemini design → SQL DW provision → bulk insert → SSAS cube deploy.
    /// </summary>
    Task<ProcessResponse> AnalyzeDatasetAsync(
        int datasetId,
        List<Dictionary<string, object?>> sampleData);
}
