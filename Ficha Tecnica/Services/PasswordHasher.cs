using System.Security.Cryptography;

namespace Ficha_Tecnica.Services;

public class PasswordHasher
{
    private const int SaltSize = 16;
    private const int KeySize = 32;
    private const int Iterations = 100_000;

    public (string Hash, byte[] Salt) HashPassword(string password)
    {
        ArgumentException.ThrowIfNullOrEmpty(password);

        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256);
        var hash = pbkdf2.GetBytes(KeySize);
        return (Convert.ToBase64String(hash), salt);
    }

    public bool VerifyPassword(string password, string storedHash, byte[] salt)
    {
        if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(storedHash) || salt.Length == 0)
        {
            return false;
        }

        try
        {
            using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256);
            var hashToCompare = pbkdf2.GetBytes(KeySize);
            var existingHash = Convert.FromBase64String(storedHash);
            return CryptographicOperations.FixedTimeEquals(existingHash, hashToCompare);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
