using Microsoft.Extensions.Logging;
using NavitalevichBot.Helpers;
using System.Collections.Concurrent;

namespace NavitalevichBot;

public class LastUpdatesManager
{
    private readonly ILogger _logger;

    public LastUpdatesManager(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<LastUpdatesManager>();
    }
    ConcurrentDictionary<long, string> _lastUpdatesDict = new ConcurrentDictionary<long, string>();

    public string GetLastUpdateMessage(long chatId)
    {
        if (_lastUpdatesDict.ContainsKey(chatId))
        {
            return _lastUpdatesDict[chatId];
        }
        return null;
    }

    public void SetLastUpdate(long chatId, string updateMessage)
    {
        _lastUpdatesDict.AddOrUpdate(chatId, updateMessage, (x, v) => updateMessage);
        _logger.LogDebug(chatId, $"Установили \"{updateMessage}\" как последнее");
    }
}
