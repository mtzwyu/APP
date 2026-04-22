namespace OlapAnalytics.Domain.Entities;

/// <summary>
/// Represents a single cell in the OLAP cube result.
/// </summary>
public class CubeCell
{
    public List<string> AxisValues { get; set; } = new();
    public decimal Value { get; set; }
    public string FormattedValue { get; set; } = string.Empty;
    public string MeasureName { get; set; } = string.Empty;
}

/// <summary>
/// Represents the full result returned from an MDX query execution.
/// </summary>
public class CubeResult
{
    public List<string> ColumnHeaders { get; set; } = new();
    public List<CubeCell> Cells { get; set; } = new();
    public string ExecutedMdx { get; set; } = string.Empty;
    public TimeSpan ExecutionTime { get; set; }
    public bool FromCache { get; set; }
    public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;
    public int TotalRows => Cells.Count;
}
