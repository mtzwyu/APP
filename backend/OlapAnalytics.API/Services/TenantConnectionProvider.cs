using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using OlapAnalytics.Application.Interfaces;
using OlapAnalytics.Domain.Interfaces;
using System.Security.Claims;

namespace OlapAnalytics.API.Services;

public class TenantConnectionProvider : ITenantConnectionProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IUserRepository _userRepository;
    private readonly IConfiguration _configuration;

    private readonly IEncryptionService _encryptionService;

    private string? _cachedSql;
    private string? _cachedSsas;
    private string? _cachedGemini;

    public TenantConnectionProvider(
        IHttpContextAccessor httpContextAccessor,
        IUserRepository userRepository,
        IConfiguration configuration,
        IEncryptionService encryptionService)
    {
        _httpContextAccessor = httpContextAccessor;
        _userRepository = userRepository;
        _configuration = configuration;
        _encryptionService = encryptionService;
    }

    private async Task LoadConnectionsAsync()
    {
        if (_cachedSql != null && _cachedSsas != null)
            return;

        var userContext = _httpContextAccessor.HttpContext?.User;
        var userIdClaim = userContext?.FindFirstValue(ClaimTypes.NameIdentifier)
                          ?? userContext?.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub);

        if (int.TryParse(userIdClaim, out var userId))
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user != null)
            {
                _cachedSql    = user.SqlConnectionString;
                _cachedSsas   = user.SsasConnectionString;
                _cachedGemini = user.GeminiApiKey;
            }
        }

        // Fallback to SqlMaster for SQL (no SSAS fallback — user MUST configure)
        if (string.IsNullOrWhiteSpace(_cachedSql))
        {
            var rawSql = _configuration.GetConnectionString("SqlMaster") ?? string.Empty;
            _cachedSql = _encryptionService.Decrypt(rawSql);
        }

        if (string.IsNullOrWhiteSpace(_cachedSsas))
            _cachedSsas = string.Empty; // No hardcoded fallback — user must configure SSAS

        // Fallback to appsettings Gemini:ApiKey if user hasn't saved one yet
        if (string.IsNullOrWhiteSpace(_cachedGemini))
        {
            var rawGemini = _configuration["Gemini:ApiKey"] ?? string.Empty;
            _cachedGemini = _encryptionService.Decrypt(rawGemini);
        }
    }

    public async Task<string> GetSqlConnectionStringAsync()
    {
        await LoadConnectionsAsync();
        return _cachedSql ?? string.Empty;
    }

    public async Task<string> GetSsasConnectionStringAsync()
    {
        await LoadConnectionsAsync();
        return _cachedSsas ?? string.Empty;
    }

    public async Task<string> GetGeminiApiKeyAsync()
    {
        await LoadConnectionsAsync();
        return _cachedGemini ?? string.Empty;
    }
}
