using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using OlapAnalytics.Domain.Entities;
using OlapAnalytics.Domain.Interfaces;

namespace OlapAnalytics.Infrastructure.Repositories;

public class DatasetRepository : IDatasetRepository
{
    private readonly string _connectionString;

    public DatasetRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("AppDb") ?? throw new InvalidOperationException("AppDb connection string not found.");
    }

    private IDbConnection CreateConnection() => new SqlConnection(_connectionString);

    public async Task<Dataset?> GetByIdAsync(int id)
    {
        using var connection = CreateConnection();
        const string sql = "SELECT * FROM Datasets WHERE Id = @Id";
        return await connection.QuerySingleOrDefaultAsync<Dataset>(sql, new { Id = id });
    }

    public async Task<IEnumerable<Dataset>> GetByUserIdAsync(int userId)
    {
        using var connection = CreateConnection();
        const string sql = "SELECT * FROM Datasets WHERE UserId = @UserId ORDER BY CreatedAt DESC";
        return await connection.QueryAsync<Dataset>(sql, new { UserId = userId });
    }

    public async Task<IEnumerable<Dataset>> GetAllAsync()
    {
        using var connection = CreateConnection();
        const string sql = "SELECT * FROM Datasets ORDER BY CreatedAt DESC";
        return await connection.QueryAsync<Dataset>(sql);
    }

    public async Task<int> CreateAsync(Dataset dataset)
    {
        using var connection = CreateConnection();
        const string sql = @"
            INSERT INTO Datasets (UserId, FileName, DbName, [RowCount], Status, ErrorMsg, RawFileBytes, SampleJson)
            OUTPUT INSERTED.Id
            VALUES (@UserId, @FileName, @DbName, @RowCount, @Status, @ErrorMsg, @RawFileBytes, @SampleJson)";
        return await connection.ExecuteScalarAsync<int>(sql, dataset);
    }

    public async Task UpdateFileDataAsync(int id, byte[] rawBytes, string sampleJson)
    {
        using var connection = CreateConnection();
        const string sql = @"
            UPDATE Datasets
            SET RawFileBytes = @RawFileBytes, SampleJson = @SampleJson, UpdatedAt = GETUTCDATE()
            WHERE Id = @Id";
        await connection.ExecuteAsync(sql, new { Id = id, RawFileBytes = rawBytes, SampleJson = sampleJson });
    }


    public async Task UpdateStatusAsync(int id, string status, string? errorMsg = null)
    {
        using var connection = CreateConnection();
        const string sql = "UPDATE Datasets SET Status = @Status, ErrorMsg = @ErrorMsg, UpdatedAt = GETUTCDATE() WHERE Id = @Id";
        await connection.ExecuteAsync(sql, new { Id = id, Status = status, ErrorMsg = errorMsg });
    }

    public async Task DeleteAsync(int id)
    {
        using var connection = CreateConnection();
        const string sql = "DELETE FROM Datasets WHERE Id = @Id";
        await connection.ExecuteAsync(sql, new { Id = id });
    }
}
