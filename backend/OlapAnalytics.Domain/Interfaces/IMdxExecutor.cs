using OlapAnalytics.Domain.Entities;

namespace OlapAnalytics.Domain.Interfaces;

/// <summary>
/// Contract for executing MDX queries against an OLAP data source (SSAS).
/// </summary>
public interface IMdxExecutor
{
    /// <summary>Executes an MDX query string and returns parsed results.</summary>
    Task<CubeResult> ExecuteQueryAsync(string mdxQuery, CancellationToken cancellationToken = default);

    /// <summary>Returns all available dimensions for the connected cube.</summary>
    Task<IEnumerable<Dimension>> GetDimensionsAsync(CancellationToken cancellationToken = default);

    /// <summary>Returns all available measures for the connected cube.</summary>
    Task<IEnumerable<Measure>> GetMeasuresAsync(CancellationToken cancellationToken = default);

    /// <summary>Tests the SSAS connection and returns true if healthy.</summary>
    Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default);

    /// <summary>Finds the first available user cube name dynamically.</summary>
    Task<string?> GetActiveCubeNameAsync(CancellationToken cancellationToken = default);
}
