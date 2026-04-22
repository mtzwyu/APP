namespace OlapAnalytics.Domain.ValueObjects;

/// <summary>
/// Represents a date range filter for MDX queries.
/// </summary>
public record DateRange
{
    public int? Year { get; init; }
    public int? Quarter { get; init; }
    public int? Month { get; init; }
    public int? Day { get; init; }      // 1-31
    public int? Weekday { get; init; } // 1=Monday...7=Sunday
    public string YearColumn { get; init; } = "Year";
    public DateTime? StartDate { get; init; }
    public DateTime? EndDate { get; init; }

    public DateRange() { }
    public DateRange(int year) => Year = year;
    public DateRange(int year, int quarter) { Year = year; Quarter = quarter; }
    public DateRange(int year, int quarter, int month) { Year = year; Quarter = quarter; Month = month; }

    public override string ToString()
    {
        var parts = new List<string>();
        if (Year.HasValue) parts.Add($"Year {Year}");
        if (Quarter.HasValue) parts.Add($"Q{Quarter}");
        if (Month.HasValue) parts.Add($"Month {Month}");
        if (Day.HasValue) parts.Add($"Day {Day}");
        if (Weekday.HasValue) parts.Add($"Weekday {Weekday}");
        return parts.Any() ? string.Join(", ", parts) : "All Time";
    }
}

/// <summary>
/// Represents a filter for a specific dimension member.
/// </summary>
public record DimensionFilter
{
    public string DimensionName { get; init; } = string.Empty;
    public string HierarchyName { get; init; } = string.Empty;
    public string LevelName { get; init; } = string.Empty;
    public List<string> MemberValues { get; init; } = new();
    public bool IsExclude { get; init; } = false;
}

/// <summary>
/// Represents a drill-down path from one level to a deeper level.
/// </summary>
public record DrillPath
{
    public string DimensionUniqueName { get; init; } = string.Empty;  // e.g. [Destination]
    public string HierarchyUniqueName { get; init; } = string.Empty;  // e.g. [Destination].[Destination]
    public string FromLevel { get; init; } = string.Empty;            // e.g. [All]
    public string ToLevel { get; init; } = string.Empty;              // e.g. [Country]
    public string? MemberValue { get; init; }                         // e.g. "Vietnam" for drill into
}
