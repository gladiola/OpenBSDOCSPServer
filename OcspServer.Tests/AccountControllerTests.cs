using OcspServer.Controllers;

namespace OcspServer.Tests;

public class AccountControllerTests
{
    // ── HashPassword / VerifyPbkdf2 round-trip ────────────────────────────────

    [Fact]
    public void HashPassword_ProducesVerifiableHash()
    {
        var hash = AccountController.HashPassword("correct-horse-battery-staple");
        // Verify by calling login logic: replicate the check inline
        Assert.True(VerifyPbkdf2("correct-horse-battery-staple", hash));
    }

    [Fact]
    public void HashPassword_WrongPassword_FailsVerification()
    {
        var hash = AccountController.HashPassword("MyS3cr3t");
        Assert.False(VerifyPbkdf2("WrongPassword", hash));
    }

    [Fact]
    public void HashPassword_DifferentSalts_ProduceDifferentHashes()
    {
        var hash1 = AccountController.HashPassword("same");
        var hash2 = AccountController.HashPassword("same");
        Assert.NotEqual(hash1, hash2);          // different random salts
    }

    [Fact]
    public void HashPassword_MalformedHash_ReturnsFalse()
    {
        Assert.False(VerifyPbkdf2("anything", "not-a-valid-hash"));
        Assert.False(VerifyPbkdf2("anything", "a:b"));          // only 2 parts
        Assert.False(VerifyPbkdf2("anything", "abc:def:ghi"));  // bad base64
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private static bool VerifyPbkdf2(string password, string stored)
    {
        try
        {
            var parts = stored.Split(':');
            if (parts.Length != 3) return false;
            int iterations = int.Parse(parts[0]);
            byte[] salt = Convert.FromBase64String(parts[1]);
            byte[] expectedHash = Convert.FromBase64String(parts[2]);
            var pbkdf2 = new System.Security.Cryptography.Rfc2898DeriveBytes(
                password, salt, iterations,
                System.Security.Cryptography.HashAlgorithmName.SHA256);
            byte[] actual = pbkdf2.GetBytes(32);
            return System.Security.Cryptography.CryptographicOperations
                .FixedTimeEquals(actual, expectedHash);
        }
        catch { return false; }
    }
}
