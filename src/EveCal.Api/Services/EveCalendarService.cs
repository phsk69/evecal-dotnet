using System.Net.Http.Headers;
using System.Text.Json;
using EveCal.Api.Models;

namespace EveCal.Api.Services;

public interface IEveCalendarService
{
    Task<List<EveCalendarEventDetail>> GetCorporationEventsAsync();
}

public class EveCalendarService(
    IHttpClientFactory httpClientFactory,
    IEveAuthService authService,
    ILogger<EveCalendarService> logger) : IEveCalendarService
{
    private List<EveCalendarEventDetail>? _cachedEvents;
    private DateTime _cacheExpiry = DateTime.MinValue;

    public async Task<List<EveCalendarEventDetail>> GetCorporationEventsAsync()
    {
        // Return cached if valid
        if (_cachedEvents != null && DateTime.UtcNow < _cacheExpiry)
        {
            return _cachedEvents;
        }

        var tokens = await authService.GetValidTokensAsync();
        if (tokens == null)
        {
            throw new InvalidOperationException("No valid tokens available. Run setup first.");
        }

        var character = authService.ParseJwtToken(tokens.AccessToken);
        var client = httpClientFactory.CreateClient("ESI");
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        // Get calendar event summaries
        var summaries = await GetEventSummariesAsync(client, character.CharacterId);
        logger.LogInformation("Found {Count} calendar events", summaries.Count);

        // Get details for each event and filter to corporation events only
        var corpEvents = new List<EveCalendarEventDetail>();

        foreach (var summary in summaries)
        {
            try
            {
                var detail = await GetEventDetailAsync(client, character.CharacterId, summary.EventId);
                if (detail != null && detail.OwnerType.Equals("corporation", StringComparison.OrdinalIgnoreCase))
                {
                    corpEvents.Add(detail);
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to get details for event {EventId}", summary.EventId);
            }
        }

        logger.LogInformation("Found {Count} corporation events", corpEvents.Count);

        _cachedEvents = corpEvents;
        _cacheExpiry = DateTime.UtcNow.AddMinutes(5);

        return corpEvents;
    }

    private async Task<List<EveCalendarEventSummary>> GetEventSummariesAsync(HttpClient client, int characterId)
    {
        var response = await client.GetAsync($"/v1/characters/{characterId}/calendar/");

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            logger.LogError("Failed to get calendar: {Status} - {Error}",
                response.StatusCode, error);
            throw new InvalidOperationException($"ESI calendar request failed: {response.StatusCode}");
        }

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<List<EveCalendarEventSummary>>(json) ?? [];
    }

    private async Task<EveCalendarEventDetail?> GetEventDetailAsync(
        HttpClient client, int characterId, int eventId)
    {
        var response = await client.GetAsync($"/v3/characters/{characterId}/calendar/{eventId}/");

        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("Failed to get event {EventId}: {Status}",
                eventId, response.StatusCode);
            return null;
        }

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<EveCalendarEventDetail>(json);
    }
}
