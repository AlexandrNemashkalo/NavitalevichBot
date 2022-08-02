using Microsoft.Extensions.Logging;

namespace NavitalevichBot.Helpers;

public static class LoggerExtensions
{
    public static void LogError(this ILogger logger, long chatId, string? message, params object?[] args)
    {
        logger.LogError($"[{chatId}] " + message, args);
    }

    public static void LogInformation(this ILogger logger, long chatId, string? message, params object?[] args)
    {
        logger.LogInformation($"[{chatId}] " + message, args);
    }

    public static void LogDebug(this ILogger logger, long chatId, string? message, params object?[] args)
    {
        logger.LogDebug($"[{chatId}] " + message, args);
    }
}
