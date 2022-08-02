using NavitalevichBot.Actions.Core;
using NavitalevichBot.Data;
using NavitalevichBot.Models;
using Telegram.Bot;

namespace NavitalevichBot.Actions;

internal class GetInfoAction : BaseUserAction<GetInfoAction>, IBotAction
{
    private readonly InstModuleManager _instModuleManager;
    private readonly IStorageContext _dbContext;

    public GetInfoAction(
        InstModuleManager instModuleManager,
        ITelegramBotClient botClient,
        IStorageContext dbContext
    ) : base(botClient)
    {
        _instModuleManager = instModuleManager;
        _dbContext = dbContext;
    }

    public async Task<bool> HandleAction(BotActionParams data, CancellationToken cancellationToken = default)
    {
        var updateMessage = data.Update.Message;
        if (updateMessage!.Text != BotActionCommands.getinfo) return false;


        var chatId = updateMessage!.Chat!.Id;
        if (!await IsInstAuth(chatId, data.UserStatus, cancellationToken)) return true;

        var instModule = _instModuleManager.GetInstModule(chatId);
        var settings = instModule.Settings;
        var blackList = (await _dbContext.GetBlackList(chatId)).ToHashSet();

        var message = ""
            + $"⚙️ Bot settings:\n"
            + $"get stories: {settings.IsGetStories}\n"
            + $"period: {settings.StoryPeriodHours} h\n"
            + $"\n"
            + $"get posts: {settings.IsGetPosts}\n"
            + $"period: {settings.PostsPeriodHours} h\n"
            + $"page: {settings.PostsCountPage}\n"
            + $"\n"
            + $"🚫 Black list:\n"
            + $"{string.Join(", ", blackList)}";
        await _botClient.SendTextMessageAsync(chatId, message, cancellationToken: cancellationToken);

        return true;
    }
}
