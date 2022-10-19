using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NuGet.Common;
using ILogger = NuGet.Common.ILogger;
using LogLevel = NuGet.Common.LogLevel;

namespace NugetVendor;

public class LoggerAdapter : ILogger
{
    private readonly Microsoft.Extensions.Logging.ILogger _inner;

    public LoggerAdapter(Microsoft.Extensions.Logging.ILogger inner)
    {
        _inner = inner;
    }

    public void LogDebug(string data)
    {
        _inner.LogDebug(data);
    }

    public void LogVerbose(string data)
    {
        _inner.LogTrace(data);
    }

    public void LogInformation(string data)
    {
        _inner.LogInformation(data);
    }

    public void LogMinimal(string data)
    {
        _inner.LogInformation(data);
    }

    public void LogWarning(string data)
    {
        _inner.LogWarning(data);
    }

    public void LogError(string data)
    {
        _inner.LogError(data);
    }

    public void LogInformationSummary(string data)
    {
        _inner.LogInformation(data);
    }

    public void Log(LogLevel level, string data)
    {
        switch (level)
        {
            case LogLevel.Debug:
                _inner.LogDebug(data);
                break;
            case LogLevel.Verbose:
                _inner.LogTrace(data);
                break;
            case LogLevel.Information:
                _inner.LogInformation(data);
                break;
            case LogLevel.Minimal:
                _inner.LogInformation(data);
                break;
            case LogLevel.Warning:
                _inner.LogWarning(data);
                break;
            case LogLevel.Error:
                _inner.LogError(data);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(level), level, null);
        }
    }

    public Task LogAsync(LogLevel level, string data)
    {
        Log(level, data);
        return Task.CompletedTask;
    }

    public void Log(ILogMessage message)
    {
        Microsoft.Extensions.Logging.LogLevel innerLevel;

        switch (message.Level)
        {
            case LogLevel.Debug:
                innerLevel = Microsoft.Extensions.Logging.LogLevel.Debug;
                break;
            case LogLevel.Verbose:
                innerLevel = Microsoft.Extensions.Logging.LogLevel.Trace;
                break;
            case LogLevel.Information:
                innerLevel = Microsoft.Extensions.Logging.LogLevel.Information;
                break;
            case LogLevel.Minimal:
                innerLevel = Microsoft.Extensions.Logging.LogLevel.Information;
                break;
            case LogLevel.Warning:
                innerLevel = Microsoft.Extensions.Logging.LogLevel.Warning;
                break;
            case LogLevel.Error:
                innerLevel = Microsoft.Extensions.Logging.LogLevel.Error;
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        _inner.Log(innerLevel, message.Message);
    }

    public Task LogAsync(ILogMessage message)
    {
        Log(message);
        return Task.CompletedTask;
    }
}