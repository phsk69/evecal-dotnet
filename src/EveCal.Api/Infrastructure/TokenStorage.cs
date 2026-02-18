using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using EveCal.Api.Models;
using Microsoft.Extensions.Options;

namespace EveCal.Api.Infrastructure;

public interface ITokenStorage
{
    Task SaveAsync(SsoCharacter character, string refreshToken);
    Task<StoredCharacterData?> LoadAsync();
    bool HasStoredTokens();
    string GetEncryptionKey();
}

public class TokenStorage(IOptions<EveConfiguration> options, ILogger<TokenStorage> logger) : ITokenStorage
{
    private readonly EveConfiguration config = options.Value;
    private readonly string _tokenFilePath = InitPath(options.Value.DataPath, "tokens.enc");
    private readonly string _keyFilePath = InitPath(options.Value.DataPath, "encryption.key");
    private byte[]? _encryptionKey;

    private static string InitPath(string dataPath, string fileName)
    {
        Directory.CreateDirectory(dataPath);
        return Path.Combine(dataPath, fileName);
    }

    public string GetEncryptionKey()
    {
        EnsureEncryptionKey();
        return Convert.ToBase64String(_encryptionKey!);
    }

    private void EnsureEncryptionKey()
    {
        if (_encryptionKey != null) return;

        var envKey = Environment.GetEnvironmentVariable("TOKEN_ENCRYPTION_KEY");
        if (!string.IsNullOrEmpty(envKey))
        {
            _encryptionKey = Convert.FromBase64String(envKey);
            return;
        }

        if (File.Exists(_keyFilePath))
        {
            _encryptionKey = File.ReadAllBytes(_keyFilePath);
            return;
        }

        _encryptionKey = RandomNumberGenerator.GetBytes(32);
        File.WriteAllBytes(_keyFilePath, _encryptionKey);
        logger.LogInformation("new encryption key just dropped at {Path}", _keyFilePath);
    }

    public async Task SaveAsync(SsoCharacter character, string refreshToken)
    {
        EnsureEncryptionKey();

        var encryptedToken = Encrypt(refreshToken);
        var data = new StoredCharacterData
        {
            Character = character,
            EncryptedRefreshToken = encryptedToken,
            LastRefresh = DateTime.UtcNow
        };

        var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(_tokenFilePath, json);
        logger.LogInformation("saved tokens for character {CharacterId}, secured the bag", character.CharacterId);
    }

    public async Task<StoredCharacterData?> LoadAsync()
    {
        if (!File.Exists(_tokenFilePath))
        {
            logger.LogWarning("no tokens at {Path}, kinda sus", _tokenFilePath);
            return null;
        }

        try
        {
            EnsureEncryptionKey();
            var json = await File.ReadAllTextAsync(_tokenFilePath);
            var data = JsonSerializer.Deserialize<StoredCharacterData>(json);

            if (data != null)
            {
                data = data with
                {
                    EncryptedRefreshToken = Decrypt(data.EncryptedRefreshToken)
                };
            }

            return data;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "couldn't load stored tokens, big yikes");
            return null;
        }
    }

    public bool HasStoredTokens() => File.Exists(_tokenFilePath);

    private string Encrypt(string plainText)
    {
        using var aes = Aes.Create();
        aes.Key = _encryptionKey!;
        aes.GenerateIV();

        using var encryptor = aes.CreateEncryptor();
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var encryptedBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

        var result = new byte[aes.IV.Length + encryptedBytes.Length];
        Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
        Buffer.BlockCopy(encryptedBytes, 0, result, aes.IV.Length, encryptedBytes.Length);

        return Convert.ToBase64String(result);
    }

    private string Decrypt(string cipherText)
    {
        var cipherBytes = Convert.FromBase64String(cipherText);

        using var aes = Aes.Create();
        aes.Key = _encryptionKey!;

        var iv = new byte[16];
        var encrypted = new byte[cipherBytes.Length - 16];
        Buffer.BlockCopy(cipherBytes, 0, iv, 0, 16);
        Buffer.BlockCopy(cipherBytes, 16, encrypted, 0, encrypted.Length);
        aes.IV = iv;

        using var decryptor = aes.CreateDecryptor();
        var decryptedBytes = decryptor.TransformFinalBlock(encrypted, 0, encrypted.Length);
        return Encoding.UTF8.GetString(decryptedBytes);
    }
}
