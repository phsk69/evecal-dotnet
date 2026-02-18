using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using EveCal.Api.Infrastructure;
using EveCal.Api.Models;
using Microsoft.Extensions.Options;

namespace EveCal.Api.Services;

public interface IEveAuthService
{
    Task<OAuthDiscovery> GetDiscoveryDocumentAsync();
    (string AuthUrl, string CodeVerifier, string State) GenerateAuthorizationUrl();
    Task<EveTokens> ExchangeCodeAsync(string code, string codeVerifier);
    Task<EveTokens> RefreshTokenAsync(string refreshToken);
    SsoCharacter ParseJwtToken(string accessToken);
    Task<EveTokens?> GetValidTokensAsync();
    Task StoreTokensAsync(EveTokens tokens);
}

public class EveAuthService(
    IHttpClientFactory httpClientFactory,
    ITokenStorage tokenStorage,
    IOptions<EveConfiguration> options,
    ILogger<EveAuthService> logger) : IEveAuthService
{
    private readonly EveConfiguration config = options.Value;

    private EveTokens? _currentTokens;
    private SsoCharacter? _currentCharacter;
    private OAuthDiscovery? _discovery;

    public async Task<OAuthDiscovery> GetDiscoveryDocumentAsync()
    {
        if (_discovery != null) return _discovery;

        var client = httpClientFactory.CreateClient("EVEAuth");
        var response = await client.GetAsync(".well-known/oauth-authorization-server");
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        _discovery = JsonSerializer.Deserialize<OAuthDiscovery>(json)
            ?? throw new InvalidOperationException("Failed to parse OAuth discovery document");

        return _discovery;
    }

    public (string AuthUrl, string CodeVerifier, string State) GenerateAuthorizationUrl()
    {
        // cooking up PKCE code verifier (32 bytes = 256 bits) fr
        var codeVerifierBytes = RandomNumberGenerator.GetBytes(32);
        var codeVerifier = Base64UrlEncode(codeVerifierBytes);

        // code challenge generation going crazy (S256)
        var challengeBytes = SHA256.HashData(Encoding.ASCII.GetBytes(codeVerifier));
        var codeChallenge = Base64UrlEncode(challengeBytes);

        // state got that UUID drip
        var state = Base64UrlEncode(Encoding.UTF8.GetBytes(
            JsonSerializer.Serialize(new { uuid = Guid.NewGuid().ToString() })));

        var authUrl = $"https://login.eveonline.com/v2/oauth/authorize?" +
            $"response_type=code" +
            $"&redirect_uri={Uri.EscapeDataString(config.CallbackUrl)}" +
            $"&client_id={config.ClientId}" +
            $"&scope={Uri.EscapeDataString(config.Scopes)}" +
            $"&code_challenge={codeChallenge}" +
            $"&code_challenge_method=S256" +
            $"&state={state}";

        return (authUrl, codeVerifier, state);
    }

    public async Task<EveTokens> ExchangeCodeAsync(string code, string codeVerifier)
    {
        var client = httpClientFactory.CreateClient("EVEAuth");

        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["code"] = code,
            ["client_id"] = config.ClientId,
            ["code_verifier"] = codeVerifier
        });

        var response = await client.PostAsync("/v2/oauth/token", content);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            logger.LogError("💀 token exchange fumbled: {Error}", error);
            throw new InvalidOperationException($"Token exchange failed: {error}");
        }

        var json = await response.Content.ReadAsStringAsync();
        var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(json)
            ?? throw new InvalidOperationException("Failed to parse token response");

        return new EveTokens
        {
            AccessToken = tokenResponse.access_token,
            RefreshToken = tokenResponse.refresh_token,
            ExpiresAt = DateTime.UtcNow.AddSeconds(tokenResponse.expires_in),
            TokenType = tokenResponse.token_type
        };
    }

    public async Task<EveTokens> RefreshTokenAsync(string refreshToken)
    {
        var client = httpClientFactory.CreateClient("EVEAuth");

        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["refresh_token"] = refreshToken,
            ["client_id"] = config.ClientId
        });

        var response = await client.PostAsync("/v2/oauth/token", content);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            logger.LogError("💀 token refresh took an L: {Error}", error);
            throw new InvalidOperationException($"Token refresh failed: {error}");
        }

        var json = await response.Content.ReadAsStringAsync();
        var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(json)
            ?? throw new InvalidOperationException("Failed to parse token response");

        return new EveTokens
        {
            AccessToken = tokenResponse.access_token,
            RefreshToken = tokenResponse.refresh_token,
            ExpiresAt = DateTime.UtcNow.AddSeconds(tokenResponse.expires_in),
            TokenType = tokenResponse.token_type
        };
    }

    public SsoCharacter? GetCurrentCharacter() => _currentCharacter;

    public SsoCharacter ParseJwtToken(string accessToken)
    {
        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(accessToken);

        var subClaim = token.Claims.FirstOrDefault(c => c.Type == "sub")?.Value
            ?? throw new InvalidOperationException("Missing sub claim");

        // subject be like: "CHARACTER:EVE:123456"
        var parts = subClaim.Split(':');
        if (parts.Length < 3 || !int.TryParse(parts[2], out var characterId))
        {
            throw new InvalidOperationException($"Invalid subject format: {subClaim}");
        }

        var name = token.Claims.FirstOrDefault(c => c.Type == "name")?.Value ?? "Unknown";
        var scopes = token.Claims.FirstOrDefault(c => c.Type == "scp")?.Value ?? "";

        return new SsoCharacter
        {
            CharacterId = characterId,
            CharacterName = name,
            ExpiresOn = token.ValidTo,
            Scopes = scopes.Split(' ', StringSplitOptions.RemoveEmptyEntries)
        };
    }

    public async Task<EveTokens?> GetValidTokensAsync()
    {
        // giving you the cached tokens if they still slap
        if (_currentTokens != null && !_currentTokens.NeedsRefresh)
        {
            return _currentTokens;
        }

        // finna refresh these tokens if we got em
        if (_currentTokens != null)
        {
            try
            {
                _currentTokens = await RefreshTokenAsync(_currentTokens.RefreshToken);
                await StoreTokensAsync(_currentTokens);
                return _currentTokens;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "💀 couldn't refresh tokens, not bussin");
            }
        }

        // pulling from storage rn
        var stored = await tokenStorage.LoadAsync();
        if (stored == null) return null;

        try
        {
            _currentTokens = await RefreshTokenAsync(stored.EncryptedRefreshToken);
            _currentCharacter = stored.Character;
            await StoreTokensAsync(_currentTokens);
            return _currentTokens;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "💀 refresh on stored tokens flopped hard");
            return null;
        }
    }

    public async Task StoreTokensAsync(EveTokens tokens)
    {
        _currentTokens = tokens;
        _currentCharacter = ParseJwtToken(tokens.AccessToken);
        await tokenStorage.SaveAsync(_currentCharacter, tokens.RefreshToken);
    }

    private static string Base64UrlEncode(byte[] bytes)
    {
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}
