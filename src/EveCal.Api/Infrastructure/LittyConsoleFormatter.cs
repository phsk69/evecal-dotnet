using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;

namespace EveCal.Api.Infrastructure;

/// <summary>
/// formatter options, inherits the good stuff from ConsoleFormatterOptions no cap
/// </summary>
public class LittyConsoleFormatterOptions : ConsoleFormatterOptions;

/// <summary>
/// custom console formatter that makes ALL logs bussin with emojis and ANSI colors fr fr 🔥
/// </summary>
public sealed class LittyConsoleFormatter(IOptionsMonitor<LittyConsoleFormatterOptions> options)
    : ConsoleFormatter("litty")
{
    // ANSI escape codes that hit different 🎨
    private const string Reset = "\x1b[0m";
    private const string Dim = "\x1b[2m";
    private const string Cyan = "\x1b[36m";
    private const string Blue = "\x1b[34m";
    private const string Green = "\x1b[32m";
    private const string Yellow = "\x1b[33m";
    private const string Red = "\x1b[31m";
    private const string BrightRed = "\x1b[91m";

    public override void Write<TState>(
        in LogEntry<TState> logEntry,
        IExternalScopeProvider? scopeProvider,
        TextWriter textWriter)
    {
        var message = logEntry.Formatter?.Invoke(logEntry.State, logEntry.Exception);
        if (message is null && logEntry.Exception is null)
            return;

        var (emoji, levelLabel, color) = logEntry.LogLevel switch
        {
            LogLevel.Trace       => ("👀", "TRACE", Cyan),
            LogLevel.Debug       => ("🔍", "DBG",   Blue),
            LogLevel.Information => ("🔥", "INFO",  Green),
            LogLevel.Warning     => ("😤", "WARN",  Yellow),
            LogLevel.Error       => ("💀", "ERR",   Red),
            LogLevel.Critical    => ("☠️",  "CRIT",  BrightRed),
            _                    => ("❓", "???",   Reset)
        };

        // short category name - yeet the namespace bloat
        var category = logEntry.Category;
        var shortCategory = category.Contains('.')
            ? category[(category.LastIndexOf('.') + 1)..]
            : category;

        // timestamp stays dim so it don't steal the spotlight
        var opts = options.CurrentValue;
        var now = opts.UseUtcTimestamp ? DateTimeOffset.UtcNow : DateTimeOffset.Now;
        var timestampFmt = opts.TimestampFormat ?? "HH:mm:ss";
        var timestamp = now.ToString(timestampFmt);

        // the main event - colored prefix, dim metadata, clean message
        textWriter.Write($"{color}[{emoji} {levelLabel}]{Reset} ");
        textWriter.Write($"{Dim}[{timestamp}] [{shortCategory}]{Reset} ");
        textWriter.Write(message);

        // exception deets on next line if they exist
        if (logEntry.Exception is not null)
        {
            textWriter.WriteLine();
            textWriter.Write($"{Red}  {logEntry.Exception}{Reset}");
        }

        textWriter.WriteLine();
    }
}
