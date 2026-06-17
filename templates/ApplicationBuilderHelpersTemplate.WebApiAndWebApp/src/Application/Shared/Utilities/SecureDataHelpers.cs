using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;

namespace Application.Shared.Utilities;

public static class SecureDataHelpers
{
    private const int SaltSize = 32; // 256 bits
    private const int KeySize = 32; // 256 bits for AES-256
    private const int IvSize = 16; // 128 bits for AES
    private const int Iterations = 100000; // PBKDF2 iterations

    public static byte[] Encrypt(byte[] data, RSA rsa)
    {
        using var aes = Aes.Create();
        aes.GenerateKey();
        aes.GenerateIV();

        using var aesEncryptor = aes.CreateEncryptor();
        byte[] encryptedData = aesEncryptor.TransformFinalBlock(data, 0, data.Length);

        byte[] encryptedKey = rsa.Encrypt(aes.Key, RSAEncryptionPadding.OaepSHA256);

        Span<byte> encrypted = stackalloc byte[12 + encryptedData.Length + encryptedKey.Length + aes.IV.Length];
        Span<byte> encryptedDataHead = encrypted[..4];
        Span<byte> encryptedKeyHead = encrypted.Slice(4, 4);
        Span<byte> aesIVHead = encrypted.Slice(8, 4);
        Span<byte> dataSpan = encrypted.Slice(12, encryptedData.Length);
        Span<byte> keySpan = encrypted.Slice(12 + encryptedData.Length, encryptedKey.Length);
        Span<byte> aesIVSpan = encrypted.Slice(12 + encryptedData.Length + encryptedKey.Length, aes.IV.Length);

        BinaryPrimitives.WriteInt32LittleEndian(encryptedDataHead, encryptedData.Length);
        BinaryPrimitives.WriteInt32LittleEndian(encryptedKeyHead, encryptedKey.Length);
        BinaryPrimitives.WriteInt32LittleEndian(aesIVHead, aes.IV.Length);

        encryptedData.AsSpan().CopyTo(dataSpan);
        encryptedKey.AsSpan().CopyTo(keySpan);
        aes.IV.AsSpan().CopyTo(aesIVSpan);

        return encrypted.ToArray();
    }

    public static byte[] Decrypt(byte[] encryptedBytes, RSA rsa)
    {
        Span<byte> encrypted = encryptedBytes.AsSpan();
        Span<byte> encryptedDataHead = encrypted[..4];
        Span<byte> encryptedKeyHead = encrypted.Slice(4, 4);
        Span<byte> aesIVHead = encrypted.Slice(8, 4);

        int encryptedDataLength = BinaryPrimitives.ReadInt32LittleEndian(encryptedDataHead);
        int encryptedKeyLength = BinaryPrimitives.ReadInt32LittleEndian(encryptedKeyHead);
        int aesIVLength = BinaryPrimitives.ReadInt32LittleEndian(aesIVHead);

        byte[] encryptedData = new byte[encryptedDataLength];
        byte[] encryptedKey = new byte[encryptedKeyLength];
        byte[] aesIV = new byte[aesIVLength];

        encrypted.Slice(12, encryptedDataLength).CopyTo(encryptedData);
        encrypted.Slice(12 + encryptedDataLength, encryptedKeyLength).CopyTo(encryptedKey);
        encrypted.Slice(12 + encryptedDataLength + encryptedKeyLength, aesIVLength).CopyTo(aesIV);

        byte[] aesKey = rsa.Decrypt(encryptedKey, RSAEncryptionPadding.OaepSHA256);

        using var aes = Aes.Create();
        aes.Key = aesKey;
        aes.IV = aesIV;

        using var aesDecryptor = aes.CreateDecryptor();
        byte[] decryptedData = aesDecryptor.TransformFinalBlock(encryptedData, 0, encryptedData.Length);

        return decryptedData;
    }

    public static byte[] Encrypt(byte[] data, byte[] publicKey)
    {
        using var rsa = RSA.Create();
        rsa.ImportRSAPublicKey(publicKey, out _);
        return Encrypt(data, rsa);
    }

    public static byte[] Decrypt(byte[] encryptedBytes, byte[] privateKey)
    {
        using var rsa = RSA.Create();
        rsa.ImportRSAPrivateKey(privateKey, out _);
        return Decrypt(encryptedBytes, rsa);
    }

