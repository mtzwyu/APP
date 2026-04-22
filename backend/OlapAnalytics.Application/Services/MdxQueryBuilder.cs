using OlapAnalytics.Domain.Interfaces;
using OlapAnalytics.Domain.ValueObjects;
using System.Text;

namespace OlapAnalytics.Application.Services;

/// <summary>
/// Builds MDX queries dynamically by resolving exact metadata from SSAS.
/// Support: SELECT, WHERE (Slicer), DrillDown, Slice/Dice, Top N.
/// </summary>
public class MdxQueryBuilder
{
    private readonly IMdxExecutor _executor;

    public MdxQueryBuilder(IMdxExecutor executor)
    {
        _executor = executor;
    }

    // ──── Public Builder Methods ──────────────────────────────────────────────

    public async Task<string> BuildMdxQueryAsync(
        string cubeName,
        IEnumerable<string> measures,
        string rowDimension,
        string? columnDimension = null,
        string? rowLevel = null,
        bool membersOnly = true)
    {
        var sb = new StringBuilder();
        var measureList = new List<string>();
        foreach (var m in measures)
        {
            measureList.Add(await ResolveMeasureAsync(m));
        }

        if (measureList.Count > 0)
        {
            var cols = string.Join(", ", measureList);
            sb.Append($"SELECT {{ {cols} }} ON COLUMNS");
        }

        if (!string.IsNullOrEmpty(rowDimension))
        {
            var rowDimArray = rowDimension.Split(',').Select(x => x.Trim()).Where(x => !string.IsNullOrEmpty(x)).ToList();
            var dimensionAxes = new List<string>();
            foreach (var rd in rowDimArray)
            {
                var mdxPart = await BuildRowAxisAsync(rd, rowLevel, membersOnly);
                if (mdxPart != "{}") dimensionAxes.Add(mdxPart);
            }

            if (dimensionAxes.Count > 0)
            {
                if (measureList.Count > 0) sb.Append(",");
                sb.AppendLine();
                var crossJoined = string.Join(" * ", dimensionAxes);
                sb.Append($"NON EMPTY {{ {crossJoined} }} ON ROWS");
            }
        }

        sb.AppendLine();
        sb.Append($"FROM {cubeName}");
        return sb.ToString();
    }

    public async Task<string> ApplyFiltersAsync(string mdxQuery, IEnumerable<DimensionFilter> filters, DateRange? dateRange = null)
    {
        var slicers = new List<string>();

        if (dateRange != null && !string.IsNullOrEmpty(dateRange.YearColumn))
        {
            // Resolve Year level path
            var resolvedDateDim = await ResolveDimensionAndLevelAsync(dateRange.YearColumn, null);
            var timePrefix = resolvedDateDim.DimensionMdx;
            var timeLevelPrefix = $"{timePrefix}.[{resolvedDateDim.LevelName ?? "Year"}]"; 
            
            // If yearCol already has brackets, use it as is
            if (dateRange.YearColumn.StartsWith("[")) {
                timeLevelPrefix = dateRange.YearColumn;
                timePrefix = dateRange.YearColumn.Substring(0, dateRange.YearColumn.LastIndexOf(".["));
            }

            if (dateRange.Year.HasValue)
                slicers.Add($"{timeLevelPrefix}.&[{dateRange.Year}]");
            
            if (dateRange.Quarter.HasValue)
                slicers.Add($"{timePrefix}.[Quarter].&[{dateRange.Quarter}]");
            if (dateRange.Month.HasValue)
                slicers.Add($"{timePrefix}.[Month].&[{dateRange.Month}]");
            if (dateRange.Day.HasValue)
                slicers.Add($"{timePrefix}.[Day].&[{dateRange.Day}]");
            if (dateRange.Weekday.HasValue)
                slicers.Add($"{timePrefix}.[Day Of Week].&[{dateRange.Weekday}]");
        }

        foreach (var filter in filters)
        {
            var resolved = await ResolveDimensionAndLevelAsync(filter.DimensionName, filter.LevelName);
            var dimName = resolved.DimensionMdx;
            var levelName = string.IsNullOrEmpty(resolved.LevelName)
                ? $"{dimName}"
                : $"{dimName}.[{resolved.LevelName}]";

            if (filter.MemberValues.Any())
            {
                var members = filter.MemberValues
                    .Select(v => $"{levelName}.&[{v}]");

                if (filter.MemberValues.Count == 1)
                    slicers.Add(members.First());
                else
                    slicers.Add($"{{ {string.Join(", ", members)} }}");
            }
        }

        if (!slicers.Any()) return mdxQuery;

        var whereClause = slicers.Count == 1
            ? $"\nWHERE ({slicers[0]})"
            : $"\nWHERE ({string.Join(", ", slicers)})";

        // Handle existing WHERE clause
        var whereIdx = mdxQuery.IndexOf("\nWHERE", StringComparison.OrdinalIgnoreCase);
        if (whereIdx < 0) whereIdx = mdxQuery.IndexOf(" WHERE", StringComparison.OrdinalIgnoreCase);

        if (whereIdx >= 0)
        {
            // Extract existing slicers from WHERE clause
            var existingWhere = mdxQuery[whereIdx..];
            mdxQuery = mdxQuery[..whereIdx];
            
            // Basic extraction: find content between ( and )
            var startParen = existingWhere.IndexOf("(");
            var endParen = existingWhere.LastIndexOf(")");
            if (startParen >= 0 && endParen > startParen)
            {
                var content = existingWhere.Substring(startParen + 1, endParen - startParen - 1);
                // Simple merge - assumes slicers are comma separated in MDX
                whereClause = $"\nWHERE ({content}, {string.Join(", ", slicers)})";
            }
        }

        return mdxQuery + whereClause;
    }

