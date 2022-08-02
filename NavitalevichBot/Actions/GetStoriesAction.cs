using NavitalevichBot.Services;
using NavitalevichBot.Models;
using Telegram.Bot;
using NavitalevichBot.Actions.Core;

namespace NavitalevichBot.Actions;

internal class GetStoriesAction : BaseUserAction<GetStoriesAction>, IBotAction
{
    private readonly TelegramInstService _telegramInstService;

    public GetStoriesAction(
        TelegramInstService telegramInstService,
        ITelegramBotClient botClient
    ) : base(botClient)
    {
        _telegramInstService = telegramInstService;
    }

    public async Task<bool> HandleAction(BotActionParams data, CancellationToken cancellationToken = default)
    {
        var updateMessage = data.Update.Message;
        if (updateMessage!.Text != BotActionCommands.getstories) return false;


        var chatId = updateMessage!.Chat!.Id;
        if (!await IsInstAuth(chatId, data.UserStatus, cancellationToken)) return true;

        var result = await _telegramInstService.SendStories(chatId, cancellationToken);
        if (!result)
        {
            await _botClient.SendTextMessageAsync(chatId, "the stories are over", cancellationToken: cancellationToken);
        }

        return true;
    }
}
