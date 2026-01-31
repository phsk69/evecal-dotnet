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

public class ICalGeneratorService : IICalGeneratorService
{
    private readonly IEveCalendarService _calendarService;
    private readonly ILogger<ICalGeneratorService> _logger;

    private string? _cachedFeed;
    private DateTime _cacheExpiry = DateTime.MinValue;

    public ICalGeneratorService(
        IEveCalendarService calendarService,
        ILogger<ICalGeneratorService> logger)
    {
        _calendarService = calendarService;
        _logger = logger;
    }

    public async Task<string> GenerateFeedAsync()
    {
        // Return cached if valid
        if (_cachedFeed != null && DateTime.UtcNow < _cacheExpiry)
        {
            return _cachedFeed;
        }

        var events = await _calendarService.GetCorporationEventsAsync();

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
                Summary = evt.Title,
                Description = CleanDescription(evt.Text),
                DtStart = new CalDateTime(evt.Date, "UTC"),
                DtEnd = new CalDateTime(evt.Date.AddMinutes(evt.Duration), "UTC"),
                Organizer = new Organizer { CommonName = evt.OwnerName }
            };

            // Add importance as priority (1=high, 5=normal, 9=low)
            calEvent.Priority = evt.Importance switch
            {
                2 => 1,  // High
                1 => 5,  // Normal
                _ => 9   // Low
            };

            calendar.Events.Add(calEvent);
        }

        var serializer = new CalendarSerializer();
        _cachedFeed = serializer.SerializeToString(calendar) ?? string.Empty;
        _cacheExpiry = DateTime.UtcNow.AddMinutes(5);

        _logger.LogInformation("Generated ICS feed with {Count} events", events.Count);

        return _cachedFeed;
    }

    private static string CleanDescription(string text)
    {
        // EVE calendar descriptions may contain HTML-like tags
        // Clean them up for plain text display
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;

        return text
            .Replace("<br>", "\n")
            .Replace("<br/>", "\n")
            .Replace("<br />", "\n")
            .Replace("&amp;", "&")
            .Replace("&lt;", "<")
            .Replace("&gt;", ">")
            .Replace("&nbsp;", " ");
    }
}
