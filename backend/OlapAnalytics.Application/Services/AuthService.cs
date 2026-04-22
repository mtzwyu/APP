using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using OlapAnalytics.Application.DTOs.Auth;
using OlapAnalytics.Application.Interfaces;
using OlapAnalytics.Domain.Entities;
using OlapAnalytics.Domain.Interfaces;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.IO;
using System.Collections.Generic;

namespace OlapAnalytics.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IConfiguration _configuration;
    private readonly IEncryptionService _encryptionService;

    public AuthService(IUserRepository userRepository, IConfiguration configuration, IEncryptionService encryptionService)
    {
        _userRepository = userRepository;
        _configuration = configuration;
        _encryptionService = encryptionService;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        var existingEmail = await _userRepository.GetByEmailAsync(request.Email);
        if (existingEmail != null) throw new InvalidOperationException("Email is already registered.");

        var existingUser = await _userRepository.GetByUsernameAsync(request.Username);
        if (existingUser != null) throw new InvalidOperationException("Username is already taken.");

        var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
        var user = new User
        {
            Email = request.Email,
            Username = request.Username,
            PasswordHash = passwordHash,
            Role = "user",
            SqlConnectionString = string.Empty,
            SsasConnectionString = string.Empty
        };
        
        var userId = await _userRepository.CreateAsync(user);
        
        return GenerateAuthResponse(userId, user.Email, user.Username, user.Role, needsSetup: true);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        // Try login by Email first, then by Username
        var user = await _userRepository.GetByEmailAsync(request.Email) 
                ?? await _userRepository.GetByUsernameAsync(request.Email);

        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            throw new UnauthorizedAccessException("Invalid email/username or password.");
        }

        return GenerateAuthResponse(user.Id, user.Email, user.Username, user.Role,
            needsSetup: string.IsNullOrWhiteSpace(user.SqlConnectionString)
                     || string.IsNullOrWhiteSpace(user.SsasConnectionString));
    }

    public async Task UpdateSettingsAsync(int userId, SettingsRequest request)
    {
        await _userRepository.UpdateSettingsAsync(userId, request.SqlConnectionString, request.SsasConnectionString, request.GeminiApiKey);

        // Update appsettings.json if GeminiApiKey is provided (User requested this)
        if (!string.IsNullOrWhiteSpace(request.GeminiApiKey))
        {
            try
            {
                var filePath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
                if (!File.Exists(filePath)) 
                {
                    // In development, it might be in the project root
                    filePath = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json");
                }

                if (File.Exists(filePath))
                {
                    var json = File.ReadAllText(filePath);
                    using var doc = System.Text.Json.JsonDocument.Parse(json);
                    var root = doc.RootElement;

                    // Use a simple dictionary to rebuild the JSON or just a string replace if it's simple enough
                    // But for robustness, let's use a dynamic approach or just replace the specific line
                    // Since it's a specific request, a string replacement on the Gemini:ApiKey line is risky but easy.
                    // Let's do it properly with a Dictionary to avoid corruption.
                    var dict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(json);
                    if (dict != null && dict.TryGetValue("Gemini", out var geminiObj))
                    {
                        var geminiDict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(geminiObj.ToString()!);
                        if (geminiDict != null)
                        {
                            geminiDict["ApiKey"] = _encryptionService.Encrypt(request.GeminiApiKey);
                            dict["Gemini"] = geminiDict;
                            var updatedJson = System.Text.Json.JsonSerializer.Serialize(dict, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                            File.WriteAllText(filePath, updatedJson);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log but don't fail the whole operation if file update fails
                Console.WriteLine($"Warning: Could not update appsettings.json: {ex.Message}");
            }
        }
    }

    public async Task<UserSettingsDto> GetSettingsAsync(int userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null) throw new InvalidOperationException("User not found.");

        var key = user.GeminiApiKey ?? string.Empty;
        return new UserSettingsDto
        {
            SqlConnectionString  = user.SqlConnectionString  ?? string.Empty,
            SsasConnectionString = user.SsasConnectionString ?? string.Empty,
            HasGeminiKey         = !string.IsNullOrWhiteSpace(key),
            GeminiApiKeyMasked   = key.Length > 8 ? key[..8] + "••••••••••••" : (key.Length > 0 ? "••••••••" : string.Empty),
        };
    }


    private AuthResponse GenerateAuthResponse(int userId, string email, string username, string role, bool needsSetup = false)
    {
        // Actually, we should get Jwt config from AppSettings but for simplicity if not exists, we use a fallback or throw.
        var keyRaw = _configuration["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key is missing");
        var key = _encryptionService.Decrypt(keyRaw);
        var issuer = _configuration["Jwt:Issuer"] ?? "OlapAnalytics";
        var audience = _configuration["Jwt:Audience"] ?? "OlapAnalyticsClients";

        var tokenHandler = new JwtSecurityTokenHandler();
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, email),
            new Claim(ClaimTypes.Role, role),
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var expiresAt = DateTime.UtcNow.AddHours(24);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials);

        return new AuthResponse
        {
            Token = tokenHandler.WriteToken(token),
            UserId = userId,
            Email = email,
            Username = username,
            Role = role,
            ExpiresAt = expiresAt,
            NeedsSetup = needsSetup
        };
    }
}
