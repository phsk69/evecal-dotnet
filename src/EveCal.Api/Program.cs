using EveCal.Api.Controllers;
using EveCal.Api.Infrastructure;
using EveCal.Api.Models;
using EveCal.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Configure services
builder.Services.Configure<EveConfiguration>(options =>
{
    var clientId = builder.Configuration["Eve:ClientId"];
    options.ClientId = !string.IsNullOrEmpty(clientId)
        ? clientId
        : Environment.GetEnvironmentVariable("EVE_CLIENT_ID")
            ?? throw new InvalidOperationException("EVE_CLIENT_ID not configured");

    var callbackUrl = builder.Configuration["Eve:CallbackUrl"];
    options.CallbackUrl = !string.IsNullOrEmpty(callbackUrl)
        ? callbackUrl
        : Environment.GetEnvironmentVariable("EVE_CALLBACK_URL")
            ?? "http://localhost:8080/callback";

    options.Scopes = builder.Configuration["Eve:Scopes"]
        ?? Environment.GetEnvironmentVariable("EVE_SCOPES")
        ?? "esi-calendar.read_calendar_events.v1 esi-corporations.read_corporation_membership.v1 esi-characters.read_corporation_roles.v1";

    options.DataPath = builder.Configuration["Eve:DataPath"]
        ?? Environment.GetEnvironmentVariable("EVE_DATA_PATH")
        ?? "/app/data";

    var refreshMinutes = builder.Configuration["Eve:CalendarRefreshMinutes"]
        ?? Environment.GetEnvironmentVariable("CALENDAR_REFRESH_MINUTES");
    if (int.TryParse(refreshMinutes, out var minutes))
    {
        options.CalendarRefreshMinutes = minutes;
    }
});

// Add HTTP clients
builder.Services.AddEsiHttpClient();

// Add application services
builder.Services.AddSingleton<ITokenStorage, TokenStorage>();
builder.Services.AddSingleton<IEveAuthService, EveAuthService>();
builder.Services.AddSingleton<IEveCalendarService, EveCalendarService>();
builder.Services.AddSingleton<IICalGeneratorService, ICalGeneratorService>();

builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Configure Kestrel to listen on port 8080
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(8080);
});

var app = builder.Build();

// Check if running in setup mode
var isSetupMode = args.Contains("setup");

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapControllers();

if (isSetupMode)
{
    await RunSetupModeAsync(app);
}
else
{
    await RunNormalModeAsync(app);
}

static async Task RunSetupModeAsync(WebApplication app)
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    var authService = app.Services.GetRequiredService<IEveAuthService>();
    var tokenStorage = app.Services.GetRequiredService<ITokenStorage>();

    // Check if already configured
    if (tokenStorage.HasStoredTokens())
    {
        logger.LogInformation("Tokens already exist. Delete /app/data/tokens.enc to reconfigure.");
        Console.WriteLine();
        Console.WriteLine("============================================");
        Console.WriteLine("Tokens already exist!");
        Console.WriteLine("Delete the tokens.enc file to reconfigure:");
        Console.WriteLine("  rm ./data/tokens.enc");
        Console.WriteLine("============================================");
        Console.WriteLine();

        Console.Write("Continue anyway? [y/N]: ");
        var response = Console.ReadLine();
        if (!response?.Equals("y", StringComparison.OrdinalIgnoreCase) ?? true)
        {
            return;
        }
    }

    var (authUrl, completion) = AuthController.StartSetupFlow(authService);

    Console.WriteLine();
    Console.WriteLine("============================================");
    Console.WriteLine("EVE Calendar Service - Setup");
    Console.WriteLine("============================================");
    Console.WriteLine();
    Console.WriteLine("Open this URL in your browser:");
    Console.WriteLine();
    Console.WriteLine(authUrl);
    Console.WriteLine();
    Console.WriteLine("Waiting for callback...");
    Console.WriteLine("============================================");
    Console.WriteLine();

    // Start the web server in the background
    var serverTask = app.RunAsync();

    try
    {
        // Wait for the OAuth callback with a timeout
        var timeoutTask = Task.Delay(TimeSpan.FromMinutes(5));
        var completedTask = await Task.WhenAny(completion.Task, timeoutTask);

        if (completedTask == timeoutTask)
        {
            logger.LogError("Setup timed out waiting for OAuth callback");
            Console.WriteLine("Setup timed out. Please try again.");
        }
        else if (completion.Task.IsCompletedSuccessfully)
        {
            Console.WriteLine();
            Console.WriteLine("============================================");
            Console.WriteLine("Setup complete!");
            Console.WriteLine("You can now run the service in normal mode:");
            Console.WriteLine("  docker-compose up -d");
            Console.WriteLine("============================================");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Setup failed");
        Console.WriteLine($"Setup failed: {ex.Message}");
    }

    // Give time for the response to be sent
    await Task.Delay(1000);
    await app.StopAsync();
}

static async Task RunNormalModeAsync(WebApplication app)
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    var authService = app.Services.GetRequiredService<IEveAuthService>();
    var tokenStorage = app.Services.GetRequiredService<ITokenStorage>();

    // Check if tokens exist
    if (!tokenStorage.HasStoredTokens())
    {
        logger.LogWarning("No tokens found. Run setup first: docker-compose run --rm --service-ports evecal setup");
        Console.WriteLine();
        Console.WriteLine("============================================");
        Console.WriteLine("No tokens configured!");
        Console.WriteLine("Run setup first:");
        Console.WriteLine("  docker-compose run --rm --service-ports evecal setup");
        Console.WriteLine("============================================");
        Console.WriteLine();
    }
    else
    {
        // Verify tokens are valid
        try
        {
            var tokens = await authService.GetValidTokensAsync();
            if (tokens != null)
            {
                var character = authService.ParseJwtToken(tokens.AccessToken);
                logger.LogInformation("Starting with character {Name} ({Id})",
                    character.CharacterName, character.CharacterId);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load tokens. Run setup again.");
        }
    }

    logger.LogInformation("Calendar feed available at http://localhost:8080/calendar/feed.ics");
    await app.RunAsync();
}

public partial class Program { }
