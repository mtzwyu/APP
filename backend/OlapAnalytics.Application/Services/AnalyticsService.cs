using Microsoft.Extensions.Logging;
using OlapAnalytics.Application.DTOs;
using OlapAnalytics.Domain.Entities;
using OlapAnalytics.Domain.Interfaces;
using OlapAnalytics.Domain.ValueObjects;

namespace OlapAnalytics.Application.Services;

/// <summary>
/// Orchestrates MDX query building and execution for analytics queries.
/// Maps between DTOs and domain objects, delegates building to MdxQueryBuilder
/// and execution to IMdxExecutor.
/// </summary>
public class AnalyticsService
{
    private readonly IMdxExecutor _executor;
    private readonly MdxQueryBuilder _builder;
    private readonly ILogger<AnalyticsService> _logger;

    public AnalyticsService(
        IMdxExecutor executor,
        MdxQueryBuilder builder,
        ILogger<AnalyticsService> logger)
    {
        _executor = executor;
        _builder = builder;
        _logger = logger;
    }

    /// <summary>Executes a dynamic analytics query based on the request DTO.</summary>
    public async Task<QueryResponseDto> ExecuteQueryAsync(
        QueryRequestDto request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Building MDX query. Measures: {Measures}, RowDim: {RowDim}",
            string.Join(",", request.Measures), request.RowDimension);

        var cubeName = await _executor.GetActiveCubeNameAsync(cancellationToken);
        if (string.IsNullOrEmpty(cubeName))
            throw new InvalidOperationException("Hệ thống chưa tìm thấy Cube dữ liệu. Vui lòng thực hiện 'Phân tích' dữ liệu trước.");

        // Build base MDX query
        var mdx = await _builder.BuildMdxQueryAsync(
            cubeName,
            request.Measures,
            request.RowDimension,
            request.ColumnDimension,
            request.RowLevel);

        // Apply Top N if requested
        if (request.TopN > 0 && request.Measures.Any())
        {
            var dateRange = MapDateRange(request.DateRange);
            mdx = await _builder.BuildTopNQueryAsync(cubeName, request.Measures.First(), request.RowDimension, request.TopN, request.RowLevel, dateRange);
        }

        // Apply date range and dimension filters
        if (request.DateRange != null || request.Filters.Any())
        {
            var dateRange = MapDateRange(request.DateRange);
            var filters = request.Filters.Select(f => new DimensionFilter
            {
                DimensionName = f.DimensionName,
                LevelName = f.LevelName ?? string.Empty,
                MemberValues = f.MemberValues,
                IsExclude = f.IsExclude
            });
            mdx = await _builder.ApplyFiltersAsync(mdx, filters, dateRange);
        }

        // Apply drill-down
        if (request.DrillDown != null)
        {
            var drillPath = new DrillPath
            {
                DimensionUniqueName = request.DrillDown.DimensionName,
                FromLevel = request.DrillDown.FromLevel,
                ToLevel = request.DrillDown.ToLevel,
                MemberValue = request.DrillDown.MemberValue
            };
            mdx = request.DrillDown.IsDrillUp
                ? await _builder.ApplyDrillUpAsync(mdx, drillPath)
                : await _builder.ApplyDrillDownAsync(mdx, drillPath);
        }

        // Apply slice/dice
        if (request.SliceDice?.DimensionMembers.Any() == true)
        {
            mdx = await _builder.ApplyDiceAsync(mdx, request.SliceDice.DimensionMembers);
        }

        // Execute
        var result = await _executor.ExecuteQueryAsync(mdx, cancellationToken);
        return MapToResponseDto(result);
    }

    /// <summary>Returns all available dimensions from the cube.</summary>
    public async Task<IEnumerable<DimensionDto>> GetDimensionsAsync(CancellationToken cancellationToken = default)
    {
        var dimensions = await _executor.GetDimensionsAsync(cancellationToken);
        return dimensions.Select(d => new DimensionDto
        {
            Name = d.Name,
            UniqueName = d.UniqueName,
            Caption = d.Caption,
            Hierarchies = d.Hierarchies.Select(h => new HierarchyDto
            {
                Name = h.Name,
                UniqueName = h.UniqueName,
                Levels = h.Levels.Select(l => new LevelDto
                {
                    Name = l.Name,
                    UniqueName = l.UniqueName,
                    LevelNumber = l.LevelNumber
                }).ToList()
            }).ToList()
        });
    }

    /// <summary>Returns all available measures from the cube.</summary>
    public async Task<IEnumerable<MeasureDto>> GetMeasuresAsync(CancellationToken cancellationToken = default)
    {
        var measures = await _executor.GetMeasuresAsync(cancellationToken);
        return measures.Select(m => new MeasureDto
        {
            Name = m.Name,
            UniqueName = m.UniqueName,
            Caption = m.Caption,
            AggregateFunction = m.AggregateFunction,
            FormatString = m.FormatString
        });
    }

    // ──── Private Helpers ─────────────────────────────────────────────────────

    private static DateRange? MapDateRange(DateRangeDto? dto)
    {
        if (dto == null) return null;
        // If no time filter is specified at all, return null (fetch all)
        if (!dto.Year.HasValue && !dto.Month.HasValue && !dto.Quarter.HasValue && !dto.Day.HasValue && !dto.Weekday.HasValue)
            return null;

        return new DateRange
        {
            YearColumn = dto.YearColumn ?? "Year",
            Year    = dto.Year,
            Quarter = dto.Quarter,
            Month   = dto.Month,
            Day     = dto.Day,
            Weekday = dto.Weekday,
        };
    }

    private static QueryResponseDto MapToResponseDto(CubeResult result)
    {
        var rowGroups = result.Cells
            .GroupBy(c => string.Join("|", c.AxisValues))
            .ToList();

        var rows = rowGroups.Select(g =>
        {
            var cells = g.ToList();
            return new QueryRowDto
            {
                AxisValues = cells.First().AxisValues,
                Values = cells.Select(c => c.Value).ToList(),
                FormattedValues = cells.Select(c => c.FormattedValue).ToList()
            };
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
