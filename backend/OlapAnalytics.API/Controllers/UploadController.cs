using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OlapAnalytics.Application.DTOs.Upload;
using OlapAnalytics.Application.Interfaces;
using OlapAnalytics.Domain.Interfaces;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

namespace OlapAnalytics.API.Controllers;

[ApiController]
[Route("api")]
[Authorize]
public class UploadController : ControllerBase
{
    private readonly IFileService _fileService;
    private readonly IGeminiService _geminiService;
    private readonly IDatasetRepository _datasetRepository;
    private readonly IAnalysisResultRepository _analysisResultRepository;
    private readonly ILogger<UploadController> _logger;

    public UploadController(
        IFileService fileService,
        IGeminiService geminiService,
        IDatasetRepository datasetRepository,
        IAnalysisResultRepository analysisResultRepository,
        ILogger<UploadController> logger)
    {
        _fileService = fileService;
        _geminiService = geminiService;
        _datasetRepository = datasetRepository;
        _analysisResultRepository = analysisResultRepository;
        _logger = logger;
    }

    private int GetCurrentUserId()
    {
        var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return int.TryParse(idStr, out var id) ? id : 0;
    }

    [HttpPost("upload")]
    [RequestSizeLimit(100_000_000)] // 100 MB max
    public async Task<IActionResult> Upload(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file uploaded.");

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (ext != ".xlsx")
            return BadRequest("Only .xlsx files are supported currently.");

        var userId = GetCurrentUserId();
        if (userId == 0) return Unauthorized();

        try
        {
            using var stream = file.OpenReadStream();
            var response = await _fileService.SaveUploadAsync(userId, file.FileName, stream);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file");
            return StatusCode(500, "Error uploading file: " + ex.Message);
        }
    }

    [HttpPost("process")]
    public async Task<IActionResult> Process([FromBody] ProcessRequest request)
    {
        var dataset = await _datasetRepository.GetByIdAsync(request.DatasetId);
        if (dataset == null) return NotFound("Dataset not found.");

        var userId = GetCurrentUserId();
        if (dataset.UserId != userId && !User.IsInRole("admin")) return Forbid();

        if (dataset.Status == "processing")
            return Conflict(new { message = "Dataset is already being processed." });

        try
        {
            // GeminiService reads sample data from Dataset.SampleJson (stored at upload time).
            // Pass empty list — the service fetches the real data from the DB record.
            var response = await _geminiService.AnalyzeDatasetAsync(request.DatasetId, new());
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing dataset {DatasetId}", request.DatasetId);
            return StatusCode(500, new { 
                success = false,
                message = ex.Message,
                details = ex.ToString() // Include stack trace in dev mode if possible, but ex.Message is priority
            });
        }
    }


    [HttpGet("data")]
    public async Task<IActionResult> GetData()
    {
        var userId = GetCurrentUserId();
        if (userId == 0) return Unauthorized();

        var datasets = await _datasetRepository.GetByUserIdAsync(userId);
        var dtos = datasets.Select(d => new DatasetDto
        {
            Id = d.Id,
            FileName = d.FileName,
            DbName = d.DbName,
            Status = d.Status,
            RowCount = d.RowCount,
            ErrorMsg = d.ErrorMsg,
            CreatedAt = d.CreatedAt
        });

        return Ok(dtos);
    }

    [HttpGet("insight")]
    public async Task<IActionResult> GetInsight([FromQuery] int fileId)
    {
        var dataset = await _datasetRepository.GetByIdAsync(fileId);
        if (dataset == null) return NotFound();

        var userId = GetCurrentUserId();
        if (dataset.UserId != userId && !User.IsInRole("admin")) return Forbid();

        var result = await _analysisResultRepository.GetByDatasetIdAsync(fileId);
        if (result == null || string.IsNullOrEmpty(result.InsightsJson))
            return Ok(new List<InsightDto>());

        try
        {
            var insights = System.Text.Json.JsonSerializer.Deserialize<List<InsightDto>>(result.InsightsJson, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return Ok(insights);
        }
        catch
        {
            return Ok(new List<InsightDto>());
        }
    }
}
