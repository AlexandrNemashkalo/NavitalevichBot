using NavitalevichBot.Actions.Core;
using NavitalevichBot.Data;
using NavitalevichBot.Models;
using Telegram.Bot;

namespace NavitalevichBot.Actions;

internal class LogoutUserAction : BaseUserAction<LogoutUserAction>, IBotAction
{
    private readonly IStorageContext _dbContext;
    private readonly InstModuleManager _instModuleManager;

    public LogoutUserAction(
        ITelegramBotClient botClient,
        IStorageContext dbContext,
        InstModuleManager instModuleManager
    ) : base(botClient) 
    {
        _dbContext = dbContext;
        _instModuleManager = instModuleManager;
    }

    public async Task<bool> HandleAction(BotActionParams data, CancellationToken cancellationToken = default)
    {
        var updateMessage = data.Update.Message;
        if (updateMessage!.Text != BotActionCommands.logout) return false;


        var chatId = updateMessage!.Chat!.Id;
        if (!await IsInstAuth(chatId, data.UserStatus, cancellationToken)) return true;

        if (data.UserStatus != TelegramUserStatus.InstAuth)
        {
            await _botClient.SendTextMessageAsync(chatId, "you are not logged in", cancellationToken: cancellationToken);
            return true;
        }

        await _dbContext.DeleteSessionMessage(chatId);
        _instModuleManager.DeleteInstModule(chatId);

        await _botClient.SendTextMessageAsync(chatId, "success", cancellationToken: cancellationToken);
        
        return true;
    }
}
