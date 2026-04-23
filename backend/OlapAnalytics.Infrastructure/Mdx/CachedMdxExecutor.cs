using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using OlapAnalytics.Domain.Entities;
using OlapAnalytics.Application.Interfaces;
using OlapAnalytics.Domain.Interfaces;
using System.Security.Cryptography;
using System.Text;

namespace OlapAnalytics.Infrastructure.Mdx;

/// <summary>
/// Decorator for IMdxExecutor that adds MemoryCache layer.
/// Cache key: SHA256 hash of the MDX query string.
/// TTL: 5 minutes by default (configurable).
/// </summary>
public class CachedMdxExecutor : IMdxExecutor
{
    private readonly IMdxExecutor _inner;
    private readonly IMemoryCache _cache;
    private readonly ITenantConnectionProvider _connectionProvider;
    private readonly ILogger<CachedMdxExecutor> _logger;
    private readonly TimeSpan _cacheDuration;
    private string? _connHash;

    public CachedMdxExecutor(
        IMdxExecutor inner,
        IMemoryCache cache,
        ITenantConnectionProvider connectionProvider,
        ILogger<CachedMdxExecutor> logger,
        TimeSpan? cacheDuration = null)
    {
        _inner = inner;
        _cache = cache;
        _connectionProvider = connectionProvider;
        _logger = logger;
        _cacheDuration = cacheDuration ?? TimeSpan.FromMinutes(5);
    }

    private async Task<string> GetConnectionPrefixAsync()
    {
        if (_connHash != null) return _connHash;
        var connStr = await _connectionProvider.GetSsasConnectionStringAsync();
        _connHash = $"ssas:{ComputeHash(connStr)}";
        return _connHash;
    }

    public async Task<CubeResult> ExecuteQueryAsync(string mdxQuery, CancellationToken cancellationToken = default)
    {
        var prefix = await GetConnectionPrefixAsync();
        var cacheKey = $"{prefix}:mdx:{ComputeHash(mdxQuery)}";

        if (_cache.TryGetValue(cacheKey, out CubeResult? cached) && cached != null)
        {
            _logger.LogInformation("MDX query served from cache. Key: {Key}", cacheKey);
            cached.FromCache = true;
            return cached;
        }

        var result = await _inner.ExecuteQueryAsync(mdxQuery, cancellationToken);

        var options = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = _cacheDuration,
            SlidingExpiration = TimeSpan.FromMinutes(2),
            Priority = CacheItemPriority.Normal
        };

        _cache.Set(cacheKey, result, options);
        _logger.LogInformation("MDX result cached. Key: {Key}, TTL: {TTL}", cacheKey, _cacheDuration);

        return result;
    }

    public async Task<IEnumerable<Dimension>> GetDimensionsAsync(CancellationToken cancellationToken = default)
    {
        var prefix = await GetConnectionPrefixAsync();
        var key = $"{prefix}:dimensions:all";
        return (await _cache.GetOrCreateAsync(key, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30);
            return await _inner.GetDimensionsAsync(cancellationToken) ?? Enumerable.Empty<Dimension>();
        }))!;
    }

    public async Task<IEnumerable<Measure>> GetMeasuresAsync(CancellationToken cancellationToken = default)
    {
        var prefix = await GetConnectionPrefixAsync();
        var key = $"{prefix}:measures:all";
        return (await _cache.GetOrCreateAsync(key, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30);
            return await _inner.GetMeasuresAsync(cancellationToken) ?? Enumerable.Empty<Measure>();
        }))!;
    }

    public Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
        => _inner.TestConnectionAsync(cancellationToken);

    public async Task<string?> GetActiveCubeNameAsync(CancellationToken cancellationToken = default)
    {
        var prefix = await GetConnectionPrefixAsync();
        var key = $"{prefix}:cube:active";
        return (await _cache.GetOrCreateAsync(key, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30);
            return await _inner.GetActiveCubeNameAsync(cancellationToken);
        }))!;
    }

    private static string ComputeHash(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes)[..16]; // First 16 chars for brevity
    }
}
