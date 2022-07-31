using FluentScheduler;
using Microsoft.Extensions.Configuration;
using NavitalevichBot.Data;
using NavitalevichBot.Models;

namespace NavitalevichBot.Services;

public class AuthService
{
    private readonly IStorageContext _dbContext;
    private readonly InstModuleManager _instManager;
    private readonly IInstSessionHandlerManager _instSessionHandlerManager;
    private readonly InstClientFactory _instClientFactory;
    private readonly TelegramInstService _telegramInstService;
    private readonly IConfiguration _config;

    public AuthService(
        IStorageContext dbContext, 
        InstModuleManager instManager,
        IInstSessionHandlerManager instSessionHandlerManager,
        InstClientFactory instClientFactory,
        TelegramInstService telegramInstService,
        IConfiguration config
    )
    {
        _dbContext = dbContext;
        _instManager = instManager;
        _instSessionHandlerManager = instSessionHandlerManager;
        _instClientFactory = instClientFactory;
        _telegramInstService = telegramInstService;
        _config = config;
    }

    public async Task<TelegramUserStatus> GetUserStatusAndTryLogin(long chatId, CancellationToken cancellationToken)
    {
        if (!await _dbContext.IsAvaliableChatId(chatId) && chatId != long.Parse(_config.GetSection("AdminChatId").Value))
        {
            return TelegramUserStatus.NotAvaliable;
        }

        var sessionMessageId = await _dbContext.GetSessionMessage(chatId);
        var instModule = _instManager.GetInstModule(chatId);

        if ( sessionMessageId == null)
        {
            return TelegramUserStatus.NotInstAuth;
        }
        if(sessionMessageId != null && instModule != null)
        {
            return TelegramUserStatus.InstAuth;
        }

        var instSessionHandler = _instSessionHandlerManager.CreateSessionHandler(chatId);
        var stateData = instSessionHandler.GetStateData();

        if (stateData?.UserSession?.UserName == null || stateData?.UserSession?.Password == null)
        {
            return TelegramUserStatus.NotInstAuth;
        }

        var instClient = await _instClientFactory.CreateAndLoginInstClient(stateData.UserSession.UserName, stateData.UserSession.Password, instSessionHandler);
        if (instClient != null)
        {
            var newInstModule = _instManager.CreateInstModule(instClient, chatId, cancellationToken);
            SheduleInstModule(newInstModule, cancellationToken);
            return TelegramUserStatus.InstAuth;
        }

        return TelegramUserStatus.NotInstAuth;
    }

    private void SheduleInstModule(InstModule instModule, CancellationToken cancellationToken = default)
    {
        instModule.ScheduleSendPosts(_telegramInstService.SendPosts, cancellationToken);
        instModule.ScheduleSendStories(_telegramInstService.SendStories, cancellationToken);
        JobManager.Initialize(instModule);
    }
}
