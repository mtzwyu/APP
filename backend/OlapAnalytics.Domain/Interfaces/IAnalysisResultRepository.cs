using OlapAnalytics.Domain.Entities;

namespace OlapAnalytics.Domain.Interfaces;

public interface IAnalysisResultRepository
{
    Task<AnalysisResult?> GetByDatasetIdAsync(int datasetId);
    Task<int> CreateAsync(AnalysisResult result);
}
