using NavitalevichBot.Actions.Core;
using NavitalevichBot.Data;
using NavitalevichBot.Models;

using Telegram.Bot;

namespace NavitalevichBot.Actions;

internal class DisableInstUserAction : BaseUserAction, IBotAction
{
    private readonly IStorageContext _dbContext;

    public DisableInstUserAction(ITelegramBotClient botClient, IStorageContext dbContext) 
        : base(botClient)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> HandleAction(BotActionParams data, CancellationToken cancellationToken = default)
    {
        var updateMessage = data.Update.Message;
        if (updateMessage!.Text != BotActionCommands.disable) return false;


        var chatId = updateMessage!.Chat!.Id;
        if(!await IsInstAuth(chatId, data.UserStatus, cancellationToken)) return true;

        var userName = updateMessage.Text!.Split(" ")[1];

        await _dbContext.AddUserToBlackList(userName, chatId);
        await _botClient.SendTextMessageAsync(chatId, "success", cancellationToken: cancellationToken);

        return true;
    }
}