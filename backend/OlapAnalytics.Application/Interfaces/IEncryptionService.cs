namespace OlapAnalytics.Application.Interfaces;

public interface IEncryptionService
{
    string Encrypt(string clearText);
    string Decrypt(string cipherText);
}
