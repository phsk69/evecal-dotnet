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
            logger.LogError("💀 bruh OAuth fumbled: {Error} - {Description}", error, error_description);
            return BadRequest(new { error, description = error_description });
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
            return StatusCode(500, new { error = "💀 Failed to complete authentication fr", details = ex.Message });
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
}
