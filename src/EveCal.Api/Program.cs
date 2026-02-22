using EveCal.Api.Controllers;
using EveCal.Api.Infrastructure;
using EveCal.Api.Models;
using EveCal.Api.Services;
using LittyLogs;
using LittyLogs.File;
using LittyLogs.Webhooks;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// finna wire up these services
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

// adding the HTTP clients fr
builder.Services.AddEsiHttpClient();

// the main services go crazy here
builder.Services.AddSingleton<ITokenStorage, TokenStorage>();
builder.Services.AddSingleton<IEveAuthService, EveAuthService>();
builder.Services.AddSingleton<IEveCalendarService, EveCalendarService>();
builder.Services.AddSingleton<IICalGeneratorService, ICalGeneratorService>();

builder.Services.AddControllers();
builder.Services.AddOpenApi();

// litty-logs NuGet package goes absolutely feral on these logs fr fr 🔥
builder.Logging.ClearProviders();
builder.Logging.AddLittyLogs();
builder.Logging.AddLittyFileLogs(opts =>
{
    opts.FilePath = "logs/evecal.log";
    opts.RollingInterval = LittyRollingInterval.Daily;
    opts.MaxFileSizeBytes = 10 * 1024 * 1024;
});

// matrix webhook logging — the room stays informed no cap 📨
var webhookUrl = builder.Configuration["Matrix:WebhookUrl"]
    ?? Environment.GetEnvironmentVariable("MATRIX_WEBHOOK_URL");
if (!string.IsNullOrEmpty(webhookUrl))
{
    builder.Logging.AddLittyMatrixLogs(webhookUrl, opts =>
    {
        opts.MinimumLevel = LogLevel.Warning;
        opts.Username = "EveCal";
    });
}

// Kestrel vibing on port 8080
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(8080);
});

var app = builder.Build();

// checking if we in setup mode rn
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

    // see if this thing already set up
    if (tokenStorage.HasStoredTokens())
    {
        logger.LogInformation("📁 tokens already exist bestie, delete /app/data/tokens.enc to reconfigure");
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

    // web server running in the back, lowkey
    using var cts = new CancellationTokenSource();
    var serverTask = app.RunAsync(cts.Token);

    try
    {
        // waiting for OAuth callback, not gonna wait forever tho
        var timeoutTask = Task.Delay(TimeSpan.FromMinutes(5));
        var completedTask = await Task.WhenAny(completion.Task, timeoutTask);

        if (completedTask == timeoutTask)
        {
            logger.LogError("⏰ setup took too long waiting for OAuth, big L");
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
        logger.LogError(ex, "💀 setup flopped fr");
        Console.WriteLine($"Setup failed: {ex.Message}");
    }

    // let the response cook real quick then dip
    await Task.Delay(1000);
    await cts.CancelAsync();
}

static async Task RunNormalModeAsync(WebApplication app)
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    var authService = app.Services.GetRequiredService<IEveAuthService>();
    var tokenStorage = app.Services.GetRequiredService<ITokenStorage>();
    var config = app.Services.GetRequiredService<IOptions<EveConfiguration>>().Value;

    // do we even have tokens bestie
    if (!tokenStorage.HasStoredTokens())
    {
        logger.LogWarning("😤 no tokens found bestie, run setup first: docker-compose run --rm --service-ports evecal setup");
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
        // making sure these tokens hit different
        try
        {
            var tokens = await authService.GetValidTokensAsync();
            if (tokens != null)
            {
                var character = authService.ParseJwtToken(tokens.AccessToken);
                logger.LogInformation("🔒 we locked in with {Name} ({Id})",
                    character.CharacterName, character.CharacterId);

            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "💀 couldn't load tokens, run setup again bestie");
        }
    }

    // construct the feed URL from callback URL (replace /callback with /calendar/feed.ics)
    var feedUrl = config.CallbackUrl.Replace("/callback", "/calendar/feed.ics");
    logger.LogInformation("🔥 calendar feed is bussin at {FeedUrl}", feedUrl);
    await app.RunAsync();
}

public partial class Program { }
