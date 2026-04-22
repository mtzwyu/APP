using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OlapAnalytics.Application.DTOs;
using OlapAnalytics.Application.Services;

namespace OlapAnalytics.API.Controllers;

/// <summary>GET /api/dimensions — Returns all cube dimensions and hierarchies.</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DimensionsController : ControllerBase
{
    private readonly AnalyticsService _analyticsService;

    public DimensionsController(AnalyticsService analyticsService) => _analyticsService = analyticsService;

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<DimensionDto>>), 200)]
    public async Task<IActionResult> GetDimensions(CancellationToken cancellationToken = default)
    {
        var dimensions = await _analyticsService.GetDimensionsAsync(cancellationToken);
        return Ok(ApiResponse<IEnumerable<DimensionDto>>.Ok(dimensions));
    }
}

/// <summary>GET /api/measures — Returns all cube measures.</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MeasuresController : ControllerBase
{
    private readonly AnalyticsService _analyticsService;

    public MeasuresController(AnalyticsService analyticsService) => _analyticsService = analyticsService;

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<MeasureDto>>), 200)]
    public async Task<IActionResult> GetMeasures(CancellationToken cancellationToken = default)
    {
        var measures = await _analyticsService.GetMeasuresAsync(cancellationToken);
        return Ok(ApiResponse<IEnumerable<MeasureDto>>.Ok(measures));
    }
}
