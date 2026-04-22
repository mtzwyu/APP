using OlapAnalytics.Domain.Entities;

namespace OlapAnalytics.Domain.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByUsernameAsync(string username);
    Task<User?> GetByIdAsync(int id);
    Task<IEnumerable<User>> GetAllAsync();
    Task<int> CreateAsync(User user);
    Task DeleteAsync(int id);
    Task UpdateSettingsAsync(int id, string? sqlConn, string? ssasConn, string? geminiApiKey);
    Task UpdatePasswordAsync(int userId, string newHash, string? newEmail = null, string? newUsername = null);
}