    public async Task<string> ApplyDrillDownAsync(string mdxQuery, DrillPath drillPath)
    {
        var resolved = await ResolveDimensionAndLevelAsync(drillPath.DimensionUniqueName, drillPath.ToLevel);
        var dimName = resolved.DimensionMdx;
        
        string rowExpr;
        if (!string.IsNullOrEmpty(drillPath.MemberValue))
        {
            // If levels are missing, we use .Children on the member
            if (string.IsNullOrEmpty(drillPath.FromLevel))
            {
                // Try to find the member in the dimension
                // Simple approach: Use [Dim].[Member].Children
                // More complex: Find the actual level of the member
                rowExpr = $"{{ {dimName}.&[{drillPath.MemberValue}].Children }}";
                
                // If it's a date-like dimension, it might need different logic, but .Children is usually safe in SSAS
            }
            else 
            {
                var fromResolved = await ResolveDimensionAndLevelAsync(drillPath.DimensionUniqueName, drillPath.FromLevel);
                var parentMember = $"{dimName}.[{fromResolved.LevelName ?? drillPath.FromLevel}].&[{drillPath.MemberValue}]";
                
                if (string.IsNullOrEmpty(drillPath.ToLevel))
                {
                    rowExpr = $"{{ {parentMember}.Children }}";
                }
                else
                {
                    var toLevel = $"{dimName}.[{resolved.LevelName ?? drillPath.ToLevel}]";
                    rowExpr = $"DRILLDOWNMEMBER({{{toLevel}.MEMBERS}}, {{{parentMember}}})";
                }
            }
        }
        else
        {
            var toLevel = $"{dimName}.[{resolved.LevelName ?? drillPath.ToLevel}]";
            rowExpr = $"{toLevel}.MEMBERS";
        }

        return ReplaceRowsAxis(mdxQuery, $"NON EMPTY {{ {rowExpr} }} ON ROWS");
    }

    public async Task<string> ApplyDrillUpAsync(string mdxQuery, DrillPath drillPath)
    {
        var resolved = await ResolveDimensionAndLevelAsync(drillPath.DimensionUniqueName, drillPath.ToLevel);
        var dimName = resolved.DimensionMdx;
        
        string rowExpr;
        if (string.IsNullOrEmpty(drillPath.ToLevel))
        {
            // Default to the first level of the dimension if possible, or just .MEMBERS
            rowExpr = $"{dimName}.MEMBERS";
        }
        else
        {
            var upLevel = $"{dimName}.[{resolved.LevelName ?? drillPath.ToLevel}]";
            rowExpr = $"{upLevel}.MEMBERS";
        }
        
        return ReplaceRowsAxis(mdxQuery, $"NON EMPTY {{ {rowExpr} }} ON ROWS");
    }

    public async Task<string> ApplySliceAsync(string mdxQuery, string dimension, string memberValue)
    {
        var filter = new DimensionFilter
        {
            DimensionName = dimension,
            MemberValues = new List<string> { memberValue }
        };
        return await ApplyFiltersAsync(mdxQuery, new[] { filter });
    }

    public async Task<string> ApplyDiceAsync(string mdxQuery, Dictionary<string, List<string>> dimensionMembers)
    {
        var filters = dimensionMembers.Select(kv => new DimensionFilter
        {
            DimensionName = kv.Key,
            MemberValues = kv.Value
        });
        return await ApplyFiltersAsync(mdxQuery, filters);
    }

