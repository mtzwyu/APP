using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OlapAnalytics.Application.DTOs.Auth;
using OlapAnalytics.Application.Interfaces;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

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

}
