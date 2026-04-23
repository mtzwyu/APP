using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using OlapAnalytics.Application.DTOs.Auth;
using OlapAnalytics.Application.Interfaces;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Dapper;

namespace OlapAnalytics.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        try
        {
            var response = await _authService.RegisterAsync(request);
            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering user {Email}", request.Email);
            return StatusCode(500, new { message = "An error occurred during registration." });
        }
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        try
        {
            var response = await _authService.LoginAsync(request);
            _logger.LogInformation("User {Email} logged in with role {Role}", response.Email, response.Role);
            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging in user {Email}", request.Email);
            return StatusCode(500, new { message = "An error occurred during login." });
        }
    }

    [HttpGet("settings")]
    [Authorize]
    public async Task<IActionResult> GetSettings()
    {
        var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (!int.TryParse(idStr, out var userId)) return Unauthorized();
        try
        {
            var dto = await _authService.GetSettingsAsync(userId);
            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting settings for user {UserId}", userId);
            return StatusCode(500, new { message = ex.Message });
        }
    }

    [HttpPut("settings")]
    [Authorize]
    public async Task<IActionResult> UpdateSettings([FromBody] SettingsRequest request)
    {

        if (!ModelState.IsValid) return BadRequest(ModelState);

        _logger.LogInformation("UpdateSettings received: Sql={Sql}, Ssas={Ssas}, Gemini={Gemini}", 
            request.SqlConnectionString, request.SsasConnectionString, request.GeminiApiKey);

        var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (!int.TryParse(idStr, out var userId)) return Unauthorized();

        try
        {
            if (request.GeminiApiKey != null) request.GeminiApiKey = request.GeminiApiKey.Trim();
            if (request.SqlConnectionString != null) request.SqlConnectionString = request.SqlConnectionString.Trim();
            if (request.SsasConnectionString != null) request.SsasConnectionString = request.SsasConnectionString.Trim();

            await _authService.UpdateSettingsAsync(userId, request);
            return Ok(new { message = "Settings updated successfully." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating settings for user {UserId}", userId);
            return StatusCode(500, new { message = $"An error occurred updating settings: {ex.Message} {ex.InnerException?.Message}" });
        }
    }

    // ── Discover SQL Server databases ─────────────────────────────────────────
    /// <summary>
    /// POST /api/auth/sql-databases
    /// Accepts a temporary SQL Server connection string, connects to master,
    /// and returns the list of available user databases.
    /// </summary>
    [HttpPost("sql-databases")]
    [Authorize]
    public async Task<IActionResult> GetSqlDatabases([FromBody] DiscoverRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ConnectionString))
            return BadRequest(new { message = "Connection string is required." });
        try
        {
            var builder = new SqlConnectionStringBuilder(request.ConnectionString)
            {
                InitialCatalog = "master",
                ConnectTimeout = 8
            };
            using var conn = new SqlConnection(builder.ConnectionString);
            await conn.OpenAsync();
            var dbs = (await conn.QueryAsync<string>(
                "SELECT name FROM sys.databases WHERE database_id > 4 AND state_desc = 'ONLINE' ORDER BY name"
            )).ToList();
            return Ok(new { databases = dbs });
        }
        catch (Exception ex)
        {
            _logger.LogWarning("SQL discover failed: {Msg}", ex.Message);
            return BadRequest(new { message = ex.Message });
        }
    }

    // ── Discover SSAS catalogs ────────────────────────────────────────────────
    [HttpPost("ssas-catalogs")]
    [Authorize]
    public async Task<IActionResult> GetSsasCatalogs([FromBody] DiscoverRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ConnectionString))
            return BadRequest(new { message = "Connection string is required." });
        try
        {
            var catalogs = await Task.Run(() =>
            {
                using var conn = new Microsoft.AnalysisServices.AdomdClient.AdomdConnection(request.ConnectionString);
                conn.Open();
                var schema = conn.GetSchemaDataSet("DBSCHEMA_CATALOGS", null);
                var result = new List<string>();
                foreach (System.Data.DataRow row in schema.Tables[0].Rows)
                    result.Add(row["CATALOG_NAME"].ToString() ?? "");
                conn.Close();
                return result;
            });
            return Ok(new { catalogs });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // ── Discover SSAS cubes ───────────────────────────────────────────────────
    [HttpPost("ssas-cubes")]
    [Authorize]
    public async Task<IActionResult> GetSsasCubes([FromBody] DiscoverRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ConnectionString))
            return BadRequest(new { message = "Connection string is required." });
        try
        {
            var cubes = await Task.Run(() =>
            {
                using var conn = new Microsoft.AnalysisServices.AdomdClient.AdomdConnection(request.ConnectionString);
                conn.Open();
                var schema = conn.GetSchemaDataSet("MDSCHEMA_CUBES", null);
                var result = new List<string>();
                foreach (System.Data.DataRow row in schema.Tables[0].Rows)
                {
                    var name = row["CUBE_NAME"].ToString();
                    if (!string.IsNullOrEmpty(name) && !name.StartsWith("$"))
                        result.Add(name);
                }
                conn.Close();
                return result;
            });
            return Ok(new { cubes });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // ── Discover SQL Tables ───────────────────────────────────────────────────
    [HttpPost("sql-tables")]
    [Authorize]
    public async Task<IActionResult> GetSqlTables([FromBody] DiscoverRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ConnectionString))
            return BadRequest(new { message = "Connection string is required." });
        try
        {
            using var conn = new SqlConnection(request.ConnectionString);
            await conn.OpenAsync();
            var tables = (await conn.QueryAsync<string>(
                "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE' ORDER BY TABLE_NAME"
            )).ToList();
            return Ok(new { tables });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}

/// <summary>Request DTO for metadata discovery endpoints.</summary>
public class DiscoverRequest
{
    public string ConnectionString { get; set; } = string.Empty;
}
