using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OlapAnalytics.Application.DTOs.Upload;
using OlapAnalytics.Application.Interfaces;
using OlapAnalytics.Domain.Entities;
using OlapAnalytics.Domain.Interfaces;

namespace OlapAnalytics.Application.Services;

public class GeminiService : IGeminiService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<GeminiService> _logger;
    private readonly IDatasetRepository _datasetRepository;
    private readonly IAnalysisResultRepository _analysisResultRepository;
    private readonly ISqlProvisioningService _sqlProvisioningService;
    private readonly ISsasProvisioningService _ssasProvisioningService;
    private readonly ITenantConnectionProvider _connectionProvider;

    public GeminiService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<GeminiService> logger,
        IDatasetRepository datasetRepository,
        IAnalysisResultRepository analysisResultRepository,
        ISqlProvisioningService sqlProvisioningService,
        ISsasProvisioningService ssasProvisioningService,
        ITenantConnectionProvider connectionProvider)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
        _datasetRepository = datasetRepository;
        _analysisResultRepository = analysisResultRepository;
        _sqlProvisioningService = sqlProvisioningService;
        _ssasProvisioningService = ssasProvisioningService;
        _connectionProvider = connectionProvider;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Main entry point — called by UploadController.Process
    // ─────────────────────────────────────────────────────────────────────────
    public async Task<ProcessResponse> AnalyzeDatasetAsync(
        int datasetId,
        List<Dictionary<string, object?>> sampleData)
    {
        var dataset = await _datasetRepository.GetByIdAsync(datasetId)
            ?? throw new ArgumentException("Dataset not found.");

        await _datasetRepository.UpdateStatusAsync(datasetId, "processing");

        try
        {
            _logger.LogInformation("[Step 1/5] Extracting sample data for dataset {Id}...", datasetId);
            var sample = GetSampleFromDataset(dataset);
            if (!sample.Any())
                throw new InvalidOperationException("No data found in dataset. Please re-upload the file.");

            _logger.LogInformation("[Step 2/5] Calling Gemini AI for Star Schema design...");
            var aiOutput = await CallGeminiAsync(sample);

            _logger.LogInformation("[Step 3/5] Creating Data Warehouse database '{Db}'...", dataset.DbName);
            await _sqlProvisioningService.CreateDatabaseAsync(dataset.DbName);

            if (!string.IsNullOrWhiteSpace(aiOutput.SqlScript))
            {
                _logger.LogInformation("Executing T-SQL script ({N} batches)...", SplitSqlBatches(aiOutput.SqlScript).Count);
                var batches = SplitSqlBatches(aiOutput.SqlScript);
                foreach (var batch in batches)
                    await _sqlProvisioningService.ExecuteSqlScriptAsync(dataset.DbName, batch);
            }

            _logger.LogInformation("[Step 4/5] Loading data into DW tables...");
            var allRows = GetAllRowsFromDataset(dataset);
            if (allRows.Any() && aiOutput.ColumnMappings?.Any() == true && aiOutput.Schema != null)
            {
                await _sqlProvisioningService.InsertDataAsync(
                    dataset.DbName, allRows, aiOutput.Schema, aiOutput.ColumnMappings);
            }

            _logger.LogInformation("[Step 5/5] Deploying and Processing SSAS Cube...");
            if (aiOutput.Schema != null)
            {
                var sqlConnStr = await _connectionProvider.GetSqlConnectionStringAsync();
                var dwConnStr = ReplaceCatalog(sqlConnStr, dataset.DbName);
                var cubeName  = $"Cube_{dataset.DbName}";

                await _ssasProvisioningService.DeployAndProcessCubeAsync(
                    cubeName, dwConnStr, aiOutput.Schema);
            }

            var analysisResult = new AnalysisResult
            {
                DatasetId    = datasetId,
                SchemaJson   = JsonSerializer.Serialize(aiOutput.Schema),
                InsightsJson = JsonSerializer.Serialize(aiOutput.Insights ?? new()),
                SqlScript    = aiOutput.SqlScript
            };
            await _analysisResultRepository.CreateAsync(analysisResult);
            await _datasetRepository.UpdateStatusAsync(datasetId, "ready");

            _logger.LogInformation("Dataset {Id} processed successfully.", datasetId);

            return new ProcessResponse
            {
                DatasetId = datasetId,
                DbName    = dataset.DbName,
                Status    = "ready",
                Insights  = aiOutput.Insights ?? new(),
                Schema    = aiOutput.Schema
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing dataset {Id}: {Msg}", datasetId, ex.Message);
            await _datasetRepository.UpdateStatusAsync(datasetId, "error", ex.Message);
            throw;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Gemini API call
    // ─────────────────────────────────────────────────────────────────────────
    private async Task<AiParsedOutput> CallGeminiAsync(List<Dictionary<string, object?>> sample)
    {
        var apiKey  = await _connectionProvider.GetGeminiApiKeyAsync();
        
        _logger.LogInformation("Gemini API Key being used: {Key}", apiKey.Substring(0, Math.Min(apiKey.Length, 5)) + "...");

        if (string.IsNullOrWhiteSpace(apiKey) || apiKey == "YOUR_GEMINI_API_KEY")
            throw new InvalidOperationException("Gemini API Key chưa được cấu hình. Vui lòng vào Settings để nhập API Key.");
        
        var modelId = _configuration["Gemini:ModelId"] ?? "gemini-2.0-flash";
        var url     = $"https://generativelanguage.googleapis.com/v1beta/models/{modelId}:generateContent?key={apiKey}";


        var prompt = $@"You are an expert Data Architect and ETL Engineer.
Given this Excel dataset sample (JSON), design a Star Schema Data Warehouse and provide an ETL column mapping.

RULES:
- Design proper Dim tables and 1 Fact table (star schema)
- Every Dim table MUST have a surrogate key column ending in 'Key' (e.g. DateKey, ProductKey)
- The Fact table MUST have foreign key columns referencing each Dim table's key
- Numeric/measurable columns belong in the Fact table
- Date columns become a Dim_Date or Dim_Time table
- Text/categorical columns become dimension tables

OUTPUT FORMAT — respond with ONLY this JSON, no markdown:
{{
  ""schema"": {{
    ""factTable"": {{ ""name"": ""string"", ""columns"": [""string""] }},
    ""dimensionTables"": [ {{ ""name"": ""string"", ""columns"": [""string""] }} ],
    ""relationships"": [ {{ ""fromTable"": ""FactTable"", ""fromColumn"": ""DimKey"", ""toTable"": ""DimTable"", ""toColumn"": ""DimKey"" }} ]
  }},
  ""sqlScript"": ""Full T-SQL CREATE TABLE script (all tables, with PK/FK/IDENTITY). Use IF NOT EXISTS pattern."",
  ""columnMappings"": [
    {{ ""sourceColumn"": ""ExcelColName"", ""targetTable"": ""DimOrFactTable"", ""targetColumn"": ""DbColName"", ""role"": ""dimension|measure|date|key|skip"" }}
  ],
  ""insights"": [ {{ ""title"": ""string"", ""description"": ""string"", ""explanation"": ""string"", ""metric"": ""string"", ""value"": 0.0 }} ]
}}

SAMPLE DATA ({sample.Count} rows):
{JsonSerializer.Serialize(sample)}";

        var body = new
        {
            contents = new[] { new { parts = new[] { new { text = prompt } } } },
            generationConfig = new { responseMimeType = "application/json" }
        };

        var response = await RetryAsync(() => _httpClient.PostAsJsonAsync(url, body), 3);
        
        var raw = await response.Content.ReadAsStringAsync();
        
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Gemini API call failed with status {Code}. Raw response: {Raw}", response.StatusCode, raw);
            throw new InvalidOperationException($"Gemini API call failed: {response.StatusCode} - {raw}");
        }

        return ParseGeminiResponse(raw);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────────────────
    private class AiParsedOutput
    {
        public SchemaDto?         Schema         { get; set; }
        public string?            SqlScript      { get; set; }
        public List<ColumnMappingDto>? ColumnMappings { get; set; }
        public List<InsightDto>?  Insights       { get; set; }
    }

    private AiParsedOutput ParseGeminiResponse(string jsonResponse)
    {
        using var doc = JsonDocument.Parse(jsonResponse);
        var root = doc.RootElement;
        
        if (root.TryGetProperty("error", out var errorElement))
        {
            throw new InvalidOperationException($"Gemini API Error: {errorElement.GetRawText()}");
        }

        if (!root.TryGetProperty("candidates", out var candidates) || candidates.GetArrayLength() == 0)
        {
            throw new InvalidOperationException("Gemini did not return any candidates. Full response: " + jsonResponse);
        }

        var firstCandidate = candidates[0];
        
        if (!firstCandidate.TryGetProperty("content", out var content) || 
            !content.TryGetProperty("parts", out var parts) || 
            parts.GetArrayLength() == 0 ||
            !parts[0].TryGetProperty("text", out var textElement))
        {
            var reason = firstCandidate.TryGetProperty("finishReason", out var fr) ? fr.GetString() : "Unknown";
            throw new InvalidOperationException($"Gemini generation stopped. Reason: {reason}. Full response: {jsonResponse}");
        }

        var text = textElement.GetString();
        if (string.IsNullOrWhiteSpace(text))
            throw new InvalidOperationException("Empty response from Gemini AI.");

        // Fallback: extract JSON from markdown if somehow it still returns markdown
        int startIndex = text.IndexOf('{');
        int endIndex = text.LastIndexOf('}');
        if (startIndex != -1 && endIndex != -1 && endIndex > startIndex)
        {
            text = text.Substring(startIndex, endIndex - startIndex + 1);
        }

        try
        {
            return JsonSerializer.Deserialize<AiParsedOutput>(text,
                new JsonSerializerOptions 
                { 
                    PropertyNameCaseInsensitive = true,
                    AllowTrailingCommas = true,
                    ReadCommentHandling = JsonCommentHandling.Skip
                })
                ?? new AiParsedOutput();
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Failed to parse JSON from Gemini. Exception: {ex.Message}. Text: {text}");
        }
    }

    private List<Dictionary<string, object?>> GetSampleFromDataset(Dataset dataset)
    {
        if (!string.IsNullOrWhiteSpace(dataset.SampleJson))
        {
            try
            {
                return JsonSerializer.Deserialize<List<Dictionary<string, object?>>>(dataset.SampleJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
            }
            catch { /* fall through */ }
        }
        return new();
    }

    private List<Dictionary<string, object?>> GetAllRowsFromDataset(Dataset dataset)
    {
        // For full data, re-use SampleJson (up to 200 rows — sufficient for demo)
        // In production: read RawFileBytes with FileService
        if (!string.IsNullOrWhiteSpace(dataset.SampleJson))
        {
            try
            {
                return JsonSerializer.Deserialize<List<Dictionary<string, object?>>>(dataset.SampleJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
            }
            catch { /* fall through */ }
        }
        return new();
    }

    private static string ReplaceCatalog(string connStr, string newCatalog)
    {
        var b = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(connStr)
        {
            InitialCatalog = newCatalog
        };
        return b.ConnectionString;
    }

    private static List<string> SplitSqlBatches(string sql)
    {
        return sql.Split(new[] { "\nGO\n", "\ngo\n", "\nGo\n" }, StringSplitOptions.RemoveEmptyEntries)
                  .Select(b => b.Trim())
                  .Where(b => !string.IsNullOrWhiteSpace(b))
                  .ToList();
    }

    private async Task<HttpResponseMessage> RetryAsync(Func<Task<HttpResponseMessage>> action, int maxRetries)
    {
        for (int i = 0; i < maxRetries; i++)
        {
            try
            {
                var r = await action();
                if (r.IsSuccessStatusCode) return r;
            }
            catch (Exception ex) when (i < maxRetries - 1)
            {
                _logger.LogWarning(ex, "Gemini retry {N} failed.", i + 1);
            }
            await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, i)));
        }
        return await action();
    }
}
