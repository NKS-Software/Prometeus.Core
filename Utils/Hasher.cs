namespace Prometeus.Core.Utils;

public static class Hasher
{
    public static string Hash(string input) => BCrypt.Net.BCrypt.HashPassword(input);
    public static bool Verify(string input, string stored) => BCrypt.Net.BCrypt.Verify(input, stored);
}
