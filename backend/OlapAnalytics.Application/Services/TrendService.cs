using Microsoft.Extensions.Logging;
using OlapAnalytics.Application.DTOs;
using OlapAnalytics.Domain.Interfaces;

namespace OlapAnalytics.Application.Services;

/// <summary>
/// Provides time-series trend analysis and Top-N analytics.
/// Executes time-granularity MDX queries and computes period-over-period deltas.
/// </summary>
public class TrendService
{
    private readonly IMdxExecutor _executor;
    private readonly MdxQueryBuilder _builder;
    private readonly ILogger<TrendService> _logger;

    public TrendService(
        IMdxExecutor executor,
        MdxQueryBuilder builder,
        ILogger<TrendService> logger)
    {
        _executor = executor;
        _builder = builder;
        _logger = logger;
    }

    /// <summary>
    /// Returns time-series trend data for a given measure at the requested granularity.
    /// Granularity: "Yearly" | "Quarterly" | "Monthly"
    /// </summary>
    public async Task<TrendDto> GetTrendAsync(TrendRequestDto request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Building trend query. Measure: {Measure}, Granularity: {G}",
            request.Measure, request.Granularity);

        var cubeName = await _executor.GetActiveCubeNameAsync(cancellationToken);
        if (string.IsNullOrEmpty(cubeName))
            throw new InvalidOperationException("Hệ thống chưa tìm thấy Cube dữ liệu. Vui lòng thực hiện 'Phân tích' dữ liệu trước.");
        
        IEnumerable<Domain.ValueObjects.DimensionFilter>? extraFilters = null;
        if (request.Slicers != null && request.Slicers.Any())
        {
            extraFilters = request.Slicers.Select(s => new Domain.ValueObjects.DimensionFilter
            {
                DimensionName = s.DimensionUniqueName,
                MemberValues = new List<string> { s.MemberValue }
            });
        }

        var mdx = await _builder.BuildTrendQueryAsync(cubeName!, request.Measure, request.YearColumn, request.Granularity, request.Year, extraFilters);
        var result = await _executor.ExecuteQueryAsync(mdx, cancellationToken);

        var dataPoints = new List<TrendDataPointDto>();
        var rowGroups = result.Cells
            .GroupBy(c => string.Join("|", c.AxisValues))
            .ToList();

        // Sort rowGroups numerically if the axis values are integers (e.g., month numbers or years)
        rowGroups.Sort((g1, g2) => {
            var v1 = string.Join(" ", g1.First().AxisValues);
            var v2 = string.Join(" ", g2.First().AxisValues);
            if (int.TryParse(v1, out int i1) && int.TryParse(v2, out int i2))
                return i1.CompareTo(i2);
            return string.Compare(v1, v2, StringComparison.OrdinalIgnoreCase);
        });

        decimal? previousValue = null;
        foreach (var group in rowGroups)
        {
            var cell = group.First();
            var growthRate = previousValue.HasValue && previousValue.Value != 0
                ? Math.Round((cell.Value - previousValue.Value) / Math.Abs(previousValue.Value) * 100, 2)
                : (decimal?)null;

            dataPoints.Add(new TrendDataPointDto
            {
                Period = string.Join(" ", cell.AxisValues),
                Value = cell.Value,
                FormattedValue = cell.FormattedValue,
                GrowthRate = growthRate
            });

            previousValue = cell.Value;
        }

        return new TrendDto
        {
            Measure = request.Measure,
            Granularity = request.Granularity,
            DataPoints = dataPoints
        };
    }

    /// <summary>
    /// Returns Top N destinations/categories by a given measure.
    /// </summary>
    public async Task<QueryResponseDto> GetTopNAsync(
        string measure, string dimension, int topN,
        string yearColumn = "Year", int? year = null,
        List<DTOs.DimensionSlicerDto>? slicers = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Building Top {N} query for {Measure} by {Dimension}", topN, measure, dimension);

        var dateRange = year.HasValue ? new Domain.ValueObjects.DateRange { YearColumn = yearColumn, Year = year } : null;
        var cubeName = await _executor.GetActiveCubeNameAsync(cancellationToken);
        if (string.IsNullOrEmpty(cubeName))
            throw new InvalidOperationException("Hệ thống chưa tìm thấy Cube dữ liệu. Vui lòng thực hiện 'Phân tích' dữ liệu trước.");

        var mdx = await _builder.BuildTopNQueryAsync(cubeName!, measure, dimension, topN, null, dateRange);

        // Apply extra dimension slicers (month, day, weekday attributes from Dim Time)
        if (slicers != null && slicers.Count > 0)
        {
            var dimFilters = slicers.Select(s => new Domain.ValueObjects.DimensionFilter
            {
                DimensionName = s.DimensionUniqueName,
                MemberValues = new List<string> { s.MemberValue }
            });
            mdx = await _builder.ApplyFiltersAsync(mdx, dimFilters);
        }

        var result = await _executor.ExecuteQueryAsync(mdx, cancellationToken);

        var rows = result.Cells
            .GroupBy(c => string.Join("|", c.AxisValues))
            .Select(g => new QueryRowDto
            {
                AxisValues = g.First().AxisValues,
                Values = g.Select(c => c.Value).ToList(),
                FormattedValues = g.Select(c => c.FormattedValue).ToList()
            }).ToList();

        return new QueryResponseDto
        {
            Headers = result.ColumnHeaders,
            Rows = rows,
            ExecutedMdx = result.ExecutedMdx,
            ExecutionTimeMs = (long)result.ExecutionTime.TotalMilliseconds,
            FromCache = result.FromCache,
            TotalRows = rows.Count
        };
    }

}
