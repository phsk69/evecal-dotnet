using System.Text.Json.Serialization;

namespace EveCal.Api.Models;

public class EveCalendarEventSummary
{
    [JsonPropertyName("event_id")]
    public int EventId { get; set; }

    [JsonPropertyName("event_date")]
    public DateTime EventDate { get; set; }

    [JsonPropertyName("event_response")]
    public string? EventResponse { get; set; }

    [JsonPropertyName("importance")]
    public int Importance { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;
}

public class EveCalendarEventDetail
{
    [JsonPropertyName("event_id")]
    public int EventId { get; set; }

    [JsonPropertyName("date")]
    public DateTime Date { get; set; }

    [JsonPropertyName("duration")]
    public int Duration { get; set; }

    [JsonPropertyName("importance")]
    public int Importance { get; set; }

    [JsonPropertyName("owner_id")]
    public int OwnerId { get; set; }

    [JsonPropertyName("owner_name")]
    public string OwnerName { get; set; } = string.Empty;

    [JsonPropertyName("owner_type")]
    public string OwnerType { get; set; } = string.Empty;

    [JsonPropertyName("response")]
    public string? Response { get; set; }

    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;
}

public class EveCalendarAttendee
{
    [JsonPropertyName("character_id")]
    public int CharacterId { get; set; }

    [JsonPropertyName("event_response")]
    public string? EventResponse { get; set; }
}
