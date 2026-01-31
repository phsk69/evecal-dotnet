namespace EveCal.Api.Models;

public class SsoCharacter
{
    public required int CharacterId { get; set; }
    public required string CharacterName { get; set; }
    public int? CorporationId { get; set; }
    public string? CorporationName { get; set; }
    public required DateTime ExpiresOn { get; set; }
    public required string[] Scopes { get; set; }
}

public record StoredCharacterData
{
    public required SsoCharacter Character { get; init; }
    public required string EncryptedRefreshToken { get; init; }
    public required DateTime LastRefresh { get; init; }
}
