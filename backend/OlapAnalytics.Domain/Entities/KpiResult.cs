namespace OlapAnalytics.Domain.Entities;

/// <summary>
/// Represents a KPI calculation result.
/// </summary>
public class KpiResult
{
    public string KpiName { get; set; } = string.Empty;
    public string Dimension { get; set; } = string.Empty;
    public string Measure { get; set; } = string.Empty;
    public decimal CurrentValue { get; set; }
    public decimal PreviousValue { get; set; }
    public decimal GrowthRate { get; set; }          // Percentage growth
    public decimal YearOverYear { get; set; }        // YoY %
    public decimal MonthOverMonth { get; set; }      // MoM %
    public string Period { get; set; } = string.Empty;
    public string TrendDirection { get; set; } = string.Empty;  // "up", "down", "flat"
    public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;
}
