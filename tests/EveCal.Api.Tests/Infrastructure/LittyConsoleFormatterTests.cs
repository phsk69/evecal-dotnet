using EveCal.Api.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace EveCal.Api.Tests.Infrastructure;

/// <summary>
/// testing that the litty formatter absolutely slaps fr fr 🔥
/// </summary>
public class LittyConsoleFormatterTests
{
    private static LittyConsoleFormatter CreateFormatter(LittyConsoleFormatterOptions? opts = null)
    {
        var monitor = new Mock<IOptionsMonitor<LittyConsoleFormatterOptions>>();
        monitor.Setup(m => m.CurrentValue).Returns(opts ?? new LittyConsoleFormatterOptions());
        return new LittyConsoleFormatter(monitor.Object);
    }

    private static string RenderLog(
        LittyConsoleFormatter formatter,
        LogLevel level,
        string category,
        string message,
        Exception? exception = null)
    {
        var writer = new StringWriter();
        var entry = new LogEntry<string>(
            level,
            category,
            new EventId(0),
            message,
            exception,
            (state, _) => state);

        formatter.Write(in entry, null, writer);
        return writer.ToString();
    }

    [Theory]
    [InlineData(LogLevel.Trace, "👀")]
    [InlineData(LogLevel.Debug, "🔍")]
    [InlineData(LogLevel.Information, "🔥")]
    [InlineData(LogLevel.Warning, "😤")]
    [InlineData(LogLevel.Error, "💀")]
    [InlineData(LogLevel.Critical, "☠️")]
    public void Write_EmitsCorrectEmojiForLogLevel(LogLevel level, string expectedEmoji)
    {
        // the emoji game gotta be on point no cap
        var formatter = CreateFormatter();
        var output = RenderLog(formatter, level, "TestCategory", "test message");
        Assert.Contains(expectedEmoji, output);
    }

    [Theory]
    [InlineData(LogLevel.Trace, "TRACE")]
    [InlineData(LogLevel.Debug, "DBG")]
    [InlineData(LogLevel.Information, "INFO")]
    [InlineData(LogLevel.Warning, "WARN")]
    [InlineData(LogLevel.Error, "ERR")]
    [InlineData(LogLevel.Critical, "CRIT")]
    public void Write_EmitsCorrectLevelLabel(LogLevel level, string expectedLabel)
    {
        // level labels gotta be concise bestie
        var formatter = CreateFormatter();
        var output = RenderLog(formatter, level, "TestCategory", "test message");
        Assert.Contains(expectedLabel, output);
    }

    [Fact]
    public void Write_ShortensCategory_ToLastSegment()
    {
        // yeeting the namespace bloat fr
        var formatter = CreateFormatter();
        var output = RenderLog(
            formatter, LogLevel.Information,
            "Microsoft.Hosting.Lifetime",
            "Application started");

        Assert.Contains("[Lifetime]", output);
        Assert.DoesNotContain("Microsoft.Hosting", output);
    }

    [Fact]
    public void Write_KeepsCategoryWhenNoNamespace()
    {
        // no dots = keep the whole thing bestie
        var formatter = CreateFormatter();
        var output = RenderLog(
            formatter, LogLevel.Information,
            "Program",
            "starting up");

        Assert.Contains("[Program]", output);
    }

    [Fact]
    public void Write_IncludesExceptionDetails()
    {
        // exceptions gotta show up so we can debug fr
        var formatter = CreateFormatter();
        var ex = new InvalidOperationException("something flopped hard bestie");
        var output = RenderLog(
            formatter, LogLevel.Error,
            "TestCategory",
            "log message",
            ex);

        Assert.Contains("something flopped hard bestie", output);
    }

    [Fact]
    public void Write_PreservesExistingEmojisInMessage()
    {
        // custom app messages already have emojis, formatter shouldn't mess with them
        var formatter = CreateFormatter();
        var output = RenderLog(
            formatter, LogLevel.Information,
            "TestCategory",
            "🔥 ICS feed generated, absolutely ate");

        // both the formatter-level emoji and the in-message emoji should be there
        Assert.Contains("🔥 INFO", output);  // formatter prefix
        Assert.Contains("🔥 ICS feed generated", output);  // message emoji preserved
    }

    [Fact]
    public void Write_IncludesTimestamp()
    {
        // gotta know when things happened bestie
        var formatter = CreateFormatter();
        var output = RenderLog(
            formatter, LogLevel.Information,
            "TestCategory",
            "test message");

        // timestamp should be in HH:mm:ss format (default)
        Assert.Matches(@"\[\d{2}:\d{2}:\d{2}\]", output);
    }

    [Fact]
    public void Write_SkipsNullMessage_WhenNoException()
    {
        // no message + no exception = no output, we don't do empty lines
        var formatter = CreateFormatter();
        var writer = new StringWriter();
        var entry = new LogEntry<string>(
            LogLevel.Information,
            "TestCategory",
            new EventId(0),
            "ignored",
            null,
            (_, _) => null!);

        formatter.Write(in entry, null, writer);
        Assert.Empty(writer.ToString());
    }

    [Fact]
    public void Write_IncludesAnsiColorCodes()
    {
        // colors gotta hit different in the terminal 🎨
        var formatter = CreateFormatter();
        var output = RenderLog(
            formatter, LogLevel.Error,
            "TestCategory",
            "error message");

        // should contain ANSI escape codes (red for errors)
        Assert.Contains("\x1b[", output);
        // should contain reset code
        Assert.Contains("\x1b[0m", output);
    }
}
