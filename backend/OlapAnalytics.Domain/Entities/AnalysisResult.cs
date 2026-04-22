namespace OlapAnalytics.Domain.Entities;

/// <summary>
/// Stores the Gemini AI analysis output for a dataset.
/// </summary>
public class AnalysisResult
{
    public int    Id           { get; set; }
    public int    DatasetId    { get; set; }
    public string? SchemaJson  { get; set; }    // full AI response JSON
    public string? InsightsJson { get; set; }   // array of insight objects
    public string? SsasCubeJson { get; set; }  // SSAS cube definition
    public string? SqlScript   { get; set; }   // generated SQL DDL
    public DateTime CreatedAt  { get; set; } = DateTime.UtcNow;
}
