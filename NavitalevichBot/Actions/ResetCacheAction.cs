using NavitalevichBot.Actions.Core;
using NavitalevichBot.Models;
using Telegram.Bot;

namespace NavitalevichBot.Actions;

internal class ResetCacheAction : BaseUserAction<ResetCacheAction>, IBotAction
{
    private InstModuleManager _instModuleManager;

    public ResetCacheAction(
        InstModuleManager instModuleManager,
        ITelegramBotClient botClient
    ) : base(botClient)
    {
        _instModuleManager = instModuleManager;
    }

    public async Task<bool> HandleAction(BotActionParams data, CancellationToken cancellationToken = default)
    {
        var updateMessage = data.Update.Message;
        if (updateMessage!.Text != BotActionCommands.resetcache) return false;


        var chatId = updateMessage!.Chat!.Id;
        if (!await IsInstAuth(chatId, data.UserStatus, cancellationToken)) return true;

        var instModule = _instModuleManager.GetInstModule(chatId);

        await instModule.ResetCache(cancellationToken);
        await _botClient.SendTextMessageAsync(chatId, "success reset ceche", cancellationToken: cancellationToken);

        return true;
    }
}
