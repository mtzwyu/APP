using OlapAnalytics.Domain.Interfaces;

namespace OlapAnalytics.Application.Services;

/// <summary>
/// Resolves cube structure at runtime from SSAS metadata.
/// Used to eliminate ALL hardcoded dimension/measure names.
/// </summary>
public class CubeMetadataResolver
{
    private readonly IMdxExecutor _executor;

    public CubeMetadataResolver(IMdxExecutor executor)
    {
        _executor = executor;
    }

    /// <summary>
    /// Finds the first "Year" level unique name from the time dimension in the cube.
    /// Returns a proper MDX level unique name like "[Dim Time].[Year]".
    /// </summary>
    public async Task<string> GetYearLevelUniqueNameAsync(CancellationToken ct = default)
    {
        var dimensions = await _executor.GetDimensionsAsync(ct);

        // 1. Look for any dimension whose unique name contains "time" or "date"
        var timeDim = dimensions.FirstOrDefault(d =>
            d.UniqueName.Contains("time", StringComparison.OrdinalIgnoreCase) ||
            d.UniqueName.Contains("date", StringComparison.OrdinalIgnoreCase));

        if (timeDim == null)
            throw new InvalidOperationException(
                "No time/date dimension found in the cube. " +
                "Please ensure an SSAS time dimension exists.");

        // 2. Find a level whose name contains "year"
        foreach (var hier in timeDim.Hierarchies)
        {
            var yearLevel = hier.Levels
                .FirstOrDefault(l => l.Name.Contains("year", StringComparison.OrdinalIgnoreCase));
            if (yearLevel != null)
                return yearLevel.UniqueName;
        }

        // 3. Fallback: return first non-All level unique name of the time dimension
        var firstLevel = timeDim.Hierarchies
            .SelectMany(h => h.Levels)
            .FirstOrDefault();

        if (firstLevel != null)
            return firstLevel.UniqueName;

        throw new InvalidOperationException(
            $"Time dimension '{timeDim.UniqueName}' has no levels defined.");
    }

    /// <summary>
    /// Returns the first measure unique name from the cube.
    /// </summary>
    public async Task<string> GetFirstMeasureNameAsync(CancellationToken ct = default)
    {
        var measures = await _executor.GetMeasuresAsync(ct);
        var first = measures.FirstOrDefault()
            ?? throw new InvalidOperationException("No measures found in the cube.");
        return first.Name;
    }

    /// <summary>
    /// Returns the first non-time dimension unique name (for use as row dimension).
    /// </summary>
    public async Task<string> GetFirstRowDimensionUniqueNameAsync(CancellationToken ct = default)
    {
        var dimensions = await _executor.GetDimensionsAsync(ct);
        var dim = dimensions.FirstOrDefault(d =>
            !d.UniqueName.Contains("time", StringComparison.OrdinalIgnoreCase) &&
            !d.UniqueName.Contains("date", StringComparison.OrdinalIgnoreCase));

        if (dim == null)
            throw new InvalidOperationException("No non-time dimension found in the cube.");

        return dim.UniqueName;
    }
}
