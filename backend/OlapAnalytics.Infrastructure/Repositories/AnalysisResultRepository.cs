using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using OlapAnalytics.Domain.Entities;
using OlapAnalytics.Domain.Interfaces;

namespace OlapAnalytics.Infrastructure.Repositories;

public class AnalysisResultRepository : IAnalysisResultRepository
{
    private readonly string _connectionString;

    public AnalysisResultRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("AppDb") ?? throw new InvalidOperationException("AppDb connection string not found.");
    }

    private IDbConnection CreateConnection() => new SqlConnection(_connectionString);

    public async Task<AnalysisResult?> GetByDatasetIdAsync(int datasetId)
    {
        using var connection = CreateConnection();
        const string sql = "SELECT * FROM AnalysisResults WHERE DatasetId = @DatasetId ORDER BY CreatedAt DESC";
        return await connection.QueryFirstOrDefaultAsync<AnalysisResult>(sql, new { DatasetId = datasetId });
    }

    public async Task<int> CreateAsync(AnalysisResult result)
    {
        using var connection = CreateConnection();
        const string sql = @"
            INSERT INTO AnalysisResults (DatasetId, SchemaJson, InsightsJson, SsasCubeJson, SqlScript)
            OUTPUT INSERTED.Id
            VALUES (@DatasetId, @SchemaJson, @InsightsJson, @SsasCubeJson, @SqlScript)";
        return await connection.ExecuteScalarAsync<int>(sql, result);
    }
}
