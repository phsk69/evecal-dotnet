using EveCal.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace EveCal.Api.Controllers;

[ApiController]
[Route("")]
public class AuthController(
    IEveAuthService authService,
    ILogger<AuthController> logger) : ControllerBase
{

    // bestie we storing the PKCE state here no cap
    private static string? _pendingCodeVerifier;
    private static string? _pendingState;
    private static TaskCompletionSource<bool>? _setupCompletionSource;

    [HttpGet("callback")]
    public async Task<IActionResult> Callback(
        [FromQuery] string? code,
        [FromQuery] string? state,
        [FromQuery] string? error,
        [FromQuery] string? error_description)
    {
        if (!string.IsNullOrEmpty(error))
        {
            var safeError = Truncate(error, 200);
            var safeDesc = Truncate(error_description, 500);
            logger.LogError("💀 bruh OAuth fumbled: {Error} - {Description}", safeError, safeDesc);
            return BadRequest(new { error = safeError, description = safeDesc });
        }

        if (string.IsNullOrEmpty(code))
        {
            return BadRequest(new { error = "💀 Missing authorization code bestie" });
        }

        if (_pendingCodeVerifier == null)
        {
            return BadRequest(new { error = "😤 No pending authorization. Start setup first bestie." });
        }

        try
        {
            logger.LogInformation("✨ slay we got the OAuth callback, finna exchange that code");

            var tokens = await authService.ExchangeCodeAsync(code, _pendingCodeVerifier);
            await authService.StoreTokensAsync(tokens);

            var character = authService.ParseJwtToken(tokens.AccessToken);

            logger.LogInformation("🔥 setup is bussin for {Name} ({Id}) no cap",
                character.CharacterName, character.CharacterId);

            // setup just ate fr fr
            _setupCompletionSource?.TrySetResult(true);

            // yeet the pending state
            _pendingCodeVerifier = null;
            _pendingState = null;

            return Ok(new
            {
                success = true,
                message = "🔥 Setup complete! You can close this window bestie.",
                character = new
                {
                    id = character.CharacterId,
                    name = character.CharacterName
                }
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "💀 OAuth flow caught an L fr");
            _setupCompletionSource?.TrySetException(ex);
            return StatusCode(500, new { error = "💀 authentication flopped, check server logs bestie" });
        }
    }

    public static (string AuthUrl, TaskCompletionSource<bool> Completion) StartSetupFlow(IEveAuthService authService)
    {
        var (authUrl, codeVerifier, state) = authService.GenerateAuthorizationUrl();
        _pendingCodeVerifier = codeVerifier;
        _pendingState = state;
        _setupCompletionSource = new TaskCompletionSource<bool>();
        return (authUrl, _setupCompletionSource);
    }

    // keeps strings from going feral on us 📏
    private static string? Truncate(string? value, int maxLength) =>
        value?.Length > maxLength ? value[..maxLength] + "..." : value;
}
