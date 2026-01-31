namespace EveCal.Api.Controllers;

[ApiController]
[Route("")]
public class AuthController(
    IEveAuthService authService,
    ILogger<AuthController> logger) : ControllerBase
{

    // Static storage for PKCE state during setup flow
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
            logger.LogError("OAuth error: {Error} - {Description}", error, error_description);
            return BadRequest(new { error, description = error_description });
        }

        if (string.IsNullOrEmpty(code))
        {
            return BadRequest(new { error = "Missing authorization code" });
        }

        if (_pendingCodeVerifier == null)
        {
            return BadRequest(new { error = "No pending authorization. Start setup first." });
        }

        try
        {
            logger.LogInformation("Received OAuth callback, exchanging code for tokens");

            var tokens = await authService.ExchangeCodeAsync(code, _pendingCodeVerifier);
            await authService.StoreTokensAsync(tokens);

            var character = authService.ParseJwtToken(tokens.AccessToken);

            logger.LogInformation("Setup complete for character {Name} ({Id})",
                character.CharacterName, character.CharacterId);

            // Signal setup completion
            _setupCompletionSource?.TrySetResult(true);

            // Clear pending state
            _pendingCodeVerifier = null;
            _pendingState = null;

            return Ok(new
            {
                success = true,
                message = "Setup complete! You can close this window.",
                character = new
                {
                    id = character.CharacterId,
                    name = character.CharacterName
                }
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to complete OAuth flow");
            _setupCompletionSource?.TrySetException(ex);
            return StatusCode(500, new { error = "Failed to complete authentication", details = ex.Message });
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
