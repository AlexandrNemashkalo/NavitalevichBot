using Microsoft.Extensions.Logging;
using NavitalevichBot.Actions;
using NavitalevichBot.Actions.Core;
using NavitalevichBot.Data;
using NavitalevichBot.Helpers;
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
    private readonly ILogger _logger;

    public Application(
        IStorageInitializer storageInitializer,
        ITelegramBotClient botClient,
        IEnumerable<IBotAction> botActions,
        LastUpdatesManager lastUpdatesManager,
        AuthService authService,
        ExceptionHandler exceptionHandler,
        ILoggerFactory loggerFactory
    )
    {
        _storageInitializer = storageInitializer;
        _botClient = botClient;
        _botActions = botActions;
        _lastUpdatesManager = lastUpdatesManager;
        _authService = authService;
        _exceptionHandler = exceptionHandler;
        _logger = loggerFactory.CreateLogger<Application>();
    }

    public async Task Run(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Run");
        await _storageInitializer.InitializeStorage();
        _logger.LogDebug("База данны инициализирована");

        await _botClient.SetMyCommandsAsync(BotActionCommands.GetUserBotCommands());

        var receiverOptions = new ReceiverOptions{ AllowedUpdates = { } };

        _botClient.StartReceiving(
            HandleUpdateAsync,
            HandleErrorAsync,
            receiverOptions,
            cancellationToken: cancellationToken);

        _logger.LogInformation("Бот запущен!");
    }

    public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        var chatId = update?.Message?.Chat?.Id;

        if (chatId == null ) return;
        if (update?.Type != UpdateType.Message || update?.Message?.Type != MessageType.Text)
        {
            await botClient.SendTextMessageAsync(chatId, $"unsupported message type", cancellationToken: cancellationToken);
            return;
        }

        var messageText = update.Message.Text;
        _logger.LogDebug(chatId.Value, $"Получено сообщение: \"{messageText}\"");
        try
        {
            var userStatus = await _authService.GetUserStatusAndTryLogin(chatId.Value, cancellationToken);
            _logger.LogDebug(chatId.Value, "UserStatus: " + userStatus.ToString());

            if (userStatus == TelegramUserStatus.NotAvaliable)
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
                    _logger.LogDebug(chatId.Value, $"Комманда \"{messageText}\" обработана {botAction.Name}");
                    break;
                }
            }

            if (!isMatch)
            {
                _logger.LogDebug(chatId.Value, $"Команда \"{messageText}\" не была обработана");
                await _botClient.SendTextMessageAsync(chatId, $"command \"{messageText}\" not found", cancellationToken: cancellationToken);
            }
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

        _logger.LogError(errorMessage);
        return Task.CompletedTask;
    }
}
