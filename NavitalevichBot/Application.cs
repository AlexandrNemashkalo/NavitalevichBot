using NavitalevichBot.Actions;
using NavitalevichBot.Actions.Core;
using NavitalevichBot.Data;
using NavitalevichBot.Models;
using NavitalevichBot.Services;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Extensions.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace NavitalevichBot;

internal class Application
{
    private readonly IStorageInitializer _storageInitializer;
    private readonly ITelegramBotClient _botClient;
    private readonly IEnumerable<IBotAction> _botActions;
    private readonly LastUpdatesManager _lastUpdatesManager;
    private readonly AuthService _authService;
    private readonly ExceptionHandler _exceptionHandler;

    public Application(
        IStorageInitializer storageInitializer,
        ITelegramBotClient botClient,
        IEnumerable<IBotAction> botActions,
        LastUpdatesManager lastUpdatesManager,
        AuthService authService,
        ExceptionHandler exceptionHandler
    )
    {
        _storageInitializer = storageInitializer;
        _botClient = botClient;
        _botActions = botActions;
        _lastUpdatesManager = lastUpdatesManager;
        _authService = authService;
        _exceptionHandler = exceptionHandler;
    }

    public async Task Run(CancellationToken cancellationToken = default)
    {
        await _storageInitializer.InitializeStorage();

        await _botClient.SetMyCommandsAsync(BotActionCommands.GetUserBotCommands());

        var receiverOptions = new ReceiverOptions{ AllowedUpdates = { } };

        _botClient.StartReceiving(
            HandleUpdateAsync,
            HandleErrorAsync,
            receiverOptions,
            cancellationToken: cancellationToken);
    }

    public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        var chatId = update?.Message?.Chat?.Id;
        Console.WriteLine($"{chatId} {DateTime.Now}");

        if (chatId == null ) return;
        if (update?.Type != UpdateType.Message || update?.Message?.Type != MessageType.Text)
        {
            await botClient.SendTextMessageAsync(chatId, $"unsupported message type", cancellationToken: cancellationToken);
            return;
        }


        var messageText = update.Message.Text;
        try
        {
            var userStatus = await _authService.GetUserStatusAndTryLogin(chatId.Value, cancellationToken);

            if(userStatus == TelegramUserStatus.NotAvaliable)
            {
                await botClient.SendTextMessageAsync(chatId, $"😩 sorry access blocked \n contact the bot admin (@navitalevich) and tell him your chatId:{chatId}", cancellationToken: cancellationToken);
                return;
            }

            var actionParams = new BotActionParams
            {
                Update = update,
                UserStatus = userStatus
            };

            var isMatch = false;
            foreach (var botAction in _botActions)
            {
                if (await botAction.HandleAction(actionParams, cancellationToken))
                {
                    isMatch = true;
                    break;
                }
            }

            if (!isMatch)
            {
                await _botClient.SendTextMessageAsync(chatId, $"command \"{messageText}\" not found", cancellationToken: cancellationToken);
            }

            await Task.Delay(100000);
        }
        catch (Exception ex)
        {
            await _exceptionHandler.HandleException(chatId.Value, ex);
        }
        finally
        {
            _lastUpdatesManager.SetLastUpdate(chatId.Value, messageText!);
        }
    }

    public Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        string errorMessage = exception is ApiRequestException apiRequestException
            ? $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}"
            : exception.ToString();

        Console.WriteLine($"{errorMessage}  {DateTime.Now}");
        return Task.CompletedTask;
    }
}
