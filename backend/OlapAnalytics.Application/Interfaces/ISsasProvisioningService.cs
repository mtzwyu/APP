using OlapAnalytics.Application.DTOs.Upload;

namespace OlapAnalytics.Application.Interfaces;

/// <summary>
/// Provisions (creates/updates/processes) an SSAS cube based on the DW schema.
/// </summary>
public interface ISsasProvisioningService
{
    /// <summary>
    /// Generates XMLA, deploys the cube to SSAS, and triggers ProcessFull.
    /// </summary>
    Task DeployAndProcessCubeAsync(
        string cubeName,
        string sqlConnectionString,
        SchemaDto schema,
        CancellationToken cancellationToken = default);
}
