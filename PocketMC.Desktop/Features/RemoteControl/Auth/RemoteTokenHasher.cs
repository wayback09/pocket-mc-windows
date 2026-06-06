using System.Security.Cryptography;

namespace PocketMC.Desktop.Features.RemoteControl.Auth;

public sealed class RemoteTokenHasher
{
    private const int TokenByteCount = 32;
    private const int SaltByteCount = 32;
    private const int HashByteCount = 32;
    private const int Iterations = 100_000;

    public string GenerateToken() => ToBase64Url(RandomNumberGenerator.GetBytes(TokenByteCount));

    public RemoteTokenHash Hash(string token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);

        byte[] salt = RandomNumberGenerator.GetBytes(SaltByteCount);
        byte[] hash = DeriveHash(token, salt);
        return new RemoteTokenHash(Convert.ToBase64String(salt), Convert.ToBase64String(hash));
    }

    public bool Verify(string? token, string? salt, string? expectedHash)
    {
        if (string.IsNullOrWhiteSpace(token) ||
            string.IsNullOrWhiteSpace(salt) ||
            string.IsNullOrWhiteSpace(expectedHash))
        {
            return false;
        }

        try
        {
            byte[] saltBytes = Convert.FromBase64String(salt);
            byte[] expectedBytes = Convert.FromBase64String(expectedHash);
            byte[] actualBytes = DeriveHash(token, saltBytes);
            return CryptographicOperations.FixedTimeEquals(actualBytes, expectedBytes);
        }
        catch (FormatException)
        {
            return false;
        }
        catch (CryptographicException)
        {
            return false;
        }
    }

    private static byte[] DeriveHash(string token, byte[] salt) =>
        Rfc2898DeriveBytes.Pbkdf2(
            token,
            salt,
            Iterations,
            HashAlgorithmName.SHA256,
            HashByteCount);

    private static string ToBase64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace("+", "-", StringComparison.Ordinal)
            .Replace("/", "_", StringComparison.Ordinal);
}
