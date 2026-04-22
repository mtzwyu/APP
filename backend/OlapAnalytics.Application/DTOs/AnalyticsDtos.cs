using System.ComponentModel.DataAnnotations;

namespace OlapAnalytics.Application.DTOs;

// ──────────────────────────────────────────────────────────────────────────────
// REQUEST DTOs
// ──────────────────────────────────────────────────────────────────────────────

/// <summary>Request body for POST /api/query</summary>
public class QueryRequestDto
{
    [Required]
    public List<string> Measures { get; set; } = new();        // e.g. ["Trip Cost", "Duration Days"]

    public string RowDimension { get; set; } = string.Empty;   // e.g. "destination, time"

    public string? ColumnDimension { get; set; }               // Optional second dimension
    public string? RowLevel { get; set; }                      // e.g. "Country" or "Destination"
    public DateRangeDto? DateRange { get; set; }
    public List<DimensionFilterDto> Filters { get; set; } = new();
    public DrillPathDto? DrillDown { get; set; }
    public SliceDiceDto? SliceDice { get; set; }

    [Range(0, 1000)]
    public int TopN { get; set; } = 0;                         // 0 = all
}

/// <summary>Request body for GET /api/kpi</summary>
public class KpiRequestDto
{
    [Required]
    public string Measure { get; set; } = "Trip Cost";
    public string YearColumn { get; set; } = "Year";
    public int? Year { get; set; }
    public int? PreviousYear { get; set; }
    public int? Month { get; set; }
    public List<DimensionSlicerDto> Slicers { get; set; } = new();
}

/// <summary>Request body for GET /api/trend</summary>
public class TrendRequestDto
{
    [Required]
    public string Measure { get; set; } = "Trip Cost";
    public string Granularity { get; set; } = "Monthly";       // Monthly, Quarterly, Yearly
    public string YearColumn { get; set; } = "Year";
    public int? Year { get; set; }
    public int TopN { get; set; } = 5;
    public List<DimensionSlicerDto> Slicers { get; set; } = new();
}

/// <summary>A single dimension-level slicer (filter by a specific member value of a level).</summary>
public class DimensionSlicerDto
{
    public string DimensionUniqueName { get; set; } = string.Empty;  // e.g. [Dim Time].[Month Start].[Month Start]
    public string MemberValue { get; set; } = string.Empty;          // e.g. "3" for March
}

// ──────────────────────────────────────────────────────────────────────────────
// SUB-DTOs
// ──────────────────────────────────────────────────────────────────────────────

public class DateRangeDto
{
    public string YearColumn { get; set; } = "Year";
    public int? Year { get; set; }
    public int? Quarter { get; set; }
    public int? Month { get; set; }
    public int? Day { get; set; }           // 1-31
    public int? Weekday { get; set; }       // 1=Monday ... 7=Sunday
}

public class DimensionFilterDto
{
    public string DimensionName { get; set; } = string.Empty;
    public string? LevelName { get; set; }
    public List<string> MemberValues { get; set; } = new();
    public bool IsExclude { get; set; } = false;
}

public class DrillPathDto
{
    public string DimensionName { get; set; } = string.Empty;
    public string FromLevel { get; set; } = string.Empty;
    public string ToLevel { get; set; } = string.Empty;
    public string? MemberValue { get; set; }
    public bool IsDrillUp { get; set; } = false;
}

public class SliceDiceDto
{
    /// <summary>Key = dimension name, Value = list of member values</summary>
    public Dictionary<string, List<string>> DimensionMembers { get; set; } = new();
}

// ──────────────────────────────────────────────────────────────────────────────
// RESPONSE DTOs
// ──────────────────────────────────────────────────────────────────────────────

/// <summary>Standard API response wrapper</summary>
public class ApiResponse<T>
{
    public bool Success { get; set; } = true;
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
    public string? Error { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public static ApiResponse<T> Ok(T data, string message = "") =>
        new() { Success = true, Data = data, Message = message };

    public static ApiResponse<T> Fail(string error) =>
        new() { Success = false, Error = error };
}

/// <summary>Response for query results</summary>
public class QueryResponseDto
{
    public List<string> Headers { get; set; } = new();
    public List<QueryRowDto> Rows { get; set; } = new();
    public string ExecutedMdx { get; set; } = string.Empty;
    public long ExecutionTimeMs { get; set; }
    public bool FromCache { get; set; }
    public int TotalRows { get; set; }
}

public class QueryRowDto
{
    public List<string> AxisValues { get; set; } = new();       // Row labels
    public List<decimal> Values { get; set; } = new();          // Measure values
    public List<string> FormattedValues { get; set; } = new();  // Formatted strings
}

/// <summary>Response for dimension metadata</summary>
public class DimensionDto
{
    public string Name { get; set; } = string.Empty;
    public string UniqueName { get; set; } = string.Empty;
    public string Caption { get; set; } = string.Empty;
    public List<HierarchyDto> Hierarchies { get; set; } = new();
}

public class HierarchyDto
{
    public string Name { get; set; } = string.Empty;
    public string UniqueName { get; set; } = string.Empty;
    public List<LevelDto> Levels { get; set; } = new();
}

public class LevelDto
{
    public string Name { get; set; } = string.Empty;
    public string UniqueName { get; set; } = string.Empty;
    public int LevelNumber { get; set; }
}

/// <summary>Response for measure metadata</summary>
public class MeasureDto
{
    public string Name { get; set; } = string.Empty;
    public string UniqueName { get; set; } = string.Empty;
    public string Caption { get; set; } = string.Empty;
    public string AggregateFunction { get; set; } = string.Empty;
    public string FormatString { get; set; } = string.Empty;
}

/// <summary>Response for KPI calculations</summary>
public class KpiDto
{
    public string MeasureName { get; set; } = string.Empty;
    public decimal CurrentValue { get; set; }
    public decimal PreviousValue { get; set; }
    public decimal GrowthRate { get; set; }
    public decimal YearOverYear { get; set; }
    public decimal MonthOverMonth { get; set; }
    public string TrendDirection { get; set; } = string.Empty;
    public string Period { get; set; } = string.Empty;
    public string FormattedCurrentValue { get; set; } = string.Empty;
    public string FormattedGrowthRate { get; set; } = string.Empty;
}

/// <summary>Response for trend analysis</summary>
public class TrendDto
{
    public string Measure { get; set; } = string.Empty;
    public string Granularity { get; set; } = string.Empty;
    public List<TrendDataPointDto> DataPoints { get; set; } = new();
}

public class TrendDataPointDto
{
    public string Period { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public string FormattedValue { get; set; } = string.Empty;
    public decimal? GrowthRate { get; set; }
}
