using System.Collections.Concurrent;

namespace NavitalevichBot;

public class LastUpdatesManager
{
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
        _lastUpdatesDict[chatId] = updateMessage;
    }
}
