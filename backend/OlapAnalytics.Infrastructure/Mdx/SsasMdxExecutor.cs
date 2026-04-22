using Microsoft.AnalysisServices.AdomdClient;
using Microsoft.Extensions.Logging;
using OlapAnalytics.Domain.Interfaces;
using OlapAnalytics.Application.Interfaces;
using System.Diagnostics;
using DomainDimension = OlapAnalytics.Domain.Entities.Dimension;
using DomainDimensionHierarchy = OlapAnalytics.Domain.Entities.DimensionHierarchy;
using DomainDimensionLevel = OlapAnalytics.Domain.Entities.DimensionLevel;
using DomainMeasure = OlapAnalytics.Domain.Entities.Measure;
using DomainCubeResult = OlapAnalytics.Domain.Entities.CubeResult;
using DomainCubeCell = OlapAnalytics.Domain.Entities.CubeCell;

namespace OlapAnalytics.Infrastructure.Mdx;

/// <summary>
/// Executes MDX queries against SSAS using ADOMD.NET.
/// Connection: Data Source=LAPTOP-KQVRNVI2\MANHTRUONG1; Initial Catalog=SSAS_Travel
/// </summary>
public class SsasMdxExecutor : IMdxExecutor
{
    private readonly ITenantConnectionProvider _connectionProvider;
    private readonly ILogger<SsasMdxExecutor> _logger;

