using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OlapAnalytics.Application.DTOs.Upload;
using OlapAnalytics.Application.Interfaces;
using System.Data;
using Dapper;


namespace OlapAnalytics.Application.Services;

public class SqlProvisioningService : ISqlProvisioningService
{
    private readonly ITenantConnectionProvider _connectionProvider;
    private readonly ILogger<SqlProvisioningService> _logger;

    public SqlProvisioningService(ITenantConnectionProvider connectionProvider, ILogger<SqlProvisioningService> logger)
    {
        _connectionProvider = connectionProvider;
        _logger = logger;
    }

    private async Task<string> GetMasterConnectionStringAsync()
    {
        var connStr = await _connectionProvider.GetSqlConnectionStringAsync();
        var builder = new SqlConnectionStringBuilder(connStr) { InitialCatalog = "master" };
        return builder.ConnectionString;
    }

    public async Task CreateDatabaseAsync(string dbName)
    {
        try
        {
            var masterConnStr = await GetMasterConnectionStringAsync();
            using var connection = new SqlConnection(masterConnStr);
            await connection.OpenAsync();

            string checkDbSql = "SELECT database_id FROM sys.databases WHERE Name = @DbName";
            var existing = await connection.QuerySingleOrDefaultAsync<int?>(checkDbSql, new { DbName = dbName });

            if (existing.HasValue)
            {
                _logger.LogInformation("Database {DbName} already exists.", dbName);
                return;
            }

            _logger.LogInformation("Creating database {DbName}...", dbName);
            // CREATE DATABASE cannot be parameterized, so we whitelist the name formatting
            if (!System.Text.RegularExpressions.Regex.IsMatch(dbName, @"^[a-zA-Z0-9_]+$"))
                throw new ArgumentException("Invalid database name");

            string createSql = $"CREATE DATABASE [{dbName}]";
            await connection.ExecuteAsync(createSql);
            _logger.LogInformation("Database {DbName} created successfully.", dbName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create database {DbName}", dbName);
            throw;
        }
    }

    public async Task ExecuteSqlScriptAsync(string dbName, string sqlScript)
    {
        try
        {
            if (!System.Text.RegularExpressions.Regex.IsMatch(dbName, @"^[a-zA-Z0-9_]+$"))
                throw new ArgumentException("Invalid database name");

            var masterConnStr = await GetMasterConnectionStringAsync();

            // We connect directly to the newly created database
            var builder = new SqlConnectionStringBuilder(masterConnStr)
            {
                InitialCatalog = dbName
            };
            
            using var connection = new SqlConnection(builder.ConnectionString);
            await connection.OpenAsync();

            _logger.LogInformation("Executing script on {DbName}...", dbName);
            
            // Execute the generated script
            await connection.ExecuteAsync(sqlScript);
            
            _logger.LogInformation("Successfully executed script on {DbName}.", dbName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute script on {DbName}", dbName);
            throw;
        }
    }

    public async Task<IEnumerable<string>> GetDatabasesAsync()
    {
        var masterConnStr = await GetMasterConnectionStringAsync();
        using var connection = new SqlConnection(masterConnStr);
        await connection.OpenAsync();
        string sql = "SELECT name FROM sys.databases WHERE name LIKE 'DW_%'";
        return await connection.QueryAsync<string>(sql);
    }

    // ── Bulk-insert Excel rows into DW tables ─────────────────────────────────
    public async Task InsertDataAsync(
        string dbName,
        List<Dictionary<string, object?>> allRows,
        SchemaDto schema,
        List<ColumnMappingDto> columnMappings)
    {
        if (allRows.Count == 0) return;

        var masterConnStr = await GetMasterConnectionStringAsync();
        var builder = new SqlConnectionStringBuilder(masterConnStr) { InitialCatalog = dbName };
        var connStr = builder.ConnectionString;

        // Build lookup: targetTable → list of mappings
        var byTable = columnMappings
            .Where(m => m.Role != "skip" && !string.IsNullOrWhiteSpace(m.TargetTable))
            .GroupBy(m => m.TargetTable, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

        // Insert Dimension tables first (so Fact FK keys resolve)
        var dimTableNames = schema.DimensionTables?.Select(d => d.Name).ToHashSet(StringComparer.OrdinalIgnoreCase) ?? new();

        var orderedTables = byTable.Keys
            .OrderBy(t => dimTableNames.Contains(t) ? 0 : 1) // dims first
            .ToList();

        foreach (var tableName in orderedTables)
        {
            var mappings = byTable[tableName];
            await BulkInsertTableAsync(connStr, tableName, mappings, allRows);
        }

        _logger.LogInformation("Bulk insert completed for {TableCount} tables, {RowCount} rows.", orderedTables.Count, allRows.Count);
    }

    private async Task BulkInsertTableAsync(
        string connStr,
        string tableName,
        List<ColumnMappingDto> mappings,
        List<Dictionary<string, object?>> allRows)
    {
        if (!System.Text.RegularExpressions.Regex.IsMatch(tableName, @"^[a-zA-Z0-9_]+$"))
            throw new ArgumentException($"Invalid table name: {tableName}");

        using var connection = new SqlConnection(connStr);
        await connection.OpenAsync();

        // Get actual columns from SQL Server to match case exactly
        var actualCols = (await connection.QueryAsync<string>(
            $"SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = @t",
            new { t = tableName })).ToHashSet(StringComparer.OrdinalIgnoreCase);

        // Filter mappings to only columns that exist in the target table
        var validMappings = mappings
            .Where(m => actualCols.Contains(m.TargetColumn))
            .ToList();

        if (!validMappings.Any())
        {
            _logger.LogWarning("No valid column mappings for table '{Table}', skipping.", tableName);
            return;
        }

        // Build DataTable
        var dt = new DataTable(tableName);
        foreach (var m in validMappings)
            dt.Columns.Add(m.TargetColumn);

        // Deduplicate dim rows by all columns (basic dedup)
        var seenKeys = new HashSet<string>();
        foreach (var row in allRows)
        {
            var values = validMappings.Select(m =>
                row.TryGetValue(m.SourceColumn, out var v) ? v ?? DBNull.Value : DBNull.Value).ToArray();

            var key = string.Join("|", values.Select(v => v?.ToString() ?? ""));
            if (!seenKeys.Add(key)) continue; // skip duplicates for dim tables

            dt.Rows.Add(values);
        }

        // Truncate first to allow re-processing
        await connection.ExecuteAsync($"TRUNCATE TABLE [{tableName}]");

        // SqlBulkCopy
        using var bulk = new SqlBulkCopy(connection)
        {
            DestinationTableName = $"[{tableName}]",
            BatchSize = 1000,
            BulkCopyTimeout = 300
        };
        foreach (var m in validMappings)
            bulk.ColumnMappings.Add(m.TargetColumn, m.TargetColumn);

        await bulk.WriteToServerAsync(dt);
        _logger.LogInformation("Inserted {Rows} rows into [{Table}].", dt.Rows.Count, tableName);
    }
}

