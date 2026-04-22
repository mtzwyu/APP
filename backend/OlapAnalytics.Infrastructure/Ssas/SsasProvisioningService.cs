using Microsoft.AnalysisServices.AdomdClient;
using Microsoft.Extensions.Logging;
using OlapAnalytics.Application.DTOs.Upload;
using OlapAnalytics.Application.Interfaces;
using System.Text;
using System.Xml.Linq;

namespace OlapAnalytics.Infrastructure.Ssas;

/// <summary>
/// Auto-generates an SSAS cube from a Star Schema DTO and deploys/processes it
/// via XMLA over the ADOMD.NET connection.
/// </summary>
public class SsasProvisioningService : ISsasProvisioningService
{
    private readonly ITenantConnectionProvider _connectionProvider;
    private readonly ILogger<SsasProvisioningService> _logger;

    public SsasProvisioningService(ITenantConnectionProvider connectionProvider, ILogger<SsasProvisioningService> logger)
    {
        _connectionProvider = connectionProvider;
        _logger = logger;
    }

    // ─────────────────────────────────────────────────────────────────────────
    public async Task DeployAndProcessCubeAsync(
        string cubeName,
        string sqlConnectionString,
        SchemaDto schema,
        CancellationToken cancellationToken = default)
    {
        var ssasConnStr = await _connectionProvider.GetSsasConnectionStringAsync();
        if (string.IsNullOrWhiteSpace(ssasConnStr))
            throw new InvalidOperationException("SSAS connection string is not configured for this user.");

        _logger.LogInformation("Starting SSAS cube provisioning for cube '{Cube}'", cubeName);

        // Build safe SSAS catalog name (no spaces/special chars)
        var safeCube = MakeSafeName(cubeName);

        // Generate XMLA Create/Alter script
        var xmla = BuildXmlaScript(safeCube, sqlConnectionString, schema);

        _logger.LogDebug("Generated XMLA:\n{Xmla}", xmla);

        // Execute XMLA via ADOMD
        await Task.Run(() =>
        {
            using var connection = new AdomdConnection(ssasConnStr);
            connection.Open();

            // Step 1: Deploy (Create or Alter)
            _logger.LogInformation("Deploying SSAS cube '{Cube}'...", safeCube);
            using var deployCmd = new AdomdCommand(xmla, connection);
            deployCmd.CommandTimeout = 300;
            deployCmd.ExecuteNonQuery();

            // Step 2: Process Full
            _logger.LogInformation("Processing SSAS cube '{Cube}'...", safeCube);
            var processXmla = BuildProcessXmla(safeCube);
            using var processCmd = new AdomdCommand(processXmla, connection);
            processCmd.CommandTimeout = 600;
            processCmd.ExecuteNonQuery();

        }, cancellationToken);

        _logger.LogInformation("SSAS cube '{Cube}' deployed and processed successfully.", safeCube);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // XMLA Generator
    // ─────────────────────────────────────────────────────────────────────────
    private string BuildXmlaScript(string cubeName, string sqlConnectionString, SchemaDto schema)
    {
        var dsvId = $"{cubeName}_DSV";
        var dsId  = $"{cubeName}_DS";


        var factTable = schema.FactTable;
        var dims      = schema.DimensionTables ?? new();
        var rels      = schema.Relationships    ?? new();

        // ── Measures from fact columns (numeric ones) ──────────────────────
        var measureCols = factTable?.Columns
            .Where(c => !c.EndsWith("Key", StringComparison.OrdinalIgnoreCase)
                     && !c.EndsWith("ID",  StringComparison.OrdinalIgnoreCase))
            .ToList() ?? new();

        // ── Build XMLA ─────────────────────────────────────────────────────
        var sb = new StringBuilder();
        sb.AppendLine("<Create xmlns=\"http://schemas.microsoft.com/analysisservices/2003/engine\">");
        sb.AppendLine("  <ObjectDefinition>");
        sb.AppendLine($"    <Database xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\">");
        sb.AppendLine($"      <ID>{XmlEsc(cubeName)}</ID>");
        sb.AppendLine($"      <Name>{XmlEsc(cubeName)}</Name>");

        // DataSources
        sb.AppendLine("      <DataSources>");
        sb.AppendLine($"        <DataSource xsi:type=\"RelationalDataSource\">");
        sb.AppendLine($"          <ID>{XmlEsc(dsId)}</ID>");
        sb.AppendLine($"          <Name>{XmlEsc(dsId)}</Name>");
        sb.AppendLine($"          <ConnectionString>{XmlEsc(sqlConnectionString)}</ConnectionString>");
        sb.AppendLine($"          <Provider>SQLNCLI11</Provider>");
        sb.AppendLine($"        </DataSource>");
        sb.AppendLine("      </DataSources>");

        // DataSourceViews
        sb.AppendLine("      <DataSourceViews>");
        sb.AppendLine($"        <DataSourceView>");
        sb.AppendLine($"          <ID>{XmlEsc(dsvId)}</ID>");
        sb.AppendLine($"          <Name>{XmlEsc(dsvId)}</Name>");
        sb.AppendLine($"          <DataSourceID>{XmlEsc(dsId)}</DataSourceID>");
        sb.AppendLine("          <Schema>");
        sb.AppendLine("            <xs:schema xmlns:xs=\"http://www.w3.org/2001/XMLSchema\" xmlns:msdata=\"urn:schemas-microsoft-com:xml-msdata\">");

        // Fact table element
        if (factTable != null)
            AppendTableElement(sb, factTable.Name, factTable.Columns);

        // Dim table elements
        foreach (var dim in dims)
            AppendTableElement(sb, dim.Name, dim.Columns);

        sb.AppendLine("            </xs:schema>");
        sb.AppendLine("          </Schema>");
        sb.AppendLine($"        </DataSourceView>");
        sb.AppendLine("      </DataSourceViews>");

        // Dimensions
        sb.AppendLine("      <Dimensions>");
        foreach (var dim in dims)
        {
            var dimId   = MakeSafeName(dim.Name);
            var keyCol  = dim.Columns.FirstOrDefault(c =>
                              c.EndsWith("Key", StringComparison.OrdinalIgnoreCase) ||
                              c.EndsWith("ID",  StringComparison.OrdinalIgnoreCase))
                          ?? dim.Columns.FirstOrDefault() ?? "ID";

            sb.AppendLine($"        <Dimension>");
            sb.AppendLine($"          <ID>{XmlEsc(dimId)}</ID>");
            sb.AppendLine($"          <Name>{XmlEsc(dim.Name)}</Name>");
            sb.AppendLine($"          <DataSourceViewID>{XmlEsc(dsvId)}</DataSourceViewID>");
            sb.AppendLine($"          <DimensionPermissions/>");
            sb.AppendLine($"          <Attributes>");

            // Key attribute
            sb.AppendLine($"            <Attribute>");
            sb.AppendLine($"              <ID>{XmlEsc(keyCol)}</ID>");
            sb.AppendLine($"              <Name>{XmlEsc(keyCol)}</Name>");
            sb.AppendLine($"              <Usage>Key</Usage>");
            sb.AppendLine($"              <KeyColumns>");
            sb.AppendLine($"                <KeyColumn>");
            sb.AppendLine($"                  <DataItem>");
            sb.AppendLine($"                    <ColumnID>{XmlEsc(keyCol)}</ColumnID>");
            sb.AppendLine($"                    <DataType>Integer</DataType>");
            sb.AppendLine($"                  </DataItem>");
            sb.AppendLine($"                </KeyColumn>");
            sb.AppendLine($"              </KeyColumns>");
            sb.AppendLine($"              <NameColumn>");
            sb.AppendLine($"                <DataItem>");
            // Name column = first non-key column
            var nameCol = dim.Columns.FirstOrDefault(c => c != keyCol) ?? keyCol;
            sb.AppendLine($"                  <ColumnID>{XmlEsc(nameCol)}</ColumnID>");
            sb.AppendLine($"                  <DataType>WChar</DataType>");
            sb.AppendLine($"                </DataItem>");
            sb.AppendLine($"              </NameColumn>");
            sb.AppendLine($"            </Attribute>");

            // Other non-key attributes
            foreach (var col in dim.Columns.Where(c => c != keyCol))
            {
                sb.AppendLine($"            <Attribute>");
                sb.AppendLine($"              <ID>{XmlEsc(col)}</ID>");
                sb.AppendLine($"              <Name>{XmlEsc(col)}</Name>");
                sb.AppendLine($"              <Usage>Regular</Usage>");
                sb.AppendLine($"              <KeyColumns><KeyColumn><DataItem><ColumnID>{XmlEsc(col)}</ColumnID><DataType>WChar</DataType></DataItem></KeyColumn></KeyColumns>");
                sb.AppendLine($"            </Attribute>");
            }

            sb.AppendLine($"          </Attributes>");
            sb.AppendLine($"        </Dimension>");
        }
        sb.AppendLine("      </Dimensions>");

        // Cube
        sb.AppendLine("      <Cubes>");
        sb.AppendLine($"        <Cube>");
        sb.AppendLine($"          <ID>{XmlEsc(cubeName)}</ID>");
        sb.AppendLine($"          <Name>{XmlEsc(cubeName)}</Name>");
        sb.AppendLine($"          <DataSourceViewID>{XmlEsc(dsvId)}</DataSourceViewID>");

        // Cube dimensions
        sb.AppendLine($"          <Dimensions>");
        foreach (var dim in dims)
        {
            var dimId = MakeSafeName(dim.Name);
            sb.AppendLine($"            <Dimension>");
            sb.AppendLine($"              <ID>{XmlEsc(dimId)}</ID>");
            sb.AppendLine($"              <Name>{XmlEsc(dim.Name)}</Name>");
            sb.AppendLine($"              <DimensionID>{XmlEsc(dimId)}</DimensionID>");
            sb.AppendLine($"            </Dimension>");
        }
        sb.AppendLine($"          </Dimensions>");

        // MeasureGroups
        if (factTable != null)
        {
            sb.AppendLine($"          <MeasureGroups>");
            sb.AppendLine($"            <MeasureGroup>");
            sb.AppendLine($"              <ID>{XmlEsc(factTable.Name)}</ID>");
            sb.AppendLine($"              <Name>{XmlEsc(factTable.Name)}</Name>");
            sb.AppendLine($"              <Measures>");

            foreach (var col in measureCols)
            {
                sb.AppendLine($"                <Measure>");
                sb.AppendLine($"                  <ID>{XmlEsc(col)}</ID>");
                sb.AppendLine($"                  <Name>{XmlEsc(col)}</Name>");
                sb.AppendLine($"                  <AggregateFunction>Sum</AggregateFunction>");
                sb.AppendLine($"                  <Source><DataItem><ColumnID>{XmlEsc(col)}</ColumnID><DataType>Double</DataType></DataItem></Source>");
                sb.AppendLine($"                </Measure>");
            }

            sb.AppendLine($"              </Measures>");

            // Dimension Usages
            sb.AppendLine($"              <DimensionUsages>");
            foreach (var rel in rels)
            {
                var dimId = MakeSafeName(rel.ToTable);
                sb.AppendLine($"                <DimensionUsage>");
                sb.AppendLine($"                  <Name>{XmlEsc(rel.ToTable)}</Name>");
                sb.AppendLine($"                  <DimensionID>{XmlEsc(dimId)}</DimensionID>");
                sb.AppendLine($"                  <ForeignKeyColumns>");
                sb.AppendLine($"                    <ForeignKeyColumn>");
                sb.AppendLine($"                      <DataItem><ColumnID>{XmlEsc(rel.FromColumn)}</ColumnID></DataItem>");
                sb.AppendLine($"                    </ForeignKeyColumn>");
                sb.AppendLine($"                  </ForeignKeyColumns>");
                sb.AppendLine($"                </DimensionUsage>");
            }
            sb.AppendLine($"              </DimensionUsages>");

            // Partitions
            sb.AppendLine($"              <Partitions>");
            sb.AppendLine($"                <Partition>");
            sb.AppendLine($"                  <ID>Partition_Default</ID>");
            sb.AppendLine($"                  <Name>Partition_Default</Name>");
            sb.AppendLine($"                  <Source xsi:type=\"QueryBinding\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\">");
            sb.AppendLine($"                    <DataSourceID>{XmlEsc(dsId)}</DataSourceID>");
            sb.AppendLine($"                    <QueryDefinition>SELECT * FROM [{XmlEsc(factTable.Name)}]</QueryDefinition>");
            sb.AppendLine($"                  </Source>");
            sb.AppendLine($"                  <StorageMode>Molap</StorageMode>");
            sb.AppendLine($"                </Partition>");
            sb.AppendLine($"              </Partitions>");

            sb.AppendLine($"            </MeasureGroup>");
            sb.AppendLine($"          </MeasureGroups>");
        }

        sb.AppendLine($"        </Cube>");
        sb.AppendLine("      </Cubes>");
        sb.AppendLine("    </Database>");
        sb.AppendLine("  </ObjectDefinition>");
        sb.AppendLine("</Create>");

        return sb.ToString();
    }

    private string BuildProcessXmla(string cubeName)
    {
        return $@"<Process xmlns=""http://schemas.microsoft.com/analysisservices/2003/engine"">
  <Object>
    <DatabaseID>{XmlEsc(cubeName)}</DatabaseID>
  </Object>
  <Type>ProcessFull</Type>
  <WriteBackTableCreation>UseExisting</WriteBackTableCreation>
</Process>";
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────
    private static void AppendTableElement(StringBuilder sb, string tableName, List<string> columns)
    {
        sb.AppendLine($"              <xs:element name=\"{XmlEsc(tableName)}\">");
        sb.AppendLine($"                <xs:complexType>");
        sb.AppendLine($"                  <xs:sequence>");
        foreach (var col in columns)
        {
            sb.AppendLine($"                    <xs:element name=\"{XmlEsc(col)}\" type=\"xs:string\" minOccurs=\"0\"/>");
        }
        sb.AppendLine($"                  </xs:sequence>");
        sb.AppendLine($"                  <xs:attribute name=\"msprop:TableName\" type=\"xs:string\" fixed=\"{XmlEsc(tableName)}\"/>");
        sb.AppendLine($"                </xs:complexType>");
        sb.AppendLine($"              </xs:element>");
    }

    private static string MakeSafeName(string name)
        => System.Text.RegularExpressions.Regex.Replace(name, @"[^a-zA-Z0-9_]", "_");

    private static string XmlEsc(string s)
        => System.Security.SecurityElement.Escape(s) ?? s;
}
