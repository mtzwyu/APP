using OlapAnalytics.Application.DTOs.Upload;

namespace OlapAnalytics.Application.Interfaces;

public interface ISqlProvisioningService
{
    Task CreateDatabaseAsync(string dbName);
    Task ExecuteSqlScriptAsync(string dbName, string sqlScript);
    Task<IEnumerable<string>> GetDatabasesAsync();

    /// <summary>
    /// Bulk-inserts rows from Excel into the provisioned DW tables
    /// using the column mappings provided by Gemini.
    /// </summary>
    Task InsertDataAsync(
        string dbName,
        List<Dictionary<string, object?>> allRows,
        SchemaDto schema,
        List<ColumnMappingDto> columnMappings);
}
