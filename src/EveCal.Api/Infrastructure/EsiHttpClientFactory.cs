namespace EveCal.Api.Infrastructure;

public class EsiHttpClientHandler(ILogger<EsiHttpClientHandler> logger) : DelegatingHandler
{
    private static readonly Random Random = new();
    private static int _errorLimitRemain = 100;
    private static DateTime _errorLimitReset = DateTime.UtcNow;

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        // Add human-like delay
        var delay = Random.Next(100, 500);
        await Task.Delay(delay, cancellationToken);

        // Check rate limit status
        if (_errorLimitRemain < 10 && DateTime.UtcNow < _errorLimitReset)
        {
            var waitTime = _errorLimitReset - DateTime.UtcNow;
            logger.LogWarning("Rate limit low ({Remaining}), waiting {Seconds}s",
                _errorLimitRemain, waitTime.TotalSeconds);
            await Task.Delay(waitTime, cancellationToken);
        }

        // Set realistic headers
        request.Headers.UserAgent.ParseAdd("EveCal/1.0 (EVE Calendar Service; +https://github.com/evecal)");
        request.Headers.Accept.ParseAdd("application/json");

        var response = await base.SendAsync(request, cancellationToken);

        // Track rate limits
        if (response.Headers.TryGetValues("X-ESI-Error-Limit-Remain", out var remainValues))
        {
            if (int.TryParse(remainValues.FirstOrDefault(), out var remain))
            {
                _errorLimitRemain = remain;
            }
        }
        if (response.Headers.TryGetValues("X-ESI-Error-Limit-Reset", out var resetValues))
        {
            if (int.TryParse(resetValues.FirstOrDefault(), out var resetSeconds))
            {
                _errorLimitReset = DateTime.UtcNow.AddSeconds(resetSeconds);
            }
        }

        // Retry on 5xx with exponential backoff
        if ((int)response.StatusCode >= 500)
        {
            for (int retry = 1; retry <= 3; retry++)
            {
                var backoff = TimeSpan.FromSeconds(Math.Pow(2, retry));
                logger.LogWarning("ESI returned {Status}, retrying in {Seconds}s (attempt {Retry}/3)",
                    response.StatusCode, backoff.TotalSeconds, retry);

                await Task.Delay(backoff, cancellationToken);
                response = await base.SendAsync(request, cancellationToken);

                if ((int)response.StatusCode < 500) break;
            }
        }

        return response;
    }
}

public static class EsiHttpClientExtensions
{
    public static IServiceCollection AddEsiHttpClient(this IServiceCollection services)
    {
        services.AddTransient<EsiHttpClientHandler>();

        services.AddHttpClient("ESI", client =>
        {
            client.BaseAddress = new Uri("https://esi.evetech.net/");
            client.Timeout = TimeSpan.FromSeconds(30);
        }).AddHttpMessageHandler<EsiHttpClientHandler>();

        services.AddHttpClient("EVEAuth", client =>
        {
            client.BaseAddress = new Uri("https://login.eveonline.com/");
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        return services;
    }
}
