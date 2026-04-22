namespace OlapAnalytics.Domain.Entities;

/// <summary>
/// Application user — stored in AppAnalytics database.
/// </summary>
public class User
{
    public int    Id           { get; set; }
    public string Email        { get; set; } = string.Empty;
    public string Username     { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role         { get; set; } = "user";   // "user" | "admin"
    public string SqlConnectionString { get; set; } = string.Empty;
    public string SsasConnectionString { get; set; } = string.Empty;
    public string? GeminiApiKey { get; set; }
    public DateTime CreatedAt  { get; set; } = DateTime.UtcNow;
}

