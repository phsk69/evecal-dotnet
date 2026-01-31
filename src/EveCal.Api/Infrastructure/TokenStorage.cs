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

public class TokenStorage : ITokenStorage
{
    private readonly EveConfiguration _config;
    private readonly ILogger<TokenStorage> _logger;
    private readonly string _tokenFilePath;
    private readonly string _keyFilePath;
    private byte[]? _encryptionKey;

    public TokenStorage(IOptions<EveConfiguration> config, ILogger<TokenStorage> logger)
    {
        _config = config.Value;
        _logger = logger;

        Directory.CreateDirectory(_config.DataPath);
        _tokenFilePath = Path.Combine(_config.DataPath, "tokens.enc");
        _keyFilePath = Path.Combine(_config.DataPath, "encryption.key");
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
        _logger.LogInformation("Generated new encryption key at {Path}", _keyFilePath);
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
        _logger.LogInformation("Saved tokens for character {CharacterId}", character.CharacterId);
    }

    public async Task<StoredCharacterData?> LoadAsync()
    {
        if (!File.Exists(_tokenFilePath))
        {
            _logger.LogWarning("No stored tokens found at {Path}", _tokenFilePath);
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
            _logger.LogError(ex, "Failed to load stored tokens");
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