    public async Task<string> BuildTopNQueryAsync(string cubeName, string measure, string dimension, int topN, string? level = null, DateRange? dateRange = null)
    {
        var measureMdx = await ResolveMeasureAsync(measure);
        
        var rowDimArray = dimension.Split(',').Select(x => x.Trim()).Where(x => !string.IsNullOrEmpty(x)).ToList();
        var dimensionAxes = new List<string>();
        foreach (var rd in rowDimArray)
        {
            var mdxPart = await BuildRowAxisAsync(rd, level, true);
            if (mdxPart != "{}") dimensionAxes.Add(mdxPart);
        }
        var setMdx = dimensionAxes.Count > 0 ? string.Join(" * ", dimensionAxes) : "{}";

        var sb = new StringBuilder();
        sb.AppendLine($"SELECT {{ {measureMdx} }} ON COLUMNS,");
        sb.AppendLine($"NON EMPTY TOPCOUNT(");
        sb.AppendLine($"  {setMdx},");
        sb.AppendLine($"  {topN},");
        sb.AppendLine($"  {measureMdx}");
        sb.AppendLine($") ON ROWS");
        sb.Append($"FROM {cubeName}");

        var query = sb.ToString();
        return dateRange != null ? await ApplyFiltersAsync(query, Array.Empty<DimensionFilter>(), dateRange) : query;
    }

    public async Task<string> BuildTrendQueryAsync(string cubeName, string measure, string timeDimension, string granularity, int? year, IEnumerable<DimensionFilter>? extraFilters = null)
    {
        var measureMdx = await ResolveMeasureAsync(measure);
        var resolved = await ResolveDimensionAndLevelAsync(timeDimension, null);
        var dimMdx = resolved.DimensionMdx;
        
        // If dimMdx is a full UniqueName like [Dim].[Hie].[Lvl], we need the Hie or Dim prefix
        var timePrefix = dimMdx;
        if (dimMdx.StartsWith("[") && dimMdx.Count(c => c == '[') > 1)
        {
            // Get everything except the last [Level]
            var lastDot = dimMdx.LastIndexOf(".[");
            if (lastDot > 0)
                timePrefix = dimMdx.Substring(0, lastDot);
        }

        string timeLabel = "";
        
        var dimensionsList = await _executor.GetDimensionsAsync();
        var dimObj = dimensionsList.FirstOrDefault(d => dimMdx.StartsWith(d.UniqueName, StringComparison.OrdinalIgnoreCase));
        if (dimObj != null)
        {
            var matchHie = dimObj.Hierarchies.FirstOrDefault(h => dimMdx.Contains($"[{h.Name}]", StringComparison.OrdinalIgnoreCase));
            
            var mLevel = matchHie?.Levels.FirstOrDefault(l => l.Name.Contains("month", StringComparison.OrdinalIgnoreCase)) 
                         ?? dimObj.Hierarchies.SelectMany(h => h.Levels).FirstOrDefault(l => l.Name.Contains("month", StringComparison.OrdinalIgnoreCase));
            
            var qLevel = matchHie?.Levels.FirstOrDefault(l => l.Name.Contains("quarter", StringComparison.OrdinalIgnoreCase)) 
                         ?? dimObj.Hierarchies.SelectMany(h => h.Levels).FirstOrDefault(l => l.Name.Contains("quarter", StringComparison.OrdinalIgnoreCase));

            var yLevel = matchHie?.Levels.FirstOrDefault(l => l.Name.Contains("year", StringComparison.OrdinalIgnoreCase)) 
                         ?? dimObj.Hierarchies.SelectMany(h => h.Levels).FirstOrDefault(l => l.Name.Contains("year", StringComparison.OrdinalIgnoreCase));

            timeLabel = granularity.ToLower() switch
            {
                "monthly" => mLevel?.UniqueName ?? $"{timePrefix}.[Month]",
                "quarterly" => qLevel?.UniqueName ?? $"{timePrefix}.[Quarter]",
                _ => yLevel?.UniqueName ?? (resolved.LevelName != null ? $"{timePrefix}.[{resolved.LevelName}]" : $"{timePrefix}.[Year]")
            };
        }
        else
        {
            timeLabel = granularity.ToLower() switch
            {
                "monthly" => $"{timePrefix}.[Month]",
                "quarterly" => $"{timePrefix}.[Quarter]",
                _ => resolved.LevelName != null ? $"{timePrefix}.[{resolved.LevelName}]" : $"{timePrefix}.[Year]"
            };
        }
        
        var query = $"SELECT {{ {measureMdx} }} ON COLUMNS,\n" +
                    $"NON EMPTY {{ {timeLabel}.MEMBERS }} ON ROWS\n" +
                    $"FROM {cubeName}";

        var dateRange = year.HasValue ? new DateRange { YearColumn = timeDimension, Year = year } : null;
        return await ApplyFiltersAsync(query, extraFilters ?? Array.Empty<DimensionFilter>(), dateRange);
    }

