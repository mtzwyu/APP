using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OlapAnalytics.Application.DTOs;
using OlapAnalytics.Infrastructure.Sql;

namespace OlapAnalytics.API.Controllers;

/// <summary>
/// GET /api/sql — Direct SQL Server Data Warehouse queries.
/// Used when SSAS is unavailable or for raw DW data access.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SqlController : ControllerBase
{
    private readonly SqlQueryService _sqlService;
    private readonly ILogger<SqlController> _logger;

    public SqlController(SqlQueryService sqlService, ILogger<SqlController> logger)
    {
        _sqlService = sqlService;
        _logger = logger;
    }

    /// <summary>Returns a high-level summary of all trips in the DW (count, total revenue, avg duration).</summary>
    [HttpGet("summary")]
    [ProducesResponseType(typeof(ApiResponse<Dictionary<string, object?>>), 200)]
    public async Task<IActionResult> GetSummary()
    {
        _logger.LogInformation("Fetching DW fact summary.");
        var data = await _sqlService.GetFactSummaryAsync();
        return Ok(ApiResponse<Dictionary<string, object?>>.Ok(data, "DW fact summary"));
    }

    /// <summary>Returns top N destinations by trip count from SQL Server DW.</summary>
    [HttpGet("top-destinations")]
    [ProducesResponseType(typeof(ApiResponse<List<Dictionary<string, object?>>>), 200)]
    public async Task<IActionResult> GetTopDestinations(
        [FromQuery] int topN = 10,
        [FromQuery] string yearColumn = "Year",
        [FromQuery] int? year = null)
    {
        _logger.LogInformation("Fetching top {N} destinations for year {Year}.", topN, year);
        var data = await _sqlService.GetTopDestinationsAsync(topN, year, yearColumn);
        return Ok(ApiResponse<List<Dictionary<string, object?>>>.Ok(data,
            $"Top {topN} destinations from DW"));
    }

    /// <summary>Returns yearly revenue and trip count trend from SQL Server DW.</summary>
    [HttpGet("yearly-trend")]
    [ProducesResponseType(typeof(ApiResponse<List<Dictionary<string, object?>>>), 200)]
    public async Task<IActionResult> GetYearlyTrend([FromQuery] string yearColumn = "Year")
    {
        _logger.LogInformation("Fetching yearly trend from DW.");
        var data = await _sqlService.GetYearlyTrendAsync(yearColumn);
        return Ok(ApiResponse<List<Dictionary<string, object?>>>.Ok(data, "Yearly trend from DW"));
    }

    /// <summary>Returns breakdown by transportation type from SQL Server DW.</summary>
    [HttpGet("by-transportation")]
    [ProducesResponseType(typeof(ApiResponse<List<Dictionary<string, object?>>>), 200)]
    public async Task<IActionResult> GetByTransportation([FromQuery] string yearColumn = "Year", [FromQuery] int? year = null)
    {
        _logger.LogInformation("Fetching transportation type breakdown. Year={Year}", year);
        var data = await _sqlService.GetByTransportationTypeAsync(year, yearColumn);
        return Ok(ApiResponse<List<Dictionary<string, object?>>>.Ok(data, "Transportation breakdown from DW"));
    }

    /// <summary>Returns breakdown by accommodation type from SQL Server DW.</summary>
    [HttpGet("by-accommodation")]
    [ProducesResponseType(typeof(ApiResponse<List<Dictionary<string, object?>>>), 200)]
    public async Task<IActionResult> GetByAccommodation([FromQuery] string yearColumn = "Year", [FromQuery] int? year = null)
    {
        _logger.LogInformation("Fetching accommodation type breakdown. Year={Year}", year);
        var data = await _sqlService.GetByAccommodationTypeAsync(year, yearColumn);
        return Ok(ApiResponse<List<Dictionary<string, object?>>>.Ok(data, "Accommodation breakdown from DW"));
    }

    /// <summary>
    /// Executes a raw SQL query (Admin only).
    /// WARNING: Use with caution. Validates query is SELECT-only.
    /// </summary>
    [HttpPost("raw")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(ApiResponse<List<Dictionary<string, object?>>>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    public async Task<IActionResult> ExecuteRaw(
        [FromBody] RawSqlRequestDto request,
        CancellationToken cancellationToken = default)
    {
        // Basic guard — only allow SELECT statements
        var trimmed = request.Sql.Trim();
        if (!trimmed.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
            return BadRequest(ApiResponse<object>.Fail("Only SELECT statements are allowed."));

        _logger.LogInformation("Admin executing raw SQL. User={User}", User.Identity?.Name);
        var data = await _sqlService.ExecuteQueryAsync(request.Sql, cancellationToken: cancellationToken);
        return Ok(ApiResponse<List<Dictionary<string, object?>>>.Ok(data, $"Returned {data.Count} rows."));
    }
}

/// <summary>Request DTO for raw SQL execution.</summary>
public class RawSqlRequestDto
{
    public string Sql { get; set; } = string.Empty;
}
