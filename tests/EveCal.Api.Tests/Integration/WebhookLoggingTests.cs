using LittyLogs.Webhooks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace EveCal.Api.Tests.Integration;

/// <summary>
/// webhook logging tests that go absolutely feral — if MATRIX_WEBHOOK_URL aint set we fail hard
/// because observability is non-negotiable no cap 🔥📨
/// </summary>
public class WebhookLoggingTests
{
    private static string GetRequiredWebhookUrl()
    {
        var url = Environment.GetEnvironmentVariable("MATRIX_WEBHOOK_URL");
        Assert.False(
            string.IsNullOrEmpty(url),
            "💀 MATRIX_WEBHOOK_URL env var not set — observability is non-negotiable bestie, configure it in .env or CI secrets");
        return url!;
    }

    [Fact]
    public void WebhookUrl_MustBeConfigured_OrWeRiot()
    {
        // the canary in the coal mine fr fr 🐦
        var url = GetRequiredWebhookUrl();
        Assert.StartsWith("http", url);
    }

    [Fact]
    public void WebhookProvider_RegistersWhenUrlIsConfigured()
    {
        // webhook provider better be in there when we give it a URL no cap 🔌
        var url = GetRequiredWebhookUrl();

        var services = new ServiceCollection();
        services.AddLogging(builder =>
        {
            builder.AddLittyMatrixLogs(url, opts =>
            {
                opts.MinimumLevel = LogLevel.Warning;
                opts.Username = "EveCal-Tests";
            });
        });

        using var provider = services.BuildServiceProvider();
        var loggerProviders = provider.GetServices<ILoggerProvider>();

        Assert.Contains(loggerProviders, p => p.GetType().FullName!.Contains("LittyWebhook"));
    }

    [Fact]
    public void WebhookProvider_NotRegistered_WhenNoUrlConfigured()
    {
        // if we dont add the webhook provider it shouldnt be there, simple as 🚫
        var services = new ServiceCollection();
        services.AddLogging(builder =>
        {
            builder.AddConsole();
        });

        using var provider = services.BuildServiceProvider();
        var loggerProviders = provider.GetServices<ILoggerProvider>();

        Assert.DoesNotContain(loggerProviders, p => p.GetType().FullName!.Contains("LittyWebhook"));
    }

    [Fact]
    public async Task WebhookProvider_ActuallySendsToMatrix_AndDoesntExplode()
    {
        // the real deal — actually yeet a message to Matrix and pray it doesnt crash 📨🔥
        var url = GetRequiredWebhookUrl();

        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddLittyMatrixLogs(url, opts =>
            {
                opts.MinimumLevel = LogLevel.Warning;
                opts.Username = "EveCal-Tests";
            });
        });

        var logger = loggerFactory.CreateLogger("EveCal.WebhookTest");

        // send it bestie — this should land in the Matrix room 🚀
        logger.LogWarning("🧪 webhook integration test from evecal — if you see this its bussin fr fr");

        // let the batch flush cook before we dispose 🍳
        await Task.Delay(3000);
    }
}