    public async Task<string> BuildYoYQueryAsync(string cubeName, string measure, int currentYear, string yearColumn)
    {
        var measureMdx = await ResolveMeasureAsync(measure);
        var prevYear = currentYear - 1;
        var resolved = await ResolveDimensionAndLevelAsync(yearColumn, null);
        var timeLevel = !string.IsNullOrEmpty(yearColumn) && yearColumn.StartsWith("[") ? yearColumn : $"{resolved.DimensionMdx}.[{resolved.LevelName ?? "Year"}]";
        
        return "WITH\n" +
               $"  MEMBER [Measures].[Current Year] AS ({measureMdx}, {timeLevel}.&[{currentYear}])\n" +
               $"  MEMBER [Measures].[Previous Year] AS ({measureMdx}, {timeLevel}.&[{prevYear}])\n" +
               "  MEMBER [Measures].[YoY Growth %] AS\n" +
               "    IIF(\n" +
               "      [Measures].[Previous Year] = 0 OR ISEMPTY([Measures].[Previous Year]),\n" +
               "      NULL,\n" +
               "      ([Measures].[Current Year] - [Measures].[Previous Year]) / [Measures].[Previous Year] * 100\n" +
               "    ), FORMAT_STRING = \"#0.00%\"\n" +
               "SELECT\n" +
               "  { [Measures].[Current Year], [Measures].[Previous Year], [Measures].[YoY Growth %] } ON COLUMNS,\n" +
               "  { [Measures].[Current Year] } ON ROWS\n" + 
               $"FROM {cubeName}";
    }

    public async Task<string> BuildMoMQueryAsync(string cubeName, string measure, int year, string yearColumn)
    {
        var measureMdx = await ResolveMeasureAsync(measure);
        var prevYear = year - 1;
        var resolved = await ResolveDimensionAndLevelAsync(yearColumn, null);
        var timeLevel = !string.IsNullOrEmpty(yearColumn) && yearColumn.StartsWith("[") ? yearColumn : $"{resolved.DimensionMdx}.[{resolved.LevelName ?? "Year"}]";
        var timePrefix = !string.IsNullOrEmpty(yearColumn) && yearColumn.StartsWith("[") ? yearColumn.Substring(0, yearColumn.LastIndexOf(".[")) : resolved.DimensionMdx;
        
        string monthUniqueName = $"{timePrefix}.[Month]";
        var dimensionsList = await _executor.GetDimensionsAsync();
        var dimObj = dimensionsList.FirstOrDefault(d => yearColumn.StartsWith(d.UniqueName, StringComparison.OrdinalIgnoreCase));
        if (dimObj != null)
        {
            var mLevel = dimObj.Hierarchies.SelectMany(h => h.Levels).FirstOrDefault(l => l.Name.Contains("month", StringComparison.OrdinalIgnoreCase));
            if (mLevel != null) monthUniqueName = mLevel.UniqueName;
        }

        return "WITH\n" +
               "  MEMBER [Measures].[MoM Growth %] AS\n" +
               "    IIF(\n" +
               $"      (ParallelPeriod({monthUniqueName}, 1, {monthUniqueName}.CurrentMember), {measureMdx}) = 0,\n" +
               "      NULL,\n" +
               $"      ({measureMdx} - (ParallelPeriod({monthUniqueName}, 1, {monthUniqueName}.CurrentMember), {measureMdx}))\n" +
               $"      / (ParallelPeriod({monthUniqueName}, 1, {monthUniqueName}.CurrentMember), {measureMdx}) * 100\n" +
               "    ), FORMAT_STRING = \"#0.00%\"\n" +
               "SELECT\n" +
               $"  {{ {measureMdx}, [Measures].[MoM Growth %] }} ON COLUMNS,\n" +
               $"  NON EMPTY {{ {monthUniqueName}.MEMBERS }} ON ROWS\n" +
               $"FROM {cubeName}\n" +
               $"WHERE ({timeLevel}.&[{year}])";
    }

    // ──── Private Helpers ─────────────────────────────────────────────────────

