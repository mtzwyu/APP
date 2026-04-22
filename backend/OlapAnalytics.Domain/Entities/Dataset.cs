namespace OlapAnalytics.Domain.Entities;

/// <summary>
/// Represents an uploaded dataset file and its DW provisioning status.
/// </summary>
public class Dataset
{
    public int    Id        { get; set; }
    public int    UserId    { get; set; }
    public string FileName  { get; set; } = string.Empty;
    public string DbName    { get; set; } = string.Empty;   // e.g. DW_3_7
    public int?   RowCount  { get; set; }
    public string Status    { get; set; } = "pending";      // pending|processing|ready|error
    public string? ErrorMsg { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Raw Excel bytes — stored so we can re-read for processing.</summary>
    public byte[]? RawFileBytes { get; set; }

    /// <summary>JSON-serialized sample rows (up to 200 rows) for Gemini analysis.</summary>
    public string? SampleJson  { get; set; }
}

