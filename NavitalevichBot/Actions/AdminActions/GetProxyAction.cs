using Microsoft.Extensions.Configuration;
using NavitalevichBot.Actions.Core;
using NavitalevichBot.Models;
using NavitalevichBot.Services;
using Telegram.Bot;

namespace NavitalevichBot.Actions.AdminActions;

internal class GetProxyAction : BaseAdminAction, IBotAction
{
    private readonly ProxyManager _proxyManager;

    public GetProxyAction(
        ITelegramBotClient botClient,
        IConfiguration config,
        ProxyManager proxyManager
    ) : base(botClient, config)
    {
        _proxyManager = proxyManager;
    }

    public async Task<bool> HandleAction(BotActionParams data, CancellationToken cancellationToken = default)
    {
        var updateMessage = data.Update.Message;
        var chatId = updateMessage!.Chat!.Id;

        if (updateMessage!.Text == BotActionCommands.getproxy)
        {
            if (!await IsAdmin(chatId, cancellationToken)) return true;

            var proxy = _proxyManager.CurrentProxy;
            await _botClient.SendTextMessageAsync(chatId, $"current proxy:\n{proxy?.Address}", cancellationToken: cancellationToken);

            return true;
        }

        return false;
    }
}
