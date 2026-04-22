using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OlapAnalytics.Application.DTOs;
using OlapAnalytics.Application.Services;
using System.Text.Json;

namespace OlapAnalytics.API.Controllers;

// Helper record to deserialize the slicers JSON from the frontend
file record SlicerParam(string Dimension, string Member);

/// <summary>GET /api/kpi — Returns KPI calculations (YoY, MoM, Growth Rate).</summary>
[ApiController]
[Route("api/[controller]")]
// [Authorize]
public class KpiController : ControllerBase
{
    private readonly KpiService _kpiService;
    private readonly CubeMetadataResolver _resolver;
    private readonly ILogger<KpiController> _logger;

    public KpiController(KpiService kpiService, CubeMetadataResolver resolver, ILogger<KpiController> logger)
    {
        _kpiService = kpiService;
        _resolver = resolver;
        _logger = logger;
    }

    /// <summary>Calculate KPI for a measure and year. If yearColumn is empty, auto-detects from SSAS metadata.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<KpiDto>), 200)]
    public async Task<IActionResult> GetKpi(
        [FromQuery] string? measure = null,
        [FromQuery] string? yearColumn = null,
        [FromQuery] int? year = null,
        [FromQuery] int? previousYear = null,
        [FromQuery] int? month = null,
        [FromQuery] string? slicers = null,
        CancellationToken cancellationToken = default)
    {
        var resolvedMeasure   = !string.IsNullOrWhiteSpace(measure)    ? measure    : await _resolver.GetFirstMeasureNameAsync(cancellationToken);
        var resolvedYearLevel = !string.IsNullOrWhiteSpace(yearColumn) ? yearColumn : await _resolver.GetYearLevelUniqueNameAsync(cancellationToken);

        var request = new KpiRequestDto
        {
            Measure     = resolvedMeasure,
            YearColumn  = resolvedYearLevel,
            Year        = year,
            PreviousYear= previousYear,
            Month       = month,
            Slicers     = ParseSlicers(slicers)
        };

        _logger.LogInformation("KPI request: Measure={M}, YearLevel={Y}, Year={Yr}", resolvedMeasure, resolvedYearLevel, year);
        var kpi = await _kpiService.GetKpiAsync(request, cancellationToken);
        return Ok(ApiResponse<KpiDto>.Ok(kpi));
    }

    private static List<DimensionSlicerDto> ParseSlicers(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new();
        try
        {
            var raw = JsonSerializer.Deserialize<List<SlicerParam>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return raw?.Select(s => new DimensionSlicerDto { DimensionUniqueName = s.Dimension, MemberValue = s.Member }).ToList() ?? new();
        }
        catch { return new(); }
    }
}

/// <summary>GET /api/trend — Returns time-series trend data.</summary>
[ApiController]
[Route("api/[controller]")]
// [Authorize]
public class TrendController : ControllerBase
{
    private readonly TrendService _trendService;
    private readonly CubeMetadataResolver _resolver;
    private readonly ILogger<TrendController> _logger;

    public TrendController(TrendService trendService, CubeMetadataResolver resolver, ILogger<TrendController> logger)
    {
        _trendService = trendService;
        _resolver = resolver;
        _logger = logger;
    }

    private static List<DimensionSlicerDto> ParseSlicers(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new();
        try
        {
            var raw = JsonSerializer.Deserialize<List<SlicerParam>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return raw?.Select(s => new DimensionSlicerDto { DimensionUniqueName = s.Dimension, MemberValue = s.Member }).ToList() ?? new();
        }
        catch { return new(); }
    }

    /// <summary>Get trend data for a measure at a specified time granularity. Auto-detects dimension if not provided.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<TrendDto>), 200)]
    public async Task<IActionResult> GetTrend(
        [FromQuery] string? measure = null,
        [FromQuery] string granularity = "Monthly",
        [FromQuery] string? yearColumn = null,
        [FromQuery] int? year = null,
        [FromQuery] int topN = 0,
        [FromQuery] string? slicers = null,
        CancellationToken cancellationToken = default)
    {
        var resolvedMeasure   = !string.IsNullOrWhiteSpace(measure)    ? measure    : await _resolver.GetFirstMeasureNameAsync(cancellationToken);
        var resolvedYearLevel = !string.IsNullOrWhiteSpace(yearColumn) ? yearColumn : await _resolver.GetYearLevelUniqueNameAsync(cancellationToken);

        var request = new TrendRequestDto
        {
            Measure     = resolvedMeasure,
            Granularity = granularity,
            YearColumn  = resolvedYearLevel,
            Year        = year,
            TopN        = topN,
            Slicers     = ParseSlicers(slicers)
        };

        _logger.LogInformation("Trend request: Measure={M}, Granularity={G}, YearLevel={Y}", resolvedMeasure, granularity, resolvedYearLevel);
        var trend = await _trendService.GetTrendAsync(request, cancellationToken);
        return Ok(ApiResponse<TrendDto>.Ok(trend));
    }

    /// <summary>Get Top N rows by a measure. Auto-detects dimension/measure if not provided.</summary>
    [HttpGet("topn")]
    [ProducesResponseType(typeof(ApiResponse<QueryResponseDto>), 200)]
    public async Task<IActionResult> GetTopN(
        [FromQuery] string? measure = null,
        [FromQuery] string? dimension = null,
        [FromQuery] int n = 10,
        [FromQuery] string? yearColumn = null,
        [FromQuery] int? year = null,
        [FromQuery] string? slicers = null,
        CancellationToken cancellationToken = default)
    {
        var resolvedMeasure   = !string.IsNullOrWhiteSpace(measure)   ? measure   : await _resolver.GetFirstMeasureNameAsync(cancellationToken);
        var resolvedDimension = !string.IsNullOrWhiteSpace(dimension) ? dimension : await _resolver.GetFirstRowDimensionUniqueNameAsync(cancellationToken);
        var resolvedYearLevel = !string.IsNullOrWhiteSpace(yearColumn)? yearColumn: await _resolver.GetYearLevelUniqueNameAsync(cancellationToken);

        _logger.LogInformation("Top N request: Top {N} {Dimension} by {Measure}", n, resolvedDimension, resolvedMeasure);
        var result = await _trendService.GetTopNAsync(resolvedMeasure, resolvedDimension, n, resolvedYearLevel, year,
            ParseSlicers(slicers), cancellationToken);
        return Ok(ApiResponse<QueryResponseDto>.Ok(result));
    }
}
