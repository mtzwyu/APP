using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OlapAnalytics.Application.Services;
using OlapAnalytics.Application.Interfaces;
using OlapAnalytics.Domain.Interfaces;
using OlapAnalytics.Infrastructure.Mdx;
using OlapAnalytics.Infrastructure.Sql;

namespace OlapAnalytics.Infrastructure.DependencyInjection;

/// <summary>
/// Extension methods for registering Infrastructure services with DI.
/// </summary>
public static class InfrastructureServiceExtensions
{
    /// <summary>
    /// Registers SSAS/ADOMD.NET executor with MemoryCache decorator.
    /// Optionally registers SqlQueryService if sqlConnectionString is provided.
    /// </summary>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services)
    {
        // Register Encryption Service
        services.AddSingleton<IEncryptionService, OlapAnalytics.Infrastructure.Security.AesEncryptionService>();

        // Register MemoryCache
        services.AddMemoryCache();

        // Register MdxQueryBuilder as scoped (requires IMdxExecutor)
        services.AddScoped<MdxQueryBuilder>();

        // Register the real SSAS executor
        services.AddScoped<SsasMdxExecutor>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<SsasMdxExecutor>>();
            var provider = sp.GetRequiredService<ITenantConnectionProvider>();
            return new SsasMdxExecutor(provider, logger);
        });

        // Register the cached decorator as IMdxExecutor
        services.AddScoped<IMdxExecutor>(sp =>
        {
            var inner = sp.GetRequiredService<SsasMdxExecutor>();
            var cache = sp.GetRequiredService<IMemoryCache>();
            var logger = sp.GetRequiredService<ILogger<CachedMdxExecutor>>();
            return new CachedMdxExecutor(inner, cache, logger);
        });

        // Register SQL Server service
        services.AddScoped<SqlQueryService>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<SqlQueryService>>();
            var provider = sp.GetRequiredService<ITenantConnectionProvider>();
            return new SqlQueryService(provider, logger);
        });

        // Register AppAnalytics Repositories
        services.AddScoped<IUserRepository, OlapAnalytics.Infrastructure.Repositories.UserRepository>();
        services.AddScoped<IDatasetRepository, OlapAnalytics.Infrastructure.Repositories.DatasetRepository>();
        services.AddScoped<IAnalysisResultRepository, OlapAnalytics.Infrastructure.Repositories.AnalysisResultRepository>();

        return services;
    }
}

