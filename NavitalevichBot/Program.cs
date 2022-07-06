using FluentScheduler;
using InstagramApiSharp.API;
using Microsoft.Data.Sqlite;
using System.Reflection;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Extensions.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
namespace NavitalevichBot;

public class Program
{
    static ITelegramBotClient botClient = new TelegramBotClient("5307410957:AAEDvwbykNd_hBUOh63upqaQfr8FPkDQJTc");

    private static IInstaApi InstaApi;
    private static InstModule InstModule;
    private static DatabaseContext _context;

    public static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        var chatId = update?.Message?.Chat?.Id;
        if (chatId == null) return;

        if (InstModule == null) {
            InstModule = new InstModule(InstaApi, botClient, _context, chatId.Value, cancellationToken);
            JobManager.Initialize(InstModule);
        }

        // Only process Message updates: https://core.telegram.org/bots/api#message
        if (update.Type != UpdateType.Message)
            return;
        // Only process text messages
        if (update.Message!.Type != MessageType.Text)
            return;

        var messageText = update.Message.Text;

        if(messageText == "/getposts")
        {
            await InstModule.SendPosts(cancellationToken);
        }
        if (messageText == "/getstories")
        {
            await InstModule.SendStories(cancellationToken);
        }
        if (messageText == "/resetcache")
        {
            await InstModule.ResetCache(cancellationToken);
        }
        if (messageText != null && messageText.StartsWith("/disable"))
        {
            await InstModule.AddUserToBlackList(messageText.Split(" ")[1], cancellationToken);
        }
        if (messageText == "/like" && update?.Message?.ReplyToMessage?.MessageId != null )
        {
            await InstModule.LikeMedia(update.Message.ReplyToMessage.MessageId, cancellationToken);
        }
    }

    public static Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        var ErrorMessage = exception switch
        {
            ApiRequestException apiRequestException
                => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
            _ => exception.ToString()
        };

        Console.WriteLine(ErrorMessage);
        return Task.CompletedTask;
    }

    public static async Task Main(string[] args)
    {
        Console.WriteLine("Запущен бот ");
        using var cts = new CancellationTokenSource();

        _context = new DatabaseContext();

        InstaApi = await InstClientFactory.CreateAndLoginInstClient();

        // StartReceiving does not block the caller thread. Receiving is done on the ThreadPool.
        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = { } // receive all update types
        };
        botClient.StartReceiving(
            HandleUpdateAsync,
            HandleErrorAsync,
            receiverOptions,
            cancellationToken: cts.Token);

        var me = await botClient.GetMeAsync();
        
        Console.ReadLine();

        cts.Cancel();

        await Task.Delay(2000);
    }
}