using Microsoft.Extensions.Logging;
using Spectre.Console;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;

namespace QuestingBots.Server.Internal;

// Copied from https://github.com/sp-tarkov/server-csharp/blob/main/Testing/UnitTests/Mock/MockLogger.cs

[Injectable]
public class MockLogger<T> : ISptLogger<T>, ILogger<T>
{
    public void LogWithColor(string data, Color? textColor = null, Color? backgroundColor = null, Exception? ex = null)
    {
        Console.WriteLine(data);
    }

    public void Success(string data, Exception? ex = null)
    {
        Console.WriteLine(data);
    }

    public void Error(string data, Exception? ex = null)
    {
        Console.WriteLine(data);
    }

    public void Warning(string data, Exception? ex = null)
    {
        Console.WriteLine(data);
    }

    public void Info(string data, Exception? ex = null)
    {
        Console.WriteLine(data);
    }

    public void Debug(string data, Exception? ex = null)
    {
        Console.WriteLine(data);
    }

    public void Critical(string data, Exception? ex = null)
    {
        Console.WriteLine(data);
    }

    public void Log(
        LogLevel level,
        string data,
        Color? textColor = null,
        Color? backgroundColor = null,
        Exception? ex = null
    )
    {
        throw new NotImplementedException();
    }

    public void WriteToLogFile(string body, LogLevel level = LogLevel.Information)
    {
        throw new NotImplementedException();
    }

    public bool IsLogEnabled(LogLevel level)
    {
        return true;
    }

    public void DumpAndStop()
    {
        throw new NotImplementedException();
    }

    public void LogWithColor(string data, Exception? ex = null, Color? textColor = null, Color? backgroundColor = null)
    {
        Console.WriteLine(data);
    }

    public void WriteToLogFile(object body)
    {
        Console.WriteLine(body);
    }

    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull
    {
        return null;
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return true;
    }

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter
    )
    {
        ArgumentNullException.ThrowIfNull(formatter);

        var message = formatter(state, exception);

        Console.WriteLine($"[{logLevel}] {message}");

        if (exception != null)
        {
            Console.WriteLine(exception);
        }
    }
}