using NavitalevichBot.Models;
using Telegram.Bot;

namespace NavitalevichBot.Actions.Core;

internal abstract class BaseUserAction<T>
{
    public string Name => typeof(T).Name;

    protected readonly ITelegramBotClient _botClient;

    public BaseUserAction(ITelegramBotClient botClient)
    {
        _botClient = botClient;
    }

    protected async Task<bool> IsInstAuth(long chatId, TelegramUserStatus userStatus, CancellationToken cancellationToken)
    {
        if(userStatus != TelegramUserStatus.InstAuth)
        {
            await _botClient.SendTextMessageAsync(chatId, "you need to login to instagram account, send /setuser", cancellationToken: cancellationToken);
            return false;
        }
        if (userStatus == TelegramUserStatus.InstAuth)
        {
            return true;
        }
        return false;
    }
}
