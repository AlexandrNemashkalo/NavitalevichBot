using NavitalevichBot.Data;
using NavitalevichBot.Services.Session;
using Telegram.Bot;

namespace NavitalevichBot.Services;

public class MessageSessionHandlerManager : IInstSessionHandlerManager
{
    private readonly ITelegramBotClient _botClient;
    private readonly IStorageContext _dbContext;
    public MessageSessionHandlerManager(ITelegramBotClient botClient, IStorageContext dbContext)
    {
        _botClient = botClient;
        _dbContext = dbContext;
    }

    public IInstSessionHandler CreateSessionHandler(long chatId)
    {
        return new MessageSessionHandler(_botClient, _dbContext, chatId);
    } 
}
