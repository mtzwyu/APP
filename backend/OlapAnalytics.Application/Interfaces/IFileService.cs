using OlapAnalytics.Application.DTOs.Upload;

namespace OlapAnalytics.Application.Interfaces;

public interface IFileService
{
    Task<UploadResponse> SaveUploadAsync(int userId, string fileName, Stream fileStream);
    Task<List<Dictionary<string, object?>>> ReadExcelSampleAsync(Stream stream, int maxRows = 200);
    Task<(List<string> Headers, List<Dictionary<string, object?>> Rows)> ReadExcelAllAsync(Stream stream);
}
