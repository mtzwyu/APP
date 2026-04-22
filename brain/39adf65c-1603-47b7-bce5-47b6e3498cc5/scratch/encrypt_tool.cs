using System;
using System.Security.Cryptography;
using System.Text;
using System.IO;

namespace EncryptionTool;

class Program
{
    static void Main()
    {
        string secret = "OlapAnalyticsDefaultSecretKey2024!";
        string ivStr = "OlapAnalyticsIV!!";
        
        byte[] key = SHA256.HashData(Encoding.UTF8.GetBytes(secret));
        byte[] iv = MD5.HashData(Encoding.UTF8.GetBytes(ivStr));

        string[] targets = {
            "OlapAnalyticsSuperSecretKey2024!!Secure",
            "Server=localhost\\MANHTRUONG1;Database=AppAnalytics;User Id=sa;Password=123;TrustServerCertificate=True;",
            "Server=localhost\\MANHTRUONG1;Database=master;User Id=sa;Password=123;TrustServerCertificate=True;"
        };

        foreach (var t in targets)
        {
            Console.WriteLine($"Target: {t}");
            Console.WriteLine($"Encrypted: {Encrypt(t, key, iv)}");
            Console.WriteLine();
        }
    }

    static string Encrypt(string clearText, byte[] key, byte[] iv)
    {
        using Aes aes = Aes.Create();
        aes.Key = key;
        aes.IV = iv;

        ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

        using MemoryStream ms = new();
        using CryptoStream cs = new(ms, encryptor, CryptoStreamMode.Write);
        using (StreamWriter sw = new(cs))
        {
            sw.Write(clearText);
        }

        return Convert.ToBase64String(ms.ToArray());
    }
}
