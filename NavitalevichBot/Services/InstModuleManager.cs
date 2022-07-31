using InstagramApiSharp.API;
using NavitalevichBot.Services;
using System.Collections.Concurrent;

namespace NavitalevichBot;

public class InstModuleManager
{
    private ConcurrentDictionary<long, InstModule> InstModuleDict = new ConcurrentDictionary<long, InstModule>();

    private readonly ExceptionHandler _exceptionHandler;

    public InstModuleManager(ExceptionHandler exceptionHandler)
    {
        _exceptionHandler = exceptionHandler;
    }

    public InstModule GetInstModule(long chatId)
    {
        if (InstModuleDict.ContainsKey(chatId))
        {
            return InstModuleDict[chatId];
        }
        return null;
    }

    public void SetInstModule(InstModule instModule)
    {
        InstModuleDict[instModule.ChatId] = instModule;
    }

    public void DeleteInstModule(long chatId)
    {
        InstModuleDict.Remove(chatId, out var instModule);
    }

    public InstModule CreateInstModule(IInstaApi instClient, long chatId, CancellationToken cancellationToken = default)
    {
        var instModule = new InstModule(instClient, chatId, _exceptionHandler, cancellationToken);

        SetInstModule(instModule);

        return instModule;
    }
}