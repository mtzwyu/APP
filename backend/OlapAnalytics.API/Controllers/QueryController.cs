using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OlapAnalytics.Application.DTOs;
using OlapAnalytics.Application.Services;

namespace OlapAnalytics.API.Controllers;

/// <summary>
/// POST /api/query — Main OLAP analytics query endpoint.
/// Supports filtering, drill-down, slice/dice, and Top N.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class QueryController : ControllerBase
{
    private readonly AnalyticsService _analyticsService;
    private readonly ILogger<QueryController> _logger;

    public QueryController(AnalyticsService analyticsService, ILogger<QueryController> logger)
    {
        _analyticsService = analyticsService;
        _logger = logger;
    }

    /// <summary>Execute an OLAP query with optional filters, drill-down, and slice/dice.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<QueryResponseDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public async Task<IActionResult> ExecuteQuery(
        [FromBody] QueryRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse<object>.Fail(
                string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage))));

        _logger.LogInformation("Query request from {User}: Measures={Measures}",
            User.Identity?.Name, string.Join(",", request.Measures));

        var result = await _analyticsService.ExecuteQueryAsync(request, cancellationToken);
        return Ok(ApiResponse<QueryResponseDto>.Ok(result,
            $"Query returned {result.TotalRows} rows in {result.ExecutionTimeMs}ms"));
    }
}
