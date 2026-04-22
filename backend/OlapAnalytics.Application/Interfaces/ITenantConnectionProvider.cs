namespace OlapAnalytics.Application.Interfaces;

public interface ITenantConnectionProvider
{
    Task<string> GetSqlConnectionStringAsync();
    Task<string> GetSsasConnectionStringAsync();
    Task<string> GetGeminiApiKeyAsync();
}