    public static byte[] EncryptJson(JsonNode obj, RSA rsa)
    {
        return Encrypt(Encoding.Unicode.GetBytes(obj.ToJsonString()), rsa);
    }

    public static JsonNode? DecryptJson(byte[] encryptedBytes, RSA rsa)
    {
        return JsonNode.Parse(Encoding.Unicode.GetString(Decrypt(encryptedBytes, rsa)));
    }

    public static byte[] EncryptWithPassword(byte[] data, string password)
    {
        if (string.IsNullOrEmpty(password))
            throw new ArgumentException("Password cannot be null or empty", nameof(password));

        // Generate random salt and IV
        byte[] salt = new byte[SaltSize];
        byte[] iv = new byte[IvSize];
        RandomNumberGenerator.Fill(salt);
        RandomNumberGenerator.Fill(iv);

        // Derive key from password using PBKDF2
        byte[] key = DeriveKeyFromPassword(password, salt);

        // Encrypt data using AES
        using var aes = Aes.Create();
        aes.Key = key;
        aes.IV = iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var encryptor = aes.CreateEncryptor();
        byte[] encryptedData = encryptor.TransformFinalBlock(data, 0, data.Length);

        // Create final encrypted package: salt + iv + encrypted data
        byte[] result = new byte[SaltSize + IvSize + encryptedData.Length];
        Array.Copy(salt, 0, result, 0, SaltSize);
        Array.Copy(iv, 0, result, SaltSize, IvSize);
        Array.Copy(encryptedData, 0, result, SaltSize + IvSize, encryptedData.Length);

        return result;
    }

    public static byte[] DecryptWithPassword(byte[] encryptedBytes, string password)
    {
        if (string.IsNullOrEmpty(password))
            throw new ArgumentException("Password cannot be null or empty", nameof(password));

        if (encryptedBytes.Length < SaltSize + IvSize)
            throw new CryptographicException("Invalid encrypted data format");

        try
        {
            // Extract salt, IV, and encrypted data
            byte[] salt = new byte[SaltSize];
            byte[] iv = new byte[IvSize];
            byte[] encryptedData = new byte[encryptedBytes.Length - SaltSize - IvSize];

            Array.Copy(encryptedBytes, 0, salt, 0, SaltSize);
            Array.Copy(encryptedBytes, SaltSize, iv, 0, IvSize);
            Array.Copy(encryptedBytes, SaltSize + IvSize, encryptedData, 0, encryptedData.Length);

            // Derive key from password using the same parameters
            byte[] key = DeriveKeyFromPassword(password, salt);

            // Decrypt data using AES
            using var aes = Aes.Create();
            aes.Key = key;
            aes.IV = iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using var decryptor = aes.CreateDecryptor();
            return decryptor.TransformFinalBlock(encryptedData, 0, encryptedData.Length);
        }
        catch (CryptographicException)
        {
            throw new CryptographicException("Decryption failed. The password may be incorrect or the data may be corrupted.");
        }
        catch (Exception ex)
        {
            throw new CryptographicException("Decryption failed. The password may be incorrect or the data may be corrupted.", ex);
        }
    }

    public static string EncryptStringWithPassword(string plainText, string password)
    {
        ArgumentNullException.ThrowIfNull(plainText);

        byte[] data = Encoding.UTF8.GetBytes(plainText);
        byte[] encryptedBytes = EncryptWithPassword(data, password);
        return Convert.ToBase64String(encryptedBytes);
    }

    public static string DecryptStringWithPassword(string encryptedText, string password)
    {
        if (string.IsNullOrEmpty(encryptedText))
            throw new ArgumentException("Encrypted text cannot be null or empty", nameof(encryptedText));

        try
        {
            byte[] encryptedBytes = Convert.FromBase64String(encryptedText);
            byte[] decryptedBytes = DecryptWithPassword(encryptedBytes, password);
            return Encoding.UTF8.GetString(decryptedBytes);
        }
        catch (FormatException ex)
        {
            throw new CryptographicException("Invalid Base64 format in encrypted text", ex);
        }
    }

    private static byte[] DeriveKeyFromPassword(string password, byte[] salt)
    {
        return Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(password), salt, Iterations, HashAlgorithmName.SHA256, KeySize);
    }
}
