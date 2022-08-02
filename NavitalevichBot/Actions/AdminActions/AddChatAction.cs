using Microsoft.Extensions.Configuration;
using NavitalevichBot.Actions.Core;
using NavitalevichBot.Data;
using NavitalevichBot.Models;
using Telegram.Bot;

namespace NavitalevichBot.Actions.AdminActions;

internal class AddChatAction : BaseAdminAction<AddChatAction>, IBotAction
{
    private readonly IStorageContext _dbContext;
    private readonly LastUpdatesManager _lastUpdatesManager;

    public AddChatAction(
        LastUpdatesManager lastUpdatesManager,
        ITelegramBotClient botClient,
        IStorageContext dbContext,
        IConfiguration config
    ) : base(botClient, config)
    {
        _lastUpdatesManager = lastUpdatesManager;
        _dbContext = dbContext;
    }

    public async Task<bool> HandleAction(BotActionParams data, CancellationToken cancellationToken = default)
    {
        var updateMessage = data.Update.Message;
        var chatId = updateMessage!.Chat!.Id;

        if (updateMessage!.Text == BotActionCommands.addchat)
        {
            if (!await IsAdmin(chatId, cancellationToken)) return true;

            await PreAddChatId(cancellationToken);
            return true;
        }

        if (_lastUpdatesManager.GetLastUpdateMessage(chatId) == BotActionCommands.addchat)
        {
            var messageText = updateMessage!.Text!;
            await AddChatId(messageText, cancellationToken);
            return true;
        }

        return false;
    }

    private async Task AddChatId(string message, CancellationToken cancellationToken)
    {
        var chatInfo = message?.Split(" ");

        if (chatInfo != null && chatInfo.Length == 2 && long.TryParse(chatInfo[0], out var chatId))
        {
            await _dbContext.AddAvaliableChatId(chatId, chatInfo[1]);
            await _botClient.SendTextMessageAsync(chatId, "🥳 the admin has allowed you to use the bot", cancellationToken: cancellationToken);
        }
        else
        {
            await _botClient.SendTextMessageAsync(long.Parse(_config.GetSection("AdminChatId").Value), "uncorrect data, try again /addchat later", cancellationToken: cancellationToken);
        }
    }

    private async Task PreAddChatId(CancellationToken cancellationToken)
    {
        await _botClient.SendTextMessageAsync(long.Parse(_config.GetSection("AdminChatId").Value), "send me your chat id and name separated by space", cancellationToken: cancellationToken);
    }
}