    private async Task<string> ResolveMeasureAsync(string? measureKey)
    {
        if (string.IsNullOrWhiteSpace(measureKey)) return string.Empty;
        if (measureKey.StartsWith("[Measures]")) return measureKey;

        var measures = await _executor.GetMeasuresAsync();
        
        var m = measures.FirstOrDefault(x => string.Equals(x.Name, measureKey, StringComparison.OrdinalIgnoreCase)
                                          || string.Equals(x.Caption, measureKey, StringComparison.OrdinalIgnoreCase)
                                          || string.Equals(x.UniqueName, measureKey, StringComparison.OrdinalIgnoreCase));
        if (m != null) return m.UniqueName;

        var fuzzy = measures.FirstOrDefault(x => x.Name.Contains(measureKey, StringComparison.OrdinalIgnoreCase)
                                              || x.Caption.Contains(measureKey, StringComparison.OrdinalIgnoreCase));
        if (fuzzy != null) return fuzzy.UniqueName;

        return $"[Measures].[{measureKey}]";
    }

    private async Task<(string DimensionMdx, string? LevelName)> ResolveDimensionAndLevelAsync(string? dimKey, string? levelKey = null)
    {
        if (string.IsNullOrWhiteSpace(dimKey)) return (string.Empty, levelKey);
        if (dimKey.StartsWith("[")) return (dimKey, levelKey);

        var dimensions = await _executor.GetDimensionsAsync();

        // Match dimension by Name or Caption
        var d = dimensions.FirstOrDefault(x => string.Equals(x.Name, dimKey, StringComparison.OrdinalIgnoreCase)
                                            || string.Equals(x.Caption, dimKey, StringComparison.OrdinalIgnoreCase));
        if (d != null)
        {
            if (!string.IsNullOrEmpty(levelKey))
            {
                var lvl = d.Hierarchies.SelectMany(h => h.Levels)
                    .FirstOrDefault(l => string.Equals(l.Name, levelKey, StringComparison.OrdinalIgnoreCase) || string.Equals(l.Caption, levelKey, StringComparison.OrdinalIgnoreCase));
                if (lvl != null) return (d.UniqueName, lvl.Name);
            }
            return (d.UniqueName, levelKey);
        }

        // If DimKey is actually a Level or Hierarchy Name
        var searchKey = string.IsNullOrEmpty(levelKey) ? dimKey : levelKey;
        if (!string.IsNullOrEmpty(searchKey)) 
        {
            foreach (var dim in dimensions)
            {
                foreach (var h in dim.Hierarchies)
                {
                    if (string.Equals(h.Name, searchKey, StringComparison.OrdinalIgnoreCase)) return (dim.UniqueName, h.Name);
                    
                    var lvl = h.Levels.FirstOrDefault(l => string.Equals(l.Name, searchKey, StringComparison.OrdinalIgnoreCase) 
                                                        || string.Equals(l.Caption, searchKey, StringComparison.OrdinalIgnoreCase));
                    if (lvl != null) return (dim.UniqueName, lvl.Name);
                }
            }
        }
        
        // Fuzzy dimension match
        var fuzzy = dimensions.FirstOrDefault(x => x.Name.Contains(dimKey, StringComparison.OrdinalIgnoreCase));
        if (fuzzy != null) return (fuzzy.UniqueName, levelKey);

        return ($"[{dimKey}]", levelKey);
    }

    private async Task<string> BuildRowAxisAsync(string rowDimension, string? rowLevel, bool membersOnly)
    {
        var resolved = await ResolveDimensionAndLevelAsync(rowDimension, rowLevel);
        var dimMdx = resolved.DimensionMdx;
        var finalLevel = resolved.LevelName;

        if (string.IsNullOrEmpty(dimMdx)) return "{}";

        if (!string.IsNullOrEmpty(finalLevel))
            return $"{dimMdx}.[{finalLevel}].MEMBERS";
        return $"{dimMdx}.MEMBERS";
    }

    private string ReplaceRowsAxis(string mdxQuery, string newRowsExpr)
    {
        var rowsIdx = mdxQuery.IndexOf("ON ROWS", StringComparison.OrdinalIgnoreCase);
        if (rowsIdx < 0) return mdxQuery;

        var nonEmptyIdx = mdxQuery.LastIndexOf("NON EMPTY", rowsIdx, StringComparison.OrdinalIgnoreCase);
        if (nonEmptyIdx < 0) nonEmptyIdx = mdxQuery.LastIndexOf(",", rowsIdx);

        var prefix = mdxQuery[..(nonEmptyIdx >= 0 ? nonEmptyIdx : rowsIdx)].TrimEnd();
        var suffix = mdxQuery[(rowsIdx + "ON ROWS".Length)..];
        return $"{prefix}\n{newRowsExpr}{suffix}";
    }
}
