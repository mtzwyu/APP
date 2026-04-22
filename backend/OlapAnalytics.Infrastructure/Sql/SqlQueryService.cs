using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using OlapAnalytics.Application.Interfaces;
using System.Data;
using System.Diagnostics;

namespace OlapAnalytics.Infrastructure.Sql;

/// <summary>
/// Executes raw SQL queries against the Data Warehouse (SQL Server).
/// Uses Dapper for lightweight, high-performance mapping.
/// Connection: Server=LAPTOP-KQVRNVI2\MANHTRUONG1;Database=DW
/// </summary>
public class SqlQueryService
{
    private readonly ITenantConnectionProvider _connectionProvider;
    private readonly ILogger<SqlQueryService> _logger;

    public SqlQueryService(ITenantConnectionProvider connectionProvider, ILogger<SqlQueryService> logger)
    {
        _connectionProvider = connectionProvider ?? throw new ArgumentNullException(nameof(connectionProvider));
        _logger = logger;
    }

    // ──── Connection Helpers ─────────────────────────────────────────────────

    private async Task<IDbConnection> CreateConnectionAsync()
    {
        var connStr = await _connectionProvider.GetSqlConnectionStringAsync();
        return new SqlConnection(connStr);
    }

    /// <summary>Tests the SQL Server connection.</summary>
    public async Task<bool> TestConnectionAsync()
    {
        try
        {
            using var conn = await CreateConnectionAsync();
            await (conn as SqlConnection)!.OpenAsync();
            _logger.LogInformation("SQL Server connection test passed.");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SQL Server connection test failed.");
            return false;
        }
    }

    // ──── Generic Query Methods ──────────────────────────────────────────────

