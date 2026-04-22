using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using OlapAnalytics.Domain.Entities;
using OlapAnalytics.Domain.Interfaces;

using OlapAnalytics.Application.Interfaces;

namespace OlapAnalytics.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly string _connectionString;
    private readonly IEncryptionService _encryptionService;

    public UserRepository(IConfiguration configuration, IEncryptionService encryptionService)
    {
        var rawConn = configuration.GetConnectionString("AppDb") ?? throw new InvalidOperationException("AppDb connection string not found.");
        _encryptionService = encryptionService;
        _connectionString = _encryptionService.Decrypt(rawConn);
    }

    private IDbConnection CreateConnection() => new SqlConnection(_connectionString);

    public async Task<User?> GetByEmailAsync(string email)
    {
        using var connection = CreateConnection();
        const string sql = "SELECT * FROM Users WHERE Email = @Email";
        var user = await connection.QuerySingleOrDefaultAsync<User>(sql, new { Email = email });
        return DecryptUser(user);
    }

    public async Task<User?> GetByUsernameAsync(string username)
    {
        using var connection = CreateConnection();
        const string sql = "SELECT * FROM Users WHERE Username = @Username";
        var user = await connection.QuerySingleOrDefaultAsync<User>(sql, new { Username = username });
        return DecryptUser(user);
    }

    public async Task<User?> GetByIdAsync(int id)
    {
        using var connection = CreateConnection();
        const string sql = "SELECT * FROM Users WHERE Id = @Id";
        var user = await connection.QuerySingleOrDefaultAsync<User>(sql, new { Id = id });
        return DecryptUser(user);
    }

    public async Task<IEnumerable<User>> GetAllAsync()
    {
        using var connection = CreateConnection();
        const string sql = "SELECT * FROM Users ORDER BY CreatedAt DESC";
        var users = await connection.QueryAsync<User>(sql);
        return users.Select(u => DecryptUser(u)!);
    }

    public async Task<int> CreateAsync(User user)
    {
        // Encrypt sensitive data before saving
        var encryptedUser = new User
        {
            Email = user.Email,
            Username = user.Username,
            PasswordHash = user.PasswordHash,
            Role = user.Role,
            SqlConnectionString = _encryptionService.Encrypt(user.SqlConnectionString),
            SsasConnectionString = _encryptionService.Encrypt(user.SsasConnectionString),
            GeminiApiKey = _encryptionService.Encrypt(user.GeminiApiKey ?? string.Empty),
            CreatedAt = user.CreatedAt
        };

        using var connection = CreateConnection();
        const string sql = @"
            INSERT INTO Users (Email, Username, PasswordHash, Role, SqlConnectionString, SsasConnectionString, GeminiApiKey) 
            OUTPUT INSERTED.Id
            VALUES (@Email, @Username, @PasswordHash, @Role, @SqlConnectionString, @SsasConnectionString, @GeminiApiKey)";
        return await connection.ExecuteScalarAsync<int>(sql, encryptedUser);
    }

    public async Task DeleteAsync(int id)
    {
        using var connection = CreateConnection();
        const string sql = "DELETE FROM Users WHERE Id = @Id";
        await connection.ExecuteAsync(sql, new { Id = id });
    }
    public async Task UpdateSettingsAsync(int id, string? sqlConn, string? ssasConn, string? geminiApiKey)
    {
        var setClauses = new List<string>();
        var parameters = new DynamicParameters();
        parameters.Add("Id", id);

        if (sqlConn != null)
        {
            setClauses.Add("SqlConnectionString = @SqlConn");
            parameters.Add("SqlConn", _encryptionService.Encrypt(sqlConn));
        }
        if (ssasConn != null)
        {
            setClauses.Add("SsasConnectionString = @SsasConn");
            parameters.Add("SsasConn", _encryptionService.Encrypt(ssasConn));
        }
        if (geminiApiKey != null)
        {
            setClauses.Add("GeminiApiKey = @GeminiApiKey");
            parameters.Add("GeminiApiKey", _encryptionService.Encrypt(geminiApiKey));
        }

        if (!setClauses.Any()) return;

        using var connection = CreateConnection();
        var sql = $"UPDATE Users SET {string.Join(", ", setClauses)} WHERE Id = @Id";
        await connection.ExecuteAsync(sql, parameters);
    }

    public async Task UpdatePasswordAsync(int userId, string newHash, string? newEmail = null, string? newUsername = null)
    {
        var parameters = new DynamicParameters();
        parameters.Add("Id", userId);
        parameters.Add("Hash", newHash);
        
        var setClauses = new List<string> { "PasswordHash = @Hash" };
        if (newEmail != null) {
            setClauses.Add("Email = @Email");
            parameters.Add("Email", newEmail);
        }
        if (newUsername != null) {
            setClauses.Add("Username = @Username");
            parameters.Add("Username", newUsername);
        }

        using var connection = CreateConnection();
        var sql = $"UPDATE Users SET {string.Join(", ", setClauses)} WHERE Id = @Id";
        await connection.ExecuteAsync(sql, parameters);
    }

    private User? DecryptUser(User? user)
    {
        if (user == null) return null;

        // Fallback for old users without Username column
        if (string.IsNullOrEmpty(user.Username)) user.Username = user.Email;

        if (!string.IsNullOrEmpty(user.SqlConnectionString))
            user.SqlConnectionString = _encryptionService.Decrypt(user.SqlConnectionString);
        
        if (!string.IsNullOrEmpty(user.SsasConnectionString))
            user.SsasConnectionString = _encryptionService.Decrypt(user.SsasConnectionString);
        
        if (!string.IsNullOrEmpty(user.GeminiApiKey))
            user.GeminiApiKey = _encryptionService.Decrypt(user.GeminiApiKey);

        return user;
    }

}
