using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using OlapAnalytics.Application.Interfaces;

namespace OlapAnalytics.Infrastructure.Security;

public class AesEncryptionService : IEncryptionService
{
    private readonly byte[] _key;
    private readonly byte[] _iv;

    public AesEncryptionService(IConfiguration configuration)
    {
        // Get secret from config, or use a default (not recommended for production)
        var secret = configuration["Encryption:Key"] ?? "OlapAnalyticsDefaultSecretKey2024!";
        var iv = configuration["Encryption:IV"] ?? "OlapAnalyticsIV!!";

        _key = SHA256.HashData(Encoding.UTF8.GetBytes(secret));
        _iv = MD5.HashData(Encoding.UTF8.GetBytes(iv));
    }

    public string Encrypt(string clearText)
    {
        if (string.IsNullOrEmpty(clearText)) return clearText;

        using Aes aes = Aes.Create();
        aes.Key = _key;
        aes.IV = _iv;

        ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

        using MemoryStream ms = new();
        using CryptoStream cs = new(ms, encryptor, CryptoStreamMode.Write);
        using (StreamWriter sw = new(cs))
        {
            sw.Write(clearText);
        }

        return Convert.ToBase64String(ms.ToArray());
    }

    public string Decrypt(string cipherText)
    {
        if (string.IsNullOrEmpty(cipherText)) return cipherText;

        try
        {
            using Aes aes = Aes.Create();
            aes.Key = _key;
            aes.IV = _iv;

            ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

            using MemoryStream ms = new(Convert.FromBase64String(cipherText));
            using CryptoStream cs = new(ms, decryptor, CryptoStreamMode.Read);
            using StreamReader sr = new(cs);
            
            return sr.ReadToEnd();
        }
        catch
        {
            // If decryption fails (maybe it was not encrypted?), return as is or handle appropriately
            return cipherText;
        }
    }
}
