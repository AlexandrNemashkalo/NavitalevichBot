using Microsoft.Extensions.Logging;
using NavitalevichBot.Data;
using NavitalevichBot.Exceptions;
using NavitalevichBot.Helpers;
using Telegram.Bot;

namespace NavitalevichBot.Services;
public class ExceptionHandler
{
    private readonly ITelegramBotClient _botClient;
    private readonly IStorageContext _dbContext;
    private readonly ILogger _logger;

    public ExceptionHandler(
        ITelegramBotClient botClient, 
        IStorageContext dbContext,
        ILoggerFactory loggerFactory
    )
    {
        _botClient = botClient;
        _dbContext = dbContext;
        _logger = loggerFactory.CreateLogger<ExceptionHandler>();
    }

    public async Task HandleException(long chatId, Exception ex)
    {
        _logger.LogError(chatId, $"{ex.Message} \n{ex.StackTrace}");

        switch (ex)
        {
            case InstException e:
                await HandleInstException(chatId, e);
                break;
            case UncorrectDataException e:
                await HandleUncorrectDataException(chatId, e);
                break;
            default:
                await HandleUnknownException(chatId, ex);
                break;
        };

    }

    private async Task HandleInstException(long chatId, InstException ex)
    {
        var messageText = "inst error: ";
        switch (ex.InstCode)
        {
            case Models.InstExceptionCode.LoginRequired:
                await _dbContext.DeleteSessionMessage(chatId);
                messageText += "you need login again, use /setuser";
                break;
            default:
                messageText += ex.Message;
                break;
        };

        await _botClient.SendTextMessageAsync(chatId, messageText);
    }

    private async Task HandleUncorrectDataException(long chatId, UncorrectDataException ex)
    {
        await _botClient.SendTextMessageAsync(chatId, "uncorrect data error: " + ex.Message);
    }

    private async Task HandleUnknownException(long chatId, Exception ex)
    {
        await _botClient.SendTextMessageAsync(chatId, "unknown error: " + ex.Message);
    }
}
