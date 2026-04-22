using System.ComponentModel.DataAnnotations;

namespace OlapAnalytics.Application.DTOs.Auth;

public class RegisterRequest
{
    [Required]
    public string Email    { get; set; } = string.Empty;

    [Required]
    public string Username { get; set; } = string.Empty;

    [Required]
    [MinLength(6)]
    public string Password { get; set; } = string.Empty;

    public string SqlConnectionString { get; set; } = string.Empty;

    public string SsasConnectionString { get; set; } = string.Empty;
}

public class SettingsRequest
{
    /// <summary>If null, this field is NOT updated in DB.</summary>
    public string? SqlConnectionString { get; set; }

    /// <summary>If null, this field is NOT updated in DB.</summary>
    public string? SsasConnectionString { get; set; }

    /// <summary>If null, this field is NOT updated in DB.</summary>
    public string? GeminiApiKey { get; set; }
}

public class LoginRequest
{
    [Required]
    public string Email    { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}

public class AuthResponse
{
    public string Token      { get; set; } = string.Empty;
    public string Email      { get; set; } = string.Empty;
    public string Username   { get; set; } = string.Empty;
    public string Role       { get; set; } = string.Empty;
    public int    UserId     { get; set; }
    public DateTime ExpiresAt { get; set; }
    /// <summary>True if user has not configured SQL/SSAS connections yet.</summary>
    public bool NeedsSetup  { get; set; }
}

/// <summary>Returned by GET /api/auth/settings — current saved connection values.</summary>
public class UserSettingsDto
{
    public string SqlConnectionString  { get; set; } = string.Empty;
    public string SsasConnectionString { get; set; } = string.Empty;
    /// <summary>Masked: only first 8 chars shown for security.</summary>
    public string GeminiApiKeyMasked   { get; set; } = string.Empty;
    /// <summary>True if a Gemini key has been saved.</summary>
    public bool   HasGeminiKey         { get; set; }
}

