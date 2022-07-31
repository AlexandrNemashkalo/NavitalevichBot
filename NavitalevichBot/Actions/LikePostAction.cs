using NavitalevichBot.Services;
using NavitalevichBot.Models;
using Telegram.Bot;
using NavitalevichBot.Actions.Core;

namespace NavitalevichBot.Actions;

internal class LikePostAction : BaseUserAction, IBotAction
{
    private readonly TelegramInstService _telegramInstService;

    public LikePostAction(TelegramInstService telegramInstService, ITelegramBotClient botClient)
        : base(botClient)
    {
        _telegramInstService = telegramInstService;
    }

    public async Task<bool> HandleAction(BotActionParams data, CancellationToken cancellationToken = default)
    {
        var updateMessage = data.Update.Message;
        if (updateMessage!.Text != BotActionCommands.like) return false;


        var chatId = updateMessage!.Chat!.Id;
        if (!await IsInstAuth(chatId, data.UserStatus, cancellationToken)) return true;

        var messageId = updateMessage?.ReplyToMessage?.MessageId;

        await _telegramInstService.LikeMedia(chatId, messageId, cancellationToken);

        return true;
    }
}
