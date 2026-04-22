namespace OlapAnalytics.Application.DTOs.Upload;

// ─── Column Mapping (Gemini output) ─────────────────────────────────────────

/// <summary>Maps one source Excel column to a target DW table/column.</summary>
public class ColumnMappingDto
{
    public string SourceColumn  { get; set; } = string.Empty;
    public string TargetTable   { get; set; } = string.Empty;
    public string TargetColumn  { get; set; } = string.Empty;
    /// <summary>dimension | measure | date | key | skip</summary>
    public string Role          { get; set; } = string.Empty;
}



// ─── Upload ────────────────────────────────────────────────────────────────

public class UploadResponse
{
    public int    DatasetId { get; set; }
    public string FileName  { get; set; } = string.Empty;
    public int    RowCount  { get; set; }
    public List<string> Columns { get; set; } = new();
    public List<Dictionary<string, object?>> Preview { get; set; } = new();
}

// ─── Process ───────────────────────────────────────────────────────────────

public class ProcessRequest
{
    public int DatasetId { get; set; }
}

public class ProcessResponse
{
    public int    DatasetId { get; set; }
    public string DbName    { get; set; } = string.Empty;
    public string Status    { get; set; } = string.Empty;
    public List<InsightDto> Insights  { get; set; } = new();
    public SchemaDto?       Schema    { get; set; }
}

// ─── Shared ────────────────────────────────────────────────────────────────

public class InsightDto
{
    public string Title       { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Explanation { get; set; } = string.Empty;
    public string Metric      { get; set; } = string.Empty;
    public double Value       { get; set; }
}

public class SchemaDto
{
    public List<ColumnInfo>     Columns         { get; set; } = new();
    public TableInfo?           FactTable       { get; set; }
    public List<TableInfo>      DimensionTables { get; set; } = new();
    public List<RelationshipInfo> Relationships { get; set; } = new();
}

public class ColumnInfo
{
    public string Name        { get; set; } = string.Empty;
    public string Type        { get; set; } = string.Empty;
    public string Role        { get; set; } = string.Empty;   // fact | dimension | date
    public bool   IsNullable  { get; set; }
}

public class TableInfo
{
    public string       Name    { get; set; } = string.Empty;
    public List<string> Columns { get; set; } = new();
}

public class RelationshipInfo
{
    public string FromTable  { get; set; } = string.Empty;
    public string FromColumn { get; set; } = string.Empty;
    public string ToTable    { get; set; } = string.Empty;
    public string ToColumn   { get; set; } = string.Empty;
}

// ─── Dataset List ──────────────────────────────────────────────────────────

public class DatasetDto
{
    public int    Id        { get; set; }
    public string FileName  { get; set; } = string.Empty;
    public string DbName    { get; set; } = string.Empty;
    public string Status    { get; set; } = string.Empty;
    public int?   RowCount  { get; set; }
    public string? ErrorMsg { get; set; }
    public DateTime CreatedAt { get; set; }
}