    /// <summary>
    /// Executes a SQL SELECT query and returns rows as List of Dictionary.
    /// Suitable for dynamic result sets that don't map to a fixed type.
    /// </summary>
    public async Task<List<Dictionary<string, object?>>> ExecuteQueryAsync(
        string sql,
        object? parameters = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Executing SQL query: {Sql}", sql);
        var sw = Stopwatch.StartNew();

        try
        {
            using var conn = await CreateConnectionAsync();
            var rows = await conn.QueryAsync(sql, parameters);
            sw.Stop();

            var result = rows
                .Select(row => ((IDictionary<string, object>)row)
                    .ToDictionary(kv => kv.Key, kv => (object?)kv.Value))
                .ToList();

            _logger.LogInformation("SQL query returned {Count} rows in {Ms}ms.", result.Count, sw.ElapsedMilliseconds);
            return result;
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "SQL query failed after {Ms}ms. Query: {Sql}", sw.ElapsedMilliseconds, sql);
            throw;
        }
    }

    /// <summary>
    /// Executes a SQL query and maps result to strongly-typed list.
    /// </summary>
    public async Task<IEnumerable<T>> QueryAsync<T>(
        string sql,
        object? parameters = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Executing typed SQL query for {Type}: {Sql}", typeof(T).Name, sql);
        var sw = Stopwatch.StartNew();

        try
        {
            using var conn = await CreateConnectionAsync();
            var result = await conn.QueryAsync<T>(sql, parameters);
            sw.Stop();
            _logger.LogInformation("SQL query returned {Count} rows in {Ms}ms.", result.Count(), sw.ElapsedMilliseconds);
            return result;
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "Typed SQL query failed after {Ms}ms.", sw.ElapsedMilliseconds);
            throw;
        }
    }

    /// <summary>
    /// Executes a scalar SQL query and returns a single value.
    /// </summary>
    public async Task<T?> ExecuteScalarAsync<T>(string sql, object? parameters = null)
    {
        try
        {
            using var conn = await CreateConnectionAsync();
            return await conn.ExecuteScalarAsync<T>(sql, parameters);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Scalar SQL query failed. Query: {Sql}", sql);
            throw;
        }
    }

    // ──── Pre-built DW Queries ────────────────────────────────────────────────

    /// <summary>
    /// Gets fact table summary: total trips, total cost, avg duration.
    /// Direct DW query for quick KPI overview.
    /// </summary>
    public async Task<Dictionary<string, object?>> GetFactSummaryAsync()
    {
        const string sql = @"
            SELECT
                COUNT(*)                    AS TotalTrips,
                SUM([Trip Cost])            AS TotalRevenue,
                AVG([Duration (days)])      AS AvgDuration,
                MIN([Start date])           AS EarliestTrip,
                MAX([End date])             AS LatestTrip
            FROM FactTravel";

        var rows = await ExecuteQueryAsync(sql);
        return rows.FirstOrDefault() ?? new Dictionary<string, object?>();
    }

    /// <summary>
    /// Gets top N destinations by trip count from DW directly.
    /// </summary>
    public async Task<List<Dictionary<string, object?>>> GetTopDestinationsAsync(int topN = 10, int? year = null, string yearColumn = "Year")
    {
        var sql = @$"
            SELECT TOP {topN}
                d.Destination,
                d.Country,
                COUNT(*)            AS TripCount,
                SUM(f.[Trip Cost])  AS TotalRevenue
            FROM FactTravel f
            JOIN DimDestination d ON f.DestinationKey = d.DestinationKey
            {(year.HasValue ? $"JOIN DimDate dt ON f.DateKey = dt.DateKey WHERE dt.[{yearColumn}] = {year}" : "")}
            GROUP BY d.Destination, d.Country
            ORDER BY TripCount DESC";

        return await ExecuteQueryAsync(sql);
    }

    /// <summary>
    /// Gets revenue and trips by year from DW.
    /// </summary>
    public async Task<List<Dictionary<string, object?>>> GetYearlyTrendAsync(string yearColumn = "Year")
    {
        string sql = $@"
            SELECT
                dt.[{yearColumn}] AS [Year],
                COUNT(*)            AS TripCount,
                SUM(f.[Trip Cost])  AS TotalRevenue,
                AVG(f.[Duration (days)]) AS AvgDuration
            FROM FactTravel f
            JOIN DimDate dt ON f.DateKey = dt.DateKey
            GROUP BY dt.[{yearColumn}]
            ORDER BY dt.[{yearColumn}]";

        return await ExecuteQueryAsync(sql);
    }

    /// <summary>
    /// Gets trips grouped by transportation type.
    /// </summary>
    public async Task<List<Dictionary<string, object?>>> GetByTransportationTypeAsync(int? year = null, string yearColumn = "Year")
    {
        var sql = @$"
            SELECT
                t.[Transportation Type],
                COUNT(*)            AS TripCount,
                SUM(f.[Trip Cost])  AS TotalRevenue
            FROM FactTravel f
            JOIN DimTransportationType t ON f.TransportationTypeKey = t.TransportationTypeKey
            {(year.HasValue ? $"JOIN DimDate dt ON f.DateKey = dt.DateKey WHERE dt.[{yearColumn}] = {year}" : "")}
            GROUP BY t.[Transportation Type]
            ORDER BY TripCount DESC";

        return await ExecuteQueryAsync(sql);
    }

    /// <summary>
    /// Gets trips grouped by accommodation type.
    /// </summary>
    public async Task<List<Dictionary<string, object?>>> GetByAccommodationTypeAsync(int? year = null, string yearColumn = "Year")
    {
        var sql = @$"
            SELECT
                a.[Accommodation Type],
                COUNT(*)            AS TripCount,
                SUM(f.[Trip Cost])  AS TotalRevenue
            FROM FactTravel f
            JOIN DimAccommodationType a ON f.AccommodationTypeKey = a.AccommodationTypeKey
            {(year.HasValue ? $"JOIN DimDate dt ON f.DateKey = dt.DateKey WHERE dt.[{yearColumn}] = {year}" : "")}
            GROUP BY a.[Accommodation Type]
            ORDER BY TripCount DESC";

        return await ExecuteQueryAsync(sql);
    }
}