    public SsasMdxExecutor(ITenantConnectionProvider connectionProvider, ILogger<SsasMdxExecutor> logger)
    {
        _connectionProvider = connectionProvider ?? throw new ArgumentNullException(nameof(connectionProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    private async Task<AdomdConnection> CreateConnectionAsync() 
    {
        var connStr = await _connectionProvider.GetSsasConnectionStringAsync();
        if (string.IsNullOrWhiteSpace(connStr))
            throw new InvalidOperationException("SSAS connection string is not configured for this user.");
        return new AdomdConnection(connStr);
    }


    // ──── IMdxExecutor Implementation ────────────────────────────────────────

    /// <inheritdoc/>
    public async Task<DomainCubeResult> ExecuteQueryAsync(string mdxQuery, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(mdxQuery))
            throw new ArgumentException("MDX query cannot be empty.", nameof(mdxQuery));

        _logger.LogInformation("Executing MDX query: {Query}", mdxQuery);
        var stopwatch = Stopwatch.StartNew();
        var result = new DomainCubeResult { ExecutedMdx = mdxQuery };

        await Task.Run(async () =>
        {
            using var connection = await CreateConnectionAsync();
            try
            {
                connection.Open();
                using var command = new AdomdCommand(mdxQuery, connection);
                command.CommandTimeout = 120;

                var reader = command.ExecuteCellSet(); // CellSet does not implement IDisposable

                // Extract column headers from COLUMNS axis (Axis 0)
                if (reader.Axes.Count > 0)
                {
                    foreach (Position position in reader.Axes[0].Positions)
                    {
                        var header = string.Join(" / ", position.Members.Cast<Member>()
                            .Select(m => m.Caption));
                        result.ColumnHeaders.Add(header);
                    }
                }

                // Extract cells from ROWS axis (Axis 1)
                if (reader.Axes.Count > 1)
                {
                    int rowIdx = 0;
                    foreach (Position rowPosition in reader.Axes[1].Positions)
                    {
                        for (int colIdx = 0; colIdx < reader.Axes[0].Positions.Count; colIdx++)
                        {
                            cancellationToken.ThrowIfCancellationRequested();

                            var cell = reader.Cells[colIdx, rowIdx];
                            var cubeCell = new DomainCubeCell
                            {
                                AxisValues = rowPosition.Members.Cast<Member>()
                                    .Select(m => m.Caption).ToList(),
                                FormattedValue = cell.FormattedValue ?? string.Empty,
                                MeasureName = result.ColumnHeaders.Count > colIdx
                                    ? result.ColumnHeaders[colIdx] : string.Empty
                            };

                            if (decimal.TryParse(cell.Value?.ToString(), out var decVal))
                                cubeCell.Value = decVal;

                            result.Cells.Add(cubeCell);
                        }
                        rowIdx++;
                    }
                }

                stopwatch.Stop();
                result.ExecutionTime = stopwatch.Elapsed;
                _logger.LogInformation("MDX query completed in {Ms}ms, returned {Rows} rows.",
                    stopwatch.ElapsedMilliseconds, result.TotalRows);
            }
            catch (AdomdConnectionException ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "SSAS connection failed.");
                throw new InvalidOperationException(
                    $"Cannot connect to SSAS. Verify Data Source and Initial Catalog. Error: {ex.Message}", ex);
            }
            catch (AdomdErrorResponseException ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "MDX query returned an error response.");
                throw new InvalidOperationException(
                    $"MDX query error: {ex.Message}\nQuery: {mdxQuery}", ex);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Unexpected error executing MDX query.");
                throw;
            }
        }, cancellationToken);

        return result;
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<DomainDimension>> GetDimensionsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching SSAS dimensions.");
        var dimensions = new List<DomainDimension>();

        await Task.Run(async () =>
        {
            using var connection = await CreateConnectionAsync();
            connection.Open();

            var cubeName = await GetActiveCubeNameAsync(cancellationToken);
            if (string.IsNullOrEmpty(cubeName))
            {
                _logger.LogWarning("No active cube found to fetch dimensions.");
                return;
            }

            var dimensionTable = connection.GetSchemaDataSet(
                "MDSCHEMA_DIMENSIONS",
                new AdomdRestrictionCollection
                {
                    new AdomdRestriction("CUBE_NAME", cubeName.Trim('[', ']'))
                });

            foreach (System.Data.DataRow row in dimensionTable.Tables[0].Rows)
            {
                var dimName = row["DIMENSION_NAME"]?.ToString() ?? string.Empty;
                if (dimName == "Measures") continue;

                var dim = new DomainDimension
                {
                    Name = dimName,
                    UniqueName = row["DIMENSION_UNIQUE_NAME"]?.ToString() ?? string.Empty,
                    Caption = row["DIMENSION_CAPTION"]?.ToString() ?? dimName,
                    Description = row["DESCRIPTION"]?.ToString() ?? string.Empty
                };

                var hierarchyTable = connection.GetSchemaDataSet(
                    "MDSCHEMA_HIERARCHIES",
                    new AdomdRestrictionCollection
                    {
                        new AdomdRestriction("CUBE_NAME", cubeName.Trim('[', ']')),
                        new AdomdRestriction("DIMENSION_UNIQUE_NAME", dim.UniqueName)
                    });

                foreach (System.Data.DataRow hierRow in hierarchyTable.Tables[0].Rows)
                {
                    var hier = new DomainDimensionHierarchy
                    {
                        Name = hierRow["HIERARCHY_NAME"]?.ToString() ?? string.Empty,
                        UniqueName = hierRow["HIERARCHY_UNIQUE_NAME"]?.ToString() ?? string.Empty,
                        Caption = hierRow["HIERARCHY_CAPTION"]?.ToString() ?? string.Empty
                    };

                    var levelTable = connection.GetSchemaDataSet(
                        "MDSCHEMA_LEVELS",
                        new AdomdRestrictionCollection
                        {
                            new AdomdRestriction("CUBE_NAME", cubeName.Trim('[', ']')),
                            new AdomdRestriction("HIERARCHY_UNIQUE_NAME", hier.UniqueName)
                        });

                    foreach (System.Data.DataRow levelRow in levelTable.Tables[0].Rows)
                    {
                        var levelNum = Convert.ToInt32(levelRow["LEVEL_NUMBER"] ?? 0);
                        if (levelNum == 0) continue;

                        hier.Levels.Add(new DomainDimensionLevel
                        {
                            Name = levelRow["LEVEL_NAME"]?.ToString() ?? string.Empty,
                            UniqueName = levelRow["LEVEL_UNIQUE_NAME"]?.ToString() ?? string.Empty,
                            Caption = levelRow["LEVEL_CAPTION"]?.ToString() ?? string.Empty,
                            LevelNumber = levelNum
                        });
                    }

                    dim.Hierarchies.Add(hier);
                }

                dimensions.Add(dim);
            }
        }, cancellationToken);

        _logger.LogInformation("Found {Count} dimensions.", dimensions.Count);
        return dimensions;
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<DomainMeasure>> GetMeasuresAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching SSAS measures.");
        var measures = new List<DomainMeasure>();

        await Task.Run(async () =>
        {
            using var connection = await CreateConnectionAsync();
            connection.Open();

            var cubeName = await GetActiveCubeNameAsync(cancellationToken);
            if (string.IsNullOrEmpty(cubeName))
            {
                _logger.LogWarning("No active cube found to fetch measures.");
                return;
            }

            var measuresTable = connection.GetSchemaDataSet(
                "MDSCHEMA_MEASURES",
                new AdomdRestrictionCollection
                {
                    new AdomdRestriction("CUBE_NAME", cubeName.Trim('[', ']'))
                });

            foreach (System.Data.DataRow row in measuresTable.Tables[0].Rows)
            {
                measures.Add(new DomainMeasure
                {
                    Name = row["MEASURE_NAME"]?.ToString() ?? string.Empty,
                    UniqueName = row["MEASURE_UNIQUE_NAME"]?.ToString() ?? string.Empty,
                    Caption = row["MEASURE_CAPTION"]?.ToString() ?? string.Empty,
                    DataType = row["DATA_TYPE"]?.ToString() ?? "Numeric",
                    AggregateFunction = row["MEASURE_AGGREGATOR"]?.ToString() ?? "Sum",
                    FormatString = row["DEFAULT_FORMAT_STRING"]?.ToString() ?? "#,##0.00"
                });
            }
        }, cancellationToken);

        _logger.LogInformation("Found {Count} measures.", measures.Count);
        return measures;
    }

    /// <inheritdoc/>
    public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await Task.Run(async () =>
            {
                using var conn = await CreateConnectionAsync();
                conn.Open();
                conn.Close();
            }, cancellationToken);

            _logger.LogInformation("SSAS connection test passed.");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SSAS connection test failed.");
            return false;
        }
    }
    public async Task<string?> GetActiveCubeNameAsync(CancellationToken cancellationToken = default)
    {
        return await Task.Run(async () =>
        {
            using var connection = await CreateConnectionAsync();
            connection.Open();
            var table = connection.GetSchemaDataSet("MDSCHEMA_CUBES", null);
            foreach (System.Data.DataRow row in table.Tables[0].Rows)
            {
                var cubeName = row["CUBE_NAME"]?.ToString();
                if (!string.IsNullOrEmpty(cubeName) && !cubeName.StartsWith("$")) // Ignore system cubes
                {
                    return $"[{cubeName}]";
                }
            }
            return null;
        }, cancellationToken);
    }
}
