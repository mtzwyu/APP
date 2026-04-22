using System;

namespace HashGen;

class Program
{
    static void Main()
    {
        string pass = "adimin123";
        string hash = BCrypt.Net.BCrypt.HashPassword(pass);
        Console.WriteLine(hash);
    }
}
