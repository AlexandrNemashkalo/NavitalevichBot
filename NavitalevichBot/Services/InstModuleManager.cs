using InstagramApiSharp.API;
using Microsoft.Extensions.Logging;
using NavitalevichBot.Helpers;
using NavitalevichBot.Services;
using System.Collections.Concurrent;

namespace NavitalevichBot;

public class InstModuleManager
{
    private ConcurrentDictionary<long, InstModule> InstModuleDict = new ConcurrentDictionary<long, InstModule>();

    private readonly ExceptionHandler _exceptionHandler;
    private readonly ILogger _logger;

    public InstModuleManager(
        ExceptionHandler exceptionHandler,
        ILoggerFactory loggerFactory
    )
    {
        _exceptionHandler = exceptionHandler;
        _logger = loggerFactory.CreateLogger<InstModuleManager>();
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

    public void ScheduleSendPosts(InstModule instModule, Func<long, int, CancellationToken, Task> sendPostsAction, CancellationToken cancellationToken)
    {
        instModule.Schedule(async () =>
        {
            try
            {
                if (instModule.Settings.IsGetPosts)
                {
                    await sendPostsAction(instModule.ChatId, instModule.Settings.PostsCountPage, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(instModule.ChatId, ex.Message);
            }
        })
            .WithName("SendPosts")
            .ToRunNow()
            .AndEvery(instModule.Settings.PostsPeriodHours).Hours();
    }

    public void ScheduleSendStories(InstModule instModule, Func<long, CancellationToken, Task> sendStoriesAction, CancellationToken cancellationToken)
    {
        instModule.Schedule(async () =>
        {
            try
            {
                if (instModule.Settings.IsGetStories)
                {
                    await sendStoriesAction(instModule.ChatId, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(instModule.ChatId, ex.Message);
            }
        })
            .WithName("SendStories")
            .ToRunNow()
            .AndEvery(instModule.Settings.StoryPeriodHours).Hours();
    }

    public InstModule CreateInstModule(IInstaApi instClient, long chatId, CancellationToken cancellationToken = default)
    {
        var instModule = new InstModule(instClient, chatId, _exceptionHandler, cancellationToken);

        SetInstModule(instModule);

        return instModule;
    }
}