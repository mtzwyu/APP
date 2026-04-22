namespace OlapAnalytics.Domain.Entities;

/// <summary>
/// Represents an OLAP Cube dimension (e.g. Time, Destination, Customer).
/// </summary>
public class Dimension
{
    public string Name { get; set; } = string.Empty;
    public string UniqueName { get; set; } = string.Empty;     // e.g. [Destination]
    public string Caption { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<DimensionHierarchy> Hierarchies { get; set; } = new();
}

/// <summary>
/// Represents a hierarchy within a dimension (e.g. [Time].[Calendar]).
/// </summary>
public class DimensionHierarchy
{
    public string Name { get; set; } = string.Empty;
    public string UniqueName { get; set; } = string.Empty;
    public string Caption { get; set; } = string.Empty;
    public List<DimensionLevel> Levels { get; set; } = new();
}

/// <summary>
/// Represents a single level in a hierarchy (e.g. Year, Quarter, Month).
/// </summary>
public class DimensionLevel
{
    public string Name { get; set; } = string.Empty;
    public string UniqueName { get; set; } = string.Empty;
    public string Caption { get; set; } = string.Empty;
    public int LevelNumber { get; set; }
    public List<string> Members { get; set; } = new();
}
