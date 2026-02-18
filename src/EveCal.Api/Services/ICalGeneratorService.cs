using System.Text.RegularExpressions;
using EveCal.Api.Models;
using Ical.Net;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using Ical.Net.Serialization;

namespace EveCal.Api.Services;

public interface IICalGeneratorService
{
    Task<string> GenerateFeedAsync();
}

public class ICalGeneratorService(
    IEveCalendarService calendarService,
    ILogger<ICalGeneratorService> logger) : IICalGeneratorService
{
    private string? _cachedFeed;
    private DateTime _cacheExpiry = DateTime.MinValue;

    public async Task<string> GenerateFeedAsync()
    {
        // cached feed if it still slaps
        if (_cachedFeed != null && DateTime.UtcNow < _cacheExpiry)
        {
            return _cachedFeed;
        }

        var events = await calendarService.GetCorporationEventsAsync();

        var calendar = new Calendar
        {
            ProductId = "-//EveCal//EVE Online Calendar//EN"
        };
        calendar.AddProperty("X-WR-CALNAME", "EVE Corp Calendar");
        calendar.AddProperty("X-WR-CALDESC", "Corporation calendar events from EVE Online");

        foreach (var evt in events)
        {
            var calEvent = new CalendarEvent
            {
                Uid = $"{evt.EventId}@eveonline.com",
                Summary = $"* {evt.Title}",
                Description = CleanDescription(evt.Text),
                DtStart = new CalDateTime(evt.Date, "UTC"),
                DtEnd = new CalDateTime(evt.Date.AddMinutes(evt.Duration), "UTC"),
                Organizer = new Organizer { CommonName = evt.OwnerName },
                // importance = priority vibes (1=high, 5=mid, 9=low)
                Priority = evt.Importance switch
                {
                    2 => 1,  // High
                    1 => 5,  // Normal
                    _ => 9   // Low
                }
            };

            calendar.Events.Add(calEvent);
        }

        var serializer = new CalendarSerializer();
        _cachedFeed = serializer.SerializeToString(calendar) ?? string.Empty;
        _cacheExpiry = DateTime.UtcNow.AddMinutes(5);

        logger.LogInformation("🔥 ICS feed generated with {Count} events, absolutely ate", events.Count);

        return _cachedFeed;
    }

    private static string CleanDescription(string text)
    {
        // EVE calendar descriptions got HTML tags and showinfo links, we cleaning that up fr
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;

        // yeet the showinfo anchor tags but keep the text inside them
        var cleaned = Regex.Replace(text, @"<a\s+href=""showinfo:[^""]*"">([^<]*)</a>", "$1");

        // also yeet any other anchor tags that might be lurking
        cleaned = Regex.Replace(cleaned, @"<a\s[^>]*>([^<]*)</a>", "$1");

        return cleaned
            .Replace("<br>", "\n")
            .Replace("<br/>", "\n")
            .Replace("<br />", "\n")
            .Replace("&amp;", "&")
            .Replace("&lt;", "<")
            .Replace("&gt;", ">")
            .Replace("&nbsp;", " ");
    }
}
