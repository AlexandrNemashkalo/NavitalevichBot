using Microsoft.Extensions.Configuration;
using NavitalevichBot.Models;
using Telegram.Bot;

namespace NavitalevichBot.Actions.Core;

internal abstract class BaseAdminAction<T> : BaseUserAction<T>
{
    protected readonly IConfiguration _config;

    public BaseAdminAction(
        ITelegramBotClient botClient,
        IConfiguration config
    ) : base(botClient)
    {
        _config = config;
    }

    protected async Task<bool> IsAdmin(long chatId, CancellationToken cancellationToken)
    {
        if (chatId != long.Parse(_config.GetSection("AdminChatId").Value))
        {
            await _botClient.SendTextMessageAsync(chatId, "sorry, you is not admin", cancellationToken: cancellationToken);
            return false;
        }
        
        return true;
    }
}
