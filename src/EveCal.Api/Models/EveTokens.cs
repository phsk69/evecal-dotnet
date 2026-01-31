namespace EveCal.Api.Models;

public class EveTokens
{
    public required string AccessToken { get; set; }
    public required string RefreshToken { get; set; }
    public required DateTime ExpiresAt { get; set; }
    public required string TokenType { get; set; }

    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool NeedsRefresh => DateTime.UtcNow >= ExpiresAt.AddMinutes(-2);
}

#pragma warning disable IDE1006 // Naming Styles
public class TokenResponse
{
    public required string access_token { get; set; }
    public required string refresh_token { get; set; }
    public required int expires_in { get; set; }
    public required string token_type { get; set; }
}
