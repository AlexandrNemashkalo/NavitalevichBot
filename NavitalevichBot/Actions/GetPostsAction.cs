using NavitalevichBot.Services;
using NavitalevichBot.Models;
using Microsoft.Extensions.Caching.Memory;
using Telegram.Bot;
using NavitalevichBot.Actions.Core;

namespace NavitalevichBot.Actions;
internal class GetPostsAction : BaseUserAction<GetPostsAction>, IBotAction
{
    private readonly TelegramInstService _telegramInstService;
    private readonly InstModuleManager _instModuleManager;

    public GetPostsAction(
        TelegramInstService telegramInstService,
        InstModuleManager instModuleManager,
        ITelegramBotClient botClient
    ) : base(botClient)
    {
        _telegramInstService = telegramInstService;
        _instModuleManager = instModuleManager;
    }

    public async Task<bool> HandleAction(BotActionParams data, CancellationToken cancellationToken = default)
    {
        var updateMessage = data.Update.Message;
        if (updateMessage!.Text != BotActionCommands.getposts) return false;


        var chatId = updateMessage!.Chat!.Id;
        if (!await IsInstAuth(chatId, data.UserStatus, cancellationToken)) return true;

        await SendPosts(chatId, cancellationToken);

        return true;
    }

    public async Task SendPosts(long chatId, CancellationToken cancellationToken)
    {
        var instModule = _instModuleManager.GetInstModule(chatId);

        int currentPage = 1; ;
        if (instModule.Cache.TryGetValue<int>(instModule.GetPageKey(), out var page))
        {
            currentPage = page + 1;
            instModule.SetPage(page + 1);
        }
        else
        {
            instModule.SetPage(1);
        }
        var result = await _telegramInstService.SendPosts(chatId, currentPage, cancellationToken);
        if (!result)
        {
            await _botClient.SendTextMessageAsync(chatId, "the new posts are over", cancellationToken: cancellationToken);
        }
    }
}
