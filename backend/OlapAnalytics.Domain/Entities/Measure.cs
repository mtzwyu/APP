namespace OlapAnalytics.Domain.Entities;

/// <summary>
/// Represents a measure in the OLAP cube (e.g. Trip Cost, Duration Days).
/// </summary>
public class Measure
{
    public string Name { get; set; } = string.Empty;
    public string UniqueName { get; set; } = string.Empty;    // e.g. [Measures].[Trip Cost]
    public string Caption { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string DataType { get; set; } = "Numeric";
    public string AggregateFunction { get; set; } = "Sum";    // Sum, Avg, Count, Min, Max
    public string FormatString { get; set; } = "#,##0.00";
}
