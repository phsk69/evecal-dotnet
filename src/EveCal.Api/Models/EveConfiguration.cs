namespace EveCal.Api.Models;

public class EveConfiguration
{
    public required string ClientId { get; set; }
    public required string CallbackUrl { get; set; }
    public string Scopes { get; set; } = "esi-calendar.read_calendar_events.v1 esi-corporations.read_corporation_membership.v1 esi-characters.read_corporation_roles.v1";
    public string DataPath { get; set; } = "/app/data";
    public int CalendarRefreshMinutes { get; set; } = 15;
}

#pragma warning disable IDE1006 // Naming Styles
public class OAuthDiscovery
{
    public string authorization_endpoint { get; set; } = string.Empty;
    public string token_endpoint { get; set; } = string.Empty;
    public string jwks_uri { get; set; } = string.Empty;
    public string issuer { get; set; } = string.Empty;
}
