using Microsoft.Extensions.Logging;
using OlapAnalytics.Application.DTOs;
using OlapAnalytics.Domain.Interfaces;

namespace OlapAnalytics.Application.Services;

/// <summary>
/// Calculates KPIs: YoY, MoM, Growth Rate for Travel analytics.
/// Executes pre-built MDX expressions and extracts metric values.
/// </summary>
public class KpiService
{
    private readonly IMdxExecutor _executor;
    private readonly MdxQueryBuilder _builder;
    private readonly ILogger<KpiService> _logger;

    public KpiService(
        IMdxExecutor executor,
        MdxQueryBuilder builder,
        ILogger<KpiService> logger)
    {
        _executor = executor;
        _builder = builder;
        _logger = logger;
    }

    /// <summary>
    /// Calculates the main KPIs for a given measure and time period.
    /// Returns YoY, MoM, and growth rate.
    /// </summary>
    public async Task<KpiDto> GetKpiAsync(KpiRequestDto request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Calculating KPI for measure: {Measure}, Year: {Year}", request.Measure, request.Year);

        var currentYear = request.Year ?? DateTime.UtcNow.Year;
        var previousYear = request.PreviousYear ?? currentYear - 1;

        var cubeName = await _executor.GetActiveCubeNameAsync(cancellationToken);
        var timeLevel = request.YearColumn;
        
        var filters = (request.Slicers ?? new()).Select(s => new Domain.ValueObjects.DimensionFilter
        {
            DimensionName = s.DimensionUniqueName,
            MemberValues = new List<string> { s.MemberValue }
        }).ToList();

        // 1. Get current year total
        var currentYearMdx =
            $"SELECT {{ [Measures].[{request.Measure}] }} ON COLUMNS,\n" +
            $"{{ {timeLevel}.&[{currentYear}] }} ON ROWS\n" +
            $"FROM {cubeName}";
        currentYearMdx = await _builder.ApplyFiltersAsync(currentYearMdx, filters);

        // 2. Get previous year total
        var previousYearMdx =
            $"SELECT {{ [Measures].[{request.Measure}] }} ON COLUMNS,\n" +
            $"{{ {timeLevel}.&[{previousYear}] }} ON ROWS\n" +
            $"FROM {cubeName}";
        previousYearMdx = await _builder.ApplyFiltersAsync(previousYearMdx, filters);

        // 3. Execute YoY query (with calculated member)
        var yoyMdx = await _builder.BuildYoYQueryAsync(cubeName, request.Measure, currentYear, request.YearColumn);
        yoyMdx = await _builder.ApplyFiltersAsync(yoyMdx, filters);

        decimal currentValue = 0, previousValue = 0, yoyGrowth = 0, momGrowth = 0;

        try
        {
            var currentResult = await _executor.ExecuteQueryAsync(currentYearMdx, cancellationToken);
            if (currentResult.Cells.Any())
                currentValue = currentResult.Cells.First().Value;

            var previousResult = await _executor.ExecuteQueryAsync(previousYearMdx, cancellationToken);
            if (previousResult.Cells.Any())
                previousValue = previousResult.Cells.First().Value;

            // YoY growth
            yoyGrowth = CalculateGrowthRate(currentValue, previousValue);

            if (request.Month.HasValue)
            {
                var momMdx = await _builder.BuildMoMQueryAsync(cubeName, request.Measure, currentYear, request.YearColumn);
                momMdx = await _builder.ApplyFiltersAsync(momMdx, filters);
                
                var momResult = await _executor.ExecuteQueryAsync(momMdx, cancellationToken);
                var momRow = momResult.Cells
                    .Where(c => c.AxisValues.Any(v => v.Contains(request.Month.Value.ToString())))
                    .ToList();
                if (momRow.Count >= 2)
                    momGrowth = momRow[1].Value; // Second column = MoM %
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating KPI for {Measure}", request.Measure);
            throw;
        }

        var growthRate = CalculateGrowthRate(currentValue, previousValue);
        var trendDirection = growthRate > 0 ? "up" : growthRate < 0 ? "down" : "flat";

        return new KpiDto
        {
            MeasureName = request.Measure,
            CurrentValue = currentValue,
            PreviousValue = previousValue,
            GrowthRate = growthRate,
            YearOverYear = yoyGrowth,
            MonthOverMonth = momGrowth,
            TrendDirection = trendDirection,
            Period = $"{currentYear} vs {previousYear}",
            FormattedCurrentValue = FormatCurrency(currentValue),
            FormattedGrowthRate = $"{growthRate:+0.00;-0.00}%"
        };
    }

    /// <summary>
    /// Calculates growth rate % between two values.
    /// Handles divide-by-zero gracefully.
    /// </summary>
    public static decimal CalculateGrowthRate(decimal current, decimal previous)
    {
        if (previous == 0) return current > 0 ? 100m : 0m;
        return Math.Round((current - previous) / Math.Abs(previous) * 100, 2);
    }

    private static string FormatCurrency(decimal value)
    {
        return value >= 1_000_000
            ? $"${value / 1_000_000:F2}M"
            : value >= 1_000
                ? $"${value / 1_000:F1}K"
                : $"${value:F2}";
    }
}
