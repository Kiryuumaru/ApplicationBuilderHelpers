using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using Application.EmbeddedConfig.Utilities;

namespace Application.UnitTests.EmbeddedConfig.Utilities;

public class EmbeddedConfigDecryptorTests
{
    private static readonly byte[] PayloadEncryptionSalt =
    [
        0x4A, 0x97, 0xB2, 0x3E, 0xC1, 0x58, 0xD6, 0x7F,
        0xA3, 0x0E, 0x91, 0x42, 0xF5, 0x8B, 0x64, 0xD0
    ];

    private const int PayloadEncryptionIterations = 10000;

    [Fact]
    public void Decrypt_WithValidEncryptedPayload_ReturnsOriginalJson()
    {
        var originalJson = """{"shared":{"key":"value"},"environments":{"dev":{"secret":"abc123"}}}""";
        var encrypted = Encrypt(originalJson, "testapp", "1.0.0", "dev");

        var result = EmbeddedConfigDecryptor.Decrypt(encrypted, "testapp", "1.0.0", "dev");

        Assert.Equal(originalJson, result);
    }

    [Fact]
    public void Decrypt_WithUnicodeContent_ReturnsOriginalJson()
    {
        var originalJson = """{"shared":{"greeting":"こんにちは世界","emoji":"🌤️"}}""";
        var encrypted = Encrypt(originalJson, "myapp", "2.0.0", "prod");

        var result = EmbeddedConfigDecryptor.Decrypt(encrypted, "myapp", "2.0.0", "prod");

        Assert.Equal(originalJson, result);
    }

    [Fact]
    public void Decrypt_WithDifferentAppName_ThrowsCryptographicException()
    {
        var originalJson = """{"key":"value"}""";
        var encrypted = Encrypt(originalJson, "app1", "1.0.0", "dev");

        Assert.ThrowsAny<CryptographicException>(() =>
            EmbeddedConfigDecryptor.Decrypt(encrypted, "app2", "1.0.0", "dev"));
    }

    [Fact]
    public void Decrypt_WithDifferentVersion_ThrowsCryptographicException()
    {
        var originalJson = """{"key":"value"}""";
        var encrypted = Encrypt(originalJson, "app", "1.0.0", "dev");

        Assert.ThrowsAny<CryptographicException>(() =>
            EmbeddedConfigDecryptor.Decrypt(encrypted, "app", "2.0.0", "dev"));
    }

    [Fact]
    public void Decrypt_WithDifferentAppTag_ThrowsCryptographicException()
    {
        var originalJson = """{"key":"value"}""";
        var encrypted = Encrypt(originalJson, "app", "1.0.0", "dev");

        Assert.ThrowsAny<CryptographicException>(() =>
            EmbeddedConfigDecryptor.Decrypt(encrypted, "app", "1.0.0", "prod"));
    }

    [Fact]
    public void Decrypt_WithEmptyString_ReturnsEmptyString()
    {
        var result = EmbeddedConfigDecryptor.Decrypt(string.Empty, "app", "1.0.0", "dev");

        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void Decrypt_WithNullString_ReturnsEmptyString()
    {
        var result = EmbeddedConfigDecryptor.Decrypt(null!, "app", "1.0.0", "dev");

        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void Decrypt_PayloadTooShort_ThrowsInvalidOperationException()
    {
        var shortPayload = Convert.ToBase64String(new byte[10]);

        Assert.Throws<InvalidOperationException>(() =>
            EmbeddedConfigDecryptor.Decrypt(shortPayload, "app", "1.0.0", "dev"));
    }

    [Fact]
    public void Decrypt_DeterministicEncryption_ProducesSameOutput()
    {
        var json = """{"deterministic":"test"}""";
        var encrypted1 = Encrypt(json, "app", "1.0.0", "dev");
        var encrypted2 = Encrypt(json, "app", "1.0.0", "dev");

        Assert.Equal(encrypted1, encrypted2);
    }

    [Fact]
    public void Decrypt_LargePayload_DecryptsCorrectly()
    {
        var largeObject = new JsonObject();
        for (var i = 0; i < 100; i++)
        {
            largeObject[$"key_{i}"] = $"value_{i}_with_some_longer_content_to_make_it_realistic";
        }
        var largeJson = largeObject.ToJsonString();
        var encrypted = Encrypt(largeJson, "app", "1.0.0", "dev");

        var result = EmbeddedConfigDecryptor.Decrypt(encrypted, "app", "1.0.0", "dev");

        Assert.Equal(largeJson, result);
    }

    [Fact]
    public void Decrypt_EncryptedOutput_IsNotReadableAsPlaintext()
    {
        var json = """{"weather_api_key":"super-secret-key-12345"}""";
        var encrypted = Encrypt(json, "app", "1.0.0", "dev");

        Assert.DoesNotContain("super-secret-key", encrypted);
        Assert.DoesNotContain("weather_api_key", encrypted);
    }

    /// <summary>
    /// Mirrors the source generator encryption logic for test data creation.
    /// Uses the same salt, iterations, and key derivation as BuildConstantsGenerator.
    /// </summary>
    private static string Encrypt(string plaintext, string appName, string version, string appTag)
    {
        var password = Encoding.UTF8.GetBytes(appName + "|" + version + "|" + appTag);
        var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);

        var keyAndIv = Rfc2898DeriveBytes.Pbkdf2(password, PayloadEncryptionSalt, PayloadEncryptionIterations, HashAlgorithmName.SHA1, 48);
        var key = keyAndIv[..32];
        var iv = keyAndIv[32..48];

        using var aes = Aes.Create();
        aes.Key = key;
        aes.IV = iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var encryptor = aes.CreateEncryptor();
        var ciphertext = encryptor.TransformFinalBlock(plaintextBytes, 0, plaintextBytes.Length);

        var result = new byte[iv.Length + ciphertext.Length];
        Buffer.BlockCopy(iv, 0, result, 0, iv.Length);
        Buffer.BlockCopy(ciphertext, 0, result, iv.Length, ciphertext.Length);

        return Convert.ToBase64String(result);
    }
}
