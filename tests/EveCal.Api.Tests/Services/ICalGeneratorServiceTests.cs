using EveCal.Api.Infrastructure;
using EveCal.Api.Models;
using EveCal.Api.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Moq;
using Xunit;

namespace EveCal.Api.Tests.Services;

/// <summary>
/// tests for ICalGeneratorService, making sure descriptions get cleaned up fr fr
/// </summary>
public class ICalGeneratorServiceTests
{
    private readonly Mock<IEveCalendarService> _mockCalendarService;
    private readonly ILogger<ICalGeneratorService> _logger;
    private readonly ICalGeneratorService _service;

    public ICalGeneratorServiceTests()
    {
        _mockCalendarService = new Mock<IEveCalendarService>();

        // real litty logger so test output is bussin too 🔥
        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole(options => options.FormatterName = "litty");
            builder.AddConsoleFormatter<LittyConsoleFormatter, LittyConsoleFormatterOptions>();
        });
        _logger = loggerFactory.CreateLogger<ICalGeneratorService>();

        _service = new ICalGeneratorService(_mockCalendarService.Object, _logger);
    }

    [Fact]
    public async Task GenerateFeedAsync_YeetsShowinfoTags_KeepsTextContent()
    {
        // arrange - showinfo tags in the description, very common in EVE
        var events = new List<EveCalendarEventDetail>
        {
            new()
            {
                EventId = 1,
                Title = "Moon Extraction",
                Text = """The moon chunk extraction for <a href="showinfo:35835//1051420707605">Bamiette - Mongstar RoidHouse</a> at <a href="showinfo:14//4017113">Bamiette VIII - Moon 15</a> will complete at this time.""",
                Date = DateTime.UtcNow.AddDays(1),
                Duration = 60,
                Importance = 1,
                OwnerName = "Test Corp",
                OwnerType = "corporation"
            }
        };

        _mockCalendarService
            .Setup(x => x.GetCorporationEventsAsync())
            .ReturnsAsync(events);

        // act
        var result = await _service.GenerateFeedAsync();

        // assert - showinfo tags should be yeeted but text stays
        Assert.Contains("Bamiette - Mongstar RoidHouse", result);
        Assert.Contains("Bamiette VIII - Moon 15", result);
        Assert.DoesNotContain("showinfo", result);
        Assert.DoesNotContain("<a href", result);
        Assert.DoesNotContain("</a>", result);
    }

    [Fact]
    public async Task GenerateFeedAsync_ConvertsBrTagsToNewlines()
    {
        // arrange - br tags in various formats
        var events = new List<EveCalendarEventDetail>
        {
            new()
            {
                EventId = 2,
                Title = "Fleet Op",
                Text = "Line 1<br>Line 2<br/>Line 3<br />Line 4",
                Date = DateTime.UtcNow.AddDays(1),
                Duration = 120,
                Importance = 2,
                OwnerName = "Test Corp",
                OwnerType = "corporation"
            }
        };

        _mockCalendarService
            .Setup(x => x.GetCorporationEventsAsync())
            .ReturnsAsync(events);

        // act
        var result = await _service.GenerateFeedAsync();

        // assert - br tags should become newlines
        Assert.DoesNotContain("<br>", result);
        Assert.DoesNotContain("<br/>", result);
        Assert.DoesNotContain("<br />", result);
    }

    [Fact]
    public async Task GenerateFeedAsync_DecodesHtmlEntities()
    {
        // arrange - HTML entities that need decoding
        var events = new List<EveCalendarEventDetail>
        {
            new()
            {
                EventId = 3,
                Title = "Mining Op",
                Text = "Rocks &amp; Minerals &lt;valuable&gt; stuff&nbsp;here",
                Date = DateTime.UtcNow.AddDays(1),
                Duration = 180,
                Importance = 1,
                OwnerName = "Test Corp",
                OwnerType = "corporation"
            }
        };

        _mockCalendarService
            .Setup(x => x.GetCorporationEventsAsync())
            .ReturnsAsync(events);

        // act
        var result = await _service.GenerateFeedAsync();

        // assert - entities should be decoded
        Assert.DoesNotContain("&amp;", result);
        Assert.DoesNotContain("&lt;", result);
        Assert.DoesNotContain("&gt;", result);
        Assert.DoesNotContain("&nbsp;", result);
    }

    [Fact]
    public async Task GenerateFeedAsync_HandlesEmptyDescription()
    {
        // arrange - empty description shouldnt break anything
        var events = new List<EveCalendarEventDetail>
        {
            new()
            {
                EventId = 4,
                Title = "Mystery Event",
                Text = "",
                Date = DateTime.UtcNow.AddDays(1),
                Duration = 60,
                Importance = 1,
                OwnerName = "Test Corp",
                OwnerType = "corporation"
            }
        };

        _mockCalendarService
            .Setup(x => x.GetCorporationEventsAsync())
            .ReturnsAsync(events);

        // act
        var result = await _service.GenerateFeedAsync();

        // assert - should still generate valid ICS
        Assert.Contains("BEGIN:VCALENDAR", result);
        Assert.Contains("END:VCALENDAR", result);
        Assert.Contains("Mystery Event", result);
    }

    [Fact]
    public async Task GenerateFeedAsync_AddsAsteriskPrefixToTitle()
    {
        // arrange - title should get asterisk prefix for the boomers who import
        var events = new List<EveCalendarEventDetail>
        {
            new()
            {
                EventId = 5,
                Title = "Fleet Stratop",
                Text = "Important fleet",
                Date = DateTime.UtcNow.AddDays(1),
                Duration = 120,
                Importance = 2,
                OwnerName = "Test Corp",
                OwnerType = "corporation"
            }
        };

        _mockCalendarService
            .Setup(x => x.GetCorporationEventsAsync())
            .ReturnsAsync(events);

        // act
        var result = await _service.GenerateFeedAsync();

        // assert - title should have asterisk prefix
        Assert.Contains("SUMMARY:* Fleet Stratop", result);
    }

    [Fact]
    public async Task GenerateFeedAsync_SetsCorrectPriorityBasedOnImportance()
    {
        // arrange - different importance levels
        var events = new List<EveCalendarEventDetail>
        {
            new()
            {
                EventId = 6,
                Title = "High Priority Event",
                Text = "Very important",
                Date = DateTime.UtcNow.AddDays(1),
                Duration = 60,
                Importance = 2, // high
                OwnerName = "Test Corp",
                OwnerType = "corporation"
            }
        };

        _mockCalendarService
            .Setup(x => x.GetCorporationEventsAsync())
            .ReturnsAsync(events);

        // act
        var result = await _service.GenerateFeedAsync();

        // assert - importance 2 should map to priority 1 (high)
        Assert.Contains("PRIORITY:1", result);
    }

    [Fact]
    public async Task GenerateFeedAsync_YeetsGenericAnchorTags()
    {
        // arrange - regular anchor tags (not showinfo)
        var events = new List<EveCalendarEventDetail>
        {
            new()
            {
                EventId = 7,
                Title = "Link Event",
                Text = """Check out <a href="https://example.com">this link</a> for details""",
                Date = DateTime.UtcNow.AddDays(1),
                Duration = 60,
                Importance = 1,
                OwnerName = "Test Corp",
                OwnerType = "corporation"
            }
        };

        _mockCalendarService
            .Setup(x => x.GetCorporationEventsAsync())
            .ReturnsAsync(events);

        // act
        var result = await _service.GenerateFeedAsync();

        // assert - anchor tags yeeted, text kept
        Assert.Contains("this link", result);
        Assert.DoesNotContain("<a href", result);
        Assert.DoesNotContain("</a>", result);
    }
}
