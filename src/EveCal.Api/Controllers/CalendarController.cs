using EveCal.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace EveCal.Api.Controllers;

[ApiController]
[Route("calendar")]
public class CalendarController(
    IICalGeneratorService icalGenerator,
    ILogger<CalendarController> logger) : ControllerBase
{
    [HttpGet("feed.ics")]
    public async Task<IActionResult> GetFeed()
    {
        try
        {
            var feed = await icalGenerator.GenerateFeedAsync();
            return Content(feed, "text/calendar", System.Text.Encoding.UTF8);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("No valid tokens"))
        {
            logger.LogWarning("Calendar requested but no valid tokens available");
            return StatusCode(503, new { error = "Service not configured. Run setup first." });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to generate calendar feed");
            return StatusCode(500, new { error = "Failed to generate calendar feed" });
        }
    }

    [HttpGet("status")]
    public async Task<IActionResult> GetStatus([FromServices] IEveAuthService authService)
    {
        try
        {
            var tokens = await authService.GetValidTokensAsync();
            if (tokens == null)
            {
                return Ok(new
                {
                    status = "not_configured",
                    message = "Run setup to configure OAuth tokens"
                });
            }

            var character = authService.ParseJwtToken(tokens.AccessToken);
            return Ok(new
            {
                status = "ready",
                character = new
                {
                    id = character.CharacterId,
                    name = character.CharacterName
                },
                tokenExpiresAt = tokens.ExpiresAt
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get status");
            return StatusCode(500, new { error = "Failed to get status" });
        }
    }
}
