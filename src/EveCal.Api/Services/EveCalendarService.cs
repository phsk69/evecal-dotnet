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
        // serving cached if it still hits
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

        // grabbing them calendar event summaries
        var summaries = await GetEventSummariesAsync(client, character.CharacterId);
        logger.LogInformation("📅 found {Count} calendar events, lowkey lit", summaries.Count);

        // getting the deets for each event, only corp ones tho
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
                logger.LogWarning(ex, "😤 couldn't get deets for event {EventId}, it's giving nothing", summary.EventId);
            }
        }

        logger.LogInformation("🍽️ found {Count} corp events, we eating good", corpEvents.Count);

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
            var safeError = error.Length > 1000 ? error[..997] + "..." : error;
            logger.LogError("🚫 calendar said no: {Status} - {Error}",
                response.StatusCode, safeError);
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
            logger.LogWarning("👻 event {EventId} ghosted us: {Status}",
                eventId, response.StatusCode);
            return null;
        }

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<EveCalendarEventDetail>(json);
    }
}
