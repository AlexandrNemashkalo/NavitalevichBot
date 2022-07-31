using System.Text.Json;
using Telegram.Bot;
using NavitalevichBot.Models;
using NavitalevichBot.Services;
using FluentScheduler;
using NavitalevichBot.Exceptions;
using NavitalevichBot.Helpers;
using NavitalevichBot.Actions.Core;

namespace NavitalevichBot.Actions;

internal class SetUserAction :BaseUserAction, IBotAction
{
    private readonly InstModuleManager _instManager;
    private readonly LastUpdatesManager _lastUpdatesManager;
    private readonly IInstSessionHandlerManager _instSessionHandlerManager;
    private readonly InstClientFactory _instClientFactory;
    private readonly TelegramInstService _telegramInstService;

    public SetUserAction(
        InstModuleManager instManager,
        LastUpdatesManager lastUpdatesManager,
        ITelegramBotClient botClient,
        IInstSessionHandlerManager instSessionHandlerManager,
        InstClientFactory instClientFactory,
        TelegramInstService telegramInstService
    ) : base(botClient)
    {
        _instManager = instManager;
        _lastUpdatesManager = lastUpdatesManager;
        _instSessionHandlerManager = instSessionHandlerManager;
        _instClientFactory = instClientFactory;
        _telegramInstService = telegramInstService;
    }

    public async Task<bool> HandleAction(BotActionParams data, CancellationToken cancellationToken = default)
    {
        var updateMessage = data.Update.Message;
        var messageText = updateMessage!.Text;
        var chatId = updateMessage!.Chat!.Id;

        if (messageText == BotActionCommands.setuser)
        {
            if (!EnvironmentHelper.VPNIsOn())
            {
                await _botClient.SendTextMessageAsync(chatId, "authorization is not supported at the moment, contact the administrator (@navitalevich)", cancellationToken: cancellationToken);
                return true;
            }

            if (data.UserStatus == TelegramUserStatus.InstAuth)
            {
                await _botClient.SendTextMessageAsync(chatId, "you are already logged in ", cancellationToken: cancellationToken);
                return true;
            }

            var userdata = new UserData { UserName = "example", Password = "1234" };
            var options = new JsonSerializerOptions { WriteIndented = true };

            await _botClient.SendTextMessageAsync(chatId, $"send me your userdata by format:", cancellationToken: cancellationToken);
            await _botClient.SendTextMessageAsync(chatId, JsonSerializer.Serialize(userdata, options), cancellationToken: cancellationToken);

            return true;
        }

        if (_lastUpdatesManager.GetLastUpdateMessage(chatId) == BotActionCommands.setuser)
        {
            UserData userData = null;
            try
            {
                userData = JsonSerializer.Deserialize<UserData>(messageText);
                if (userData == null)
                {
                    throw new Exception();
                }
            }
            catch (Exception ex)
            {
                throw new UncorrectDataException(messageText);
            }

            var instSessionHandler = _instSessionHandlerManager.CreateSessionHandler(chatId);

            var instaApi = await _instClientFactory.CreateAndLoginInstClient(userData.UserName, userData.Password, instSessionHandler);
            if (instaApi == null)
            {
                throw new InstException("😢 authentication error, try disabling two-factor authentication and confirm the current address in the app");
            }

            var instModule = _instManager.CreateInstModule(instaApi, chatId, cancellationToken);
            SheduleInstModule(instModule, cancellationToken);

            await _botClient.SendTextMessageAsync(chatId, "success", cancellationToken: cancellationToken);
            return true;
        }

        return false;
    }

    private void SheduleInstModule(InstModule instModule, CancellationToken cancellationToken = default)
    {
        instModule.ScheduleSendPosts(_telegramInstService.SendPosts, cancellationToken);
        instModule.ScheduleSendStories(_telegramInstService.SendStories, cancellationToken);
        JobManager.Initialize(instModule);
    }
}

