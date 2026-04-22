using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OlapAnalytics.Application.DTOs;
using OlapAnalytics.Application.Interfaces;
using OlapAnalytics.Application.Services;
using OlapAnalytics.Infrastructure.Sql;
using System.Security.Claims;

namespace OlapAnalytics.API.Controllers;

/// <summary>
/// POST /api/drilldown — Executes drill-down / drill-up operations on the OLAP cube.
/// Supports navigating hierarchy levels (e.g. Country → Destination).
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DrilldownController : ControllerBase
{
    private readonly AnalyticsService _analyticsService;
    private readonly ILogger<DrilldownController> _logger;

    public DrilldownController(AnalyticsService analyticsService, ILogger<DrilldownController> logger)
    {
        _analyticsService = analyticsService;
        _logger = logger;
    }

    /// <summary>
    /// Drill DOWN: Navigates from a higher-level member to its children.
    /// Example: "Asia" → Countries in Asia
    /// Example: "Year 2023" → Quarters in 2023
    /// </summary>
    /// <remarks>
    /// Sample request:
    /// {
    ///   "measures": ["Trip Cost"],
    ///   "rowDimension": "destination",
    ///   "drillDown": {
    ///     "dimensionName": "destination",
    ///     "fromLevel": "Country",
    ///     "toLevel": "Destination",
    ///     "memberValue": "Vietnam",
    ///     "isDrillUp": false
    ///   }
    /// }
    /// </remarks>
    [HttpPost("down")]
    [ProducesResponseType(typeof(ApiResponse<QueryResponseDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    public async Task<IActionResult> DrillDown(
        [FromBody] QueryRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (request.DrillDown == null)
            return BadRequest(ApiResponse<object>.Fail("'drillDown' is required for drill-down operations."));

        request.DrillDown = new DrillPathDto
        {
            DimensionName = request.DrillDown.DimensionName,
            FromLevel     = request.DrillDown.FromLevel,
            ToLevel       = request.DrillDown.ToLevel,
            MemberValue   = request.DrillDown.MemberValue,
            IsDrillUp     = false
        };

        _logger.LogInformation(
            "DrillDown: Dim={Dim}, From={From}, To={To}, Member={Member}",
            request.DrillDown.DimensionName,
            request.DrillDown.FromLevel,
            request.DrillDown.ToLevel,
            request.DrillDown.MemberValue);

        var result = await _analyticsService.ExecuteQueryAsync(request, cancellationToken);
        return Ok(ApiResponse<QueryResponseDto>.Ok(result,
            $"Drilled down to {request.DrillDown.ToLevel}. Returned {result.TotalRows} rows."));
    }

    /// <summary>
    /// Drill UP: Navigates from a lower-level back to its parent.
    /// Example: Destinations → Countries
    /// Example: Months → Quarters
    /// </summary>
    [HttpPost("up")]
    [ProducesResponseType(typeof(ApiResponse<QueryResponseDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    public async Task<IActionResult> DrillUp(
        [FromBody] QueryRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (request.DrillDown == null)
            return BadRequest(ApiResponse<object>.Fail("'drillDown' is required for drill-up operations."));

        // Force IsDrillUp = true regardless of what client sends
        request.DrillDown = new DrillPathDto
        {
            DimensionName = request.DrillDown.DimensionName,
            FromLevel     = request.DrillDown.FromLevel,
            ToLevel       = request.DrillDown.ToLevel,
            MemberValue   = request.DrillDown.MemberValue,
            IsDrillUp     = true
        };

        _logger.LogInformation(
            "DrillUp: Dim={Dim}, From={From}",
            request.DrillDown.DimensionName,
            request.DrillDown.FromLevel);

        var result = await _analyticsService.ExecuteQueryAsync(request, cancellationToken);
        return Ok(ApiResponse<QueryResponseDto>.Ok(result,
            $"Drilled up from {request.DrillDown.FromLevel}. Returned {result.TotalRows} rows."));
    }

    /// <summary>
    /// Raw MDX drill-down using .Children MDX expression.
    /// More precise control: specify parent member to expand.
    /// </summary>
    [HttpGet("children")]
    [ProducesResponseType(typeof(ApiResponse<QueryResponseDto>), 200)]
    public async Task<IActionResult> GetChildren(
        [FromQuery] string measure = "Trip Cost",
        [FromQuery] string dimension = "destination",
        [FromQuery] string parentMember = "Vietnam",
        [FromQuery] string level = "Destination",
        CancellationToken cancellationToken = default)
    {
        // Build MDX using .Children
        var mdx =
            $"SELECT {{ [Measures].[{measure}] }} ON COLUMNS,\n" +
            $"NON EMPTY {{ [Dim Destination].[{level}].&[{parentMember}].Children }} ON ROWS\n" +
            "FROM [Travel DW]";

        _logger.LogInformation("Getting children of '{ParentMember}' in {Dimension}", parentMember, dimension);

        var result = await _analyticsService.ExecuteQueryAsync(
            new QueryRequestDto
            {
                Measures = new List<string> { measure },
                RowDimension = dimension
            },
            cancellationToken);

        // Return with the explicit MDX for transparency
        result.ExecutedMdx = mdx;
        return Ok(ApiResponse<QueryResponseDto>.Ok(result));
    }
}

/// <summary>
/// GET /api/connection — SSAS and SQL Server health check endpoints.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ConnectionController : ControllerBase
{
    private readonly OlapAnalytics.Domain.Interfaces.IMdxExecutor _executor;
    private readonly SqlQueryService? _sqlService;
    private readonly ITenantConnectionProvider _connectionProvider;
    private readonly ILogger<ConnectionController> _logger;

    public ConnectionController(
        OlapAnalytics.Domain.Interfaces.IMdxExecutor executor,
        ITenantConnectionProvider connectionProvider,
        ILogger<ConnectionController> logger,
        SqlQueryService? sqlService = null)
    {
        _executor = executor;
        _connectionProvider = connectionProvider;
        _logger = logger;
        _sqlService = sqlService;
    }

    /// <summary>Tests SSAS connection — only if user has configured it.</summary>
    [HttpGet("ssas")]
    public async Task<IActionResult> TestSsas(CancellationToken cancellationToken = default)
    {
        var ssasConnStr = await _connectionProvider.GetSsasConnectionStringAsync();
        if (string.IsNullOrWhiteSpace(ssasConnStr))
            return Ok(ApiResponse<object>.Ok(new
            {
                Connected = false,
                Source    = "",
                Reason    = "Not configured",
                TestedAt  = DateTime.UtcNow
            }, "SSAS not configured"));

        _logger.LogInformation("Testing SSAS connection.");
        var ok = await _executor.TestConnectionAsync(cancellationToken);
        var catalog = ExtractCatalog(ssasConnStr);
        return Ok(ApiResponse<object>.Ok(new
        {
            Connected = ok,
            Source    = catalog,
            TestedAt  = DateTime.UtcNow
        }, ok ? "SSAS connection OK" : "SSAS connection FAILED"));
    }

    /// <summary>Tests SQL Server (DW) connection — only if user has configured it.</summary>
    [HttpGet("sql")]
    public async Task<IActionResult> TestSql()
    {
        var sqlConnStr = await _connectionProvider.GetSqlConnectionStringAsync();

        // If the connection string only resolves to SqlMaster fallback (user hasn't configured),
        // report as not configured.
        if (string.IsNullOrWhiteSpace(sqlConnStr) || IsOnlyFallback(sqlConnStr))
            return Ok(ApiResponse<object>.Ok(new
            {
                Connected = false,
                Source    = "",
                Reason    = "Not configured",
                TestedAt  = DateTime.UtcNow
            }, "SQL not configured"));

        if (_sqlService == null)
            return Ok(ApiResponse<object>.Ok(new { Connected = false, Reason = "SqlQueryService not registered." }));

        _logger.LogInformation("Testing SQL Server connection.");
        var ok = await _sqlService.TestConnectionAsync();
        var db = ExtractDatabase(sqlConnStr);
        return Ok(ApiResponse<object>.Ok(new
        {
            Connected = ok,
            Source    = db,
            TestedAt  = DateTime.UtcNow
        }, ok ? "SQL Server connection OK" : "SQL Server connection FAILED"));
    }

    /// <summary>Tests both SSAS and SQL connections.</summary>
    [HttpGet("all")]
    public async Task<IActionResult> TestAll(CancellationToken cancellationToken = default)
    {
        var sqlConnStr  = await _connectionProvider.GetSqlConnectionStringAsync();
        var ssasConnStr = await _connectionProvider.GetSsasConnectionStringAsync();

        var sqlConfigured  = !string.IsNullOrWhiteSpace(sqlConnStr) && !IsOnlyFallback(sqlConnStr);
        var ssasConfigured = !string.IsNullOrWhiteSpace(ssasConnStr);

        // Only actually test if configured
        var ssasOk = ssasConfigured && await _executor.TestConnectionAsync(cancellationToken);
        var sqlOk  = sqlConfigured && _sqlService != null && await _sqlService.TestConnectionAsync();

        return Ok(ApiResponse<object>.Ok(new
        {
            SSAS = new
            {
                Connected = ssasOk,
                Source    = ssasConfigured ? ExtractCatalog(ssasConnStr) : "",
                Reason    = ssasConfigured ? (string?)null : "Not configured"
            },
            SqlServer = new
            {
                Connected = sqlOk,
                Source    = sqlConfigured ? ExtractDatabase(sqlConnStr) : "",
                Reason    = sqlConfigured ? (string?)null : "Not configured"
            },
            TestedAt = DateTime.UtcNow
        }));
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns true if this connection string is just the SqlMaster fallback
    /// (i.e. user hasn't set a custom connection).
    /// We detect this by checking the InitialCatalog is 'master'.
    /// </summary>
    private static bool IsOnlyFallback(string connStr)
    {
        try
        {
            var b = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(connStr);
            return string.Equals(b.InitialCatalog, "master", StringComparison.OrdinalIgnoreCase)
                || string.Equals(b.InitialCatalog, "AppAnalytics", StringComparison.OrdinalIgnoreCase);
        }
        catch { return true; }
    }

    private static string ExtractDatabase(string connStr)
    {
        try { return new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(connStr).InitialCatalog; }
        catch { return "SQL Server"; }
    }

    private static string ExtractCatalog(string connStr)
    {
        // SSAS connection string: "Data Source=...;Catalog=XYZ"
        var match = System.Text.RegularExpressions.Regex.Match(
            connStr, @"(?:Catalog|Initial Catalog)=([^;]+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        return match.Success ? match.Groups[1].Value.Trim() : "SSAS";
    }
}
