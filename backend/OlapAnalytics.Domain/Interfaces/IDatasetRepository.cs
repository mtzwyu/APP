using OlapAnalytics.Domain.Entities;

namespace OlapAnalytics.Domain.Interfaces;

public interface IDatasetRepository
{
    Task<Dataset?> GetByIdAsync(int id);
    Task<IEnumerable<Dataset>> GetByUserIdAsync(int userId);
    Task<IEnumerable<Dataset>> GetAllAsync();
    Task<int> CreateAsync(Dataset dataset);
    Task UpdateStatusAsync(int id, string status, string? errorMsg = null);
    Task UpdateFileDataAsync(int id, byte[] rawBytes, string sampleJson);
    Task DeleteAsync(int id);
}

