using Microsoft.Extensions.Configuration;
using NavitalevichBot.Actions.Core;
using NavitalevichBot.Models;
using NavitalevichBot.Services;
using Telegram.Bot;

namespace NavitalevichBot.Actions.AdminActions;

internal class SetProxyAction : BaseAdminAction, IBotAction
{
    private readonly ProxyManager _proxyManager;
    private readonly LastUpdatesManager _lastUpdatesManager;

    public SetProxyAction(
        ITelegramBotClient botClient,
        IConfiguration config,
        ProxyManager proxyManager,
        LastUpdatesManager lastUpdatesManager
    ) : base(botClient, config)
    {
        _proxyManager = proxyManager;
        _lastUpdatesManager = lastUpdatesManager;
    }

    public async Task<bool> HandleAction(BotActionParams data, CancellationToken cancellationToken = default)
    {
        var updateMessage = data.Update.Message;
        var chatId = updateMessage!.Chat!.Id;

        if (updateMessage!.Text == BotActionCommands.setproxy)
        {
            if (!await IsAdmin(chatId, cancellationToken)) return true;

            await _botClient.SendTextMessageAsync(chatId, "send me address or \"null\", for example: \nhttp://<host>:<port>", cancellationToken: cancellationToken);

            return true;
        }

        if (_lastUpdatesManager.GetLastUpdateMessage(chatId) == BotActionCommands.setproxy)
        {
            if(updateMessage.Text == "null"){
                updateMessage.Text = null;
            }
            _proxyManager.SetProxy(updateMessage.Text);

            await _botClient.SendTextMessageAsync(chatId, "success", cancellationToken: cancellationToken);

            return true;
        }

        return false;
    }
}